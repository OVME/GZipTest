﻿using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using GZipTest.Common;
using GZipTest.Common.MultiThreading;

namespace GZipTest.Decompress
{
    public class MultiThreadDecompressionWorker : MultiThreadWorker
    {
        private const int BufferBlocksPerThread = 10;

        private readonly object _bufferFillingLocker = new object();

        private readonly object _writeLocker = new object();

        private readonly DecompressionBuffer<ArchiveBlock> _buffer;

        private bool _readingCompleted = false;

        public MultiThreadDecompressionWorker(int processorsCount) : base(processorsCount)
        {
            var bufferMaximumSize = processorsCount * BufferBlocksPerThread;
            _buffer = new DecompressionBuffer<ArchiveBlock>(bufferMaximumSize);
        }

        protected override void WorkInternal(MultiThreadWorkerParameters parameters)
        {
            try
            {
                using (var inputArchiveFileStream = parameters.InputFileInfo.OpenRead())
                {
                    try
                    {
                        using (var outputFileStream = parameters.OutputFileInfo.OpenWrite())
                        {
                            SetOutputFileSize(inputArchiveFileStream, outputFileStream);

                            var threads = GetThreads(RunDecompression);

                            foreach (var thread in threads)
                            {
                                thread.Start(new RunDecompressionParameters
                                {
                                    InputFileStream = inputArchiveFileStream,
                                    OutputFileStream = outputFileStream
                                });
                            }

                            foreach (var thread in threads)
                            {
                                thread.Join();
                            }
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        throw new GZipTestPublicException($"{parameters.OutputFileInfo.FullName}: can not get access to file");
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                throw new GZipTestPublicException($"{parameters.InputFileInfo.FullName}: can not get access to file");
            }
        }

        private void RunDecompression(object obj)
        {
            try
            {
                var parameters = (RunDecompressionParameters)obj;
                var inputArchiveFileStream = parameters.InputFileStream;
                var outputFileStream = parameters.OutputFileStream;

                while (true)
                {
                    if (HasPrivateError || HasPublicError)
                    {
                        return;
                    }

                    if (_buffer.NeedFilling() && !_readingCompleted)
                    {
                        FillBuffer(inputArchiveFileStream);
                    }

                    var archiveBlock = _buffer.Pull();

                    if (archiveBlock == null && _readingCompleted)
                    {
                        break;
                    }

                    if (archiveBlock == null)
                    {
                        continue;
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

        private void FillBuffer(FileStream inputArchiveFileStream)
        {
            if (Monitor.TryEnter(_bufferFillingLocker))
            {
                try
                {
                    while (!_buffer.IsFull())
                    {
                        var compressedDataBlockNumber = GetIntegerFromInputStream(inputArchiveFileStream);

                        if (compressedDataBlockNumber == null)
                        {
                            _readingCompleted = true;
                            return;
                        }

                        var compressedDataBlockSize = GetIntegerFromInputStream(inputArchiveFileStream);

                        if (compressedDataBlockSize == null)
                        {
                            throw new FormatException(
                                $"Incorrect archive format. Block with information about following compressed data block length should be {sizeof(int)} bytes size, but was 0 bytes.");
                        }

                        var compressedData = new byte[compressedDataBlockSize.Value];

                        var numberOfBytesRead = inputArchiveFileStream.Read(compressedData, 0,
                            compressedDataBlockSize.Value);

                        if (numberOfBytesRead != compressedDataBlockSize.Value)
                        {
                            throw new FormatException(
                                $"Incorrect archive format. Block with compressed data block should be {compressedDataBlockSize.Value} bytes size, but was {numberOfBytesRead} bytes.");
                        }

                        var block = new ArchiveBlock
                        {
                            CompressedData = compressedData,
                            BlockNumber = compressedDataBlockNumber.Value
                        };

                        _buffer.Push(block);
                    }
                }
                finally
                {
                    Monitor.Exit(_bufferFillingLocker);
                }
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

        private DecompressedArchiveBlockData DecompressBlock(ArchiveBlock archiveBlock)
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

        private void WriteDecompressedBlockToStream(long blockPosition, DecompressedArchiveBlockData decompressedBlockData,
            FileStream outputFileStream)
        {
            lock (_writeLocker)
            {
                outputFileStream.Position = blockPosition;
                outputFileStream.Write(decompressedBlockData.DecompressedData, 0, decompressedBlockData.DecompressedDataLength);
            }
        }

        private long GetDecompressedBlockPosition(ArchiveBlock archiveBlock, FileStream outputFileStream)
        {
            long blockPosition = FormatConstants.BlockSize * archiveBlock.BlockNumber;

            if (blockPosition >= outputFileStream.Length)
            {
                throw new FormatException(
                    $"Incorrect archive format. Block position {archiveBlock.BlockNumber} does not correspond to original file size = {outputFileStream.Length} bytes.");
            }
            return blockPosition;
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

        private class DecompressedArchiveBlockData
        {
            public byte[] DecompressedData { get; set; }

            public int DecompressedDataLength { get; set; }
        }

        private class RunDecompressionParameters
        {
            public FileStream InputFileStream { get; set; }

            public FileStream OutputFileStream { get; set; }
        }
    }
}
