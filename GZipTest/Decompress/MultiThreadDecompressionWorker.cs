using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using GZipTest.Common;

namespace GZipTest.Decompress
{
    public class MultiThreadDecompressionWorker
    {
        private bool _workerIsUsed = false;
        private bool _hasPrivateError = false;
        private bool _hasPublicError = false;
        private readonly List<string> _privateErrors = new List<string>();
        private readonly List<string> _publicErrors = new List<string>();

        private readonly int _processorsCount;

        private readonly object _readLocker = new object();

        private readonly object _writeLocker = new object();

        private readonly object _privateErrorWritingLocker = new object();

        private readonly object _publicErrorWritingLocker = new object();

        public MultiThreadDecompressionWorker(int processorsCount)
        {
            _processorsCount = processorsCount;
        }

        public OperationResult Decompress(FileStream inputArchiveFileStream, FileStream outputFileStream)
        {
            if (_workerIsUsed)
            {
                throw new InvalidOperationException("Worker can not be run twice.");
            }

            SetOutputFileSize(inputArchiveFileStream, outputFileStream);

            var threads = new Thread[_processorsCount];

            for (var i = 0; i < _processorsCount; i++)
            {
                var thread = new Thread(RunDecompression);
                threads[i] = thread;
            }

            foreach (var thread in threads)
            {
                thread.Start(new RunDecompressionParameters
                {
                    InputArchiveFileStream = inputArchiveFileStream,
                    OutputFileStream = outputFileStream
                });
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            _workerIsUsed = true;

            return GetWorkResult();
        }

        private void RunDecompression(object obj)
        {
            try
            {
                var parameters = (RunDecompressionParameters) obj;
                var inputArchiveFileStream = parameters.InputArchiveFileStream;
                var outputFileStream = parameters.OutputFileStream;

                while (true)
                {
                    if (_hasPrivateError || _hasPublicError)
                    {
                        return;
                    }

                    var archiveBlock = GetCompressedBlockFromStream(inputArchiveFileStream);

                    if (archiveBlock == null)
                    {
                        break;
                    }

                    var blockPosition = GetDecompressedBlockPosition(archiveBlock, outputFileStream);

                    var decompressedBlockData = DecompressBlock(archiveBlock);

                    WriteDecompressedBlockToStream(blockPosition, decompressedBlockData, outputFileStream);
                }

            }
            catch (FormatException ex)
            {
                WritePublicError("Incorrect archive format.");

                // Can be a useful diagnostic information. So we may log or report it.
                WritePrivateError($"{ex.Message} + {ex.StackTrace}");
            }
            catch (Exception ex)
            {
                WritePrivateError(ex.Message + "\n" + ex.StackTrace);
            }
        }

        private void WriteDecompressedBlockToStream(long blockPosition, DecompressedArchiveBlockData decompressedBlockData,
            FileStream outputFileStream)
        {
            lock (_writeLocker)
            {
                outputFileStream.Position = blockPosition;
                outputFileStream.Write(decompressedBlockData.DecompressedData, 0, decompressedBlockData.DecompressedDataLength);
            }
        }

        private long GetDecompressedBlockPosition(ArchiveBlockData archiveBlock, FileStream outputFileStream)
        {
            long blockPosition = FormatConstants.BlockSize*archiveBlock.BlockNumber;

            if (blockPosition >= outputFileStream.Length)
            {
                throw new FormatException(
                    $"Incorrect archive format. Block position {archiveBlock.BlockNumber} does not correspond to original file size = {outputFileStream.Length} bytes.");
            }
            return blockPosition;
        }

        private DecompressedArchiveBlockData DecompressBlock(ArchiveBlockData archiveBlock)
        {
            var decompressedData = new byte[FormatConstants.BlockSize];
            int numberOfBytesDecompressed;

            using (var memoryStream = new MemoryStream(archiveBlock.CompressedData))
            {
                using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    numberOfBytesDecompressed = gZipStream.Read(decompressedData, 0, FormatConstants.BlockSize);
                }
            }

            return new DecompressedArchiveBlockData
            {
                DecompressedData = decompressedData,
                DecompressedDataLength = numberOfBytesDecompressed
            };
        }

        private ArchiveBlockData GetCompressedBlockFromStream(FileStream inputArchiveFileStream)
        {
            lock (_readLocker)
            {
                var compressedDataBlockNumber = GetIntegerFromInputStream(inputArchiveFileStream);

                if (compressedDataBlockNumber == null)
                {
                    return null;
                }

                var compressedDataBlockSize = GetIntegerFromInputStream(inputArchiveFileStream);

                if (compressedDataBlockSize == null)
                {
                    throw new FormatException(
                        $"Incorrect archive format. Block with information about following compressed data block length should be {sizeof(int)} bytes size, but was 0 bytes.");
                }

                var compressedData = new byte[compressedDataBlockSize.Value];

                var numberOfBytesRead = inputArchiveFileStream.Read(compressedData, 0, compressedDataBlockSize.Value);

                if (numberOfBytesRead != compressedDataBlockSize.Value)
                {
                    throw new FormatException(
                        $"Incorrect archive format. Block with compressed data block should be {compressedDataBlockSize.Value} bytes size, but was {numberOfBytesRead} bytes.");
                }

                return new ArchiveBlockData
                {
                    CompressedData = compressedData,
                    BlockNumber = compressedDataBlockNumber.Value
                };
            }
        }

        private int? GetIntegerFromInputStream(FileStream inputArchiveFileStream)
        {
            var followingIntegerValueBytes = new byte[sizeof(int)];
            var numberOfBytesRead = inputArchiveFileStream.Read(followingIntegerValueBytes, 0, sizeof(int));

            if (numberOfBytesRead == 0)
            {
                return null;
            }
            
            if (numberOfBytesRead != sizeof(int))
            {
                throw new FormatException(
                    $"Incorrect archive format. Block with integer value information should be {sizeof(int)} bytes size, but was {numberOfBytesRead} bytes.");
            }

            return BitConverter.ToInt32(followingIntegerValueBytes, 0);
        }

        private void SetOutputFileSize(FileStream inputArchiveFileStream, FileStream outputFileStream)
        {
            var originalFileSizeBytes = new byte[sizeof(long)];
            var numberOfBytesWritten = inputArchiveFileStream.Read(originalFileSizeBytes, 0, sizeof(long));
            if (numberOfBytesWritten < sizeof(long))
            {
                throw new GZipTestPublicException($"{inputArchiveFileStream.Name}: wrong archive file format.");
            }

            var originalFileSize = BitConverter.ToInt64(originalFileSizeBytes, 0);

            try
            {
                outputFileStream.SetLength(originalFileSize);
            }
            catch (IOException)
            {
                throw new GZipTestPublicException($"{outputFileStream.Name}: can not create file. Possible not enough disk space.");
            }
        }

        private void WritePublicError(string errorText)
        {
            lock (_publicErrorWritingLocker)
            {
                _hasPublicError = true;
                _publicErrors.Add(errorText);
            }
        }

        private void WritePrivateError(string errorText)
        {
            lock (_privateErrorWritingLocker)
            {
                _hasPrivateError = true;
                _privateErrors.Add(errorText);
            }
        }

        private OperationResult GetWorkResult()
        {
            if (!_hasPrivateError && !_hasPublicError)
            {
                return new OperationResult(OperationResultType.Success);
            }

            OperationResultType resultType = 0;

            if (_hasPrivateError)
            {
                resultType |= OperationResultType.PrivateError;
            }

            if (_hasPublicError)
            {
                resultType |= OperationResultType.PublicError;
            }

            return new OperationResult(resultType, _privateErrors, _publicErrors);
        }

        private class RunDecompressionParameters
        {
            public FileStream InputArchiveFileStream { get; set; }

            public FileStream OutputFileStream { get; set; }
        }

        private class ArchiveBlockData
        {
            public byte[] CompressedData { get; set; }

            public int BlockNumber { get; set; }
        }

        private class DecompressedArchiveBlockData
        {
            public byte[] DecompressedData { get; set; }

            public int DecompressedDataLength { get; set; }
        }
    }
}
