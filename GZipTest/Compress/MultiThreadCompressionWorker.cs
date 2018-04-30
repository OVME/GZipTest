using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using GZipTest.Common;

namespace GZipTest.Compress
{
    public class MultiThreadCompressionWorker
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

        private int _blockNumber = 0;
        
        public MultiThreadCompressionWorker(int processorsCount)
        {
            _processorsCount = processorsCount;
        }

        public OperationResult Compress(FileStream inputFileStream, FileStream outputArchiveFileStream)
        {
            if (_workerIsUsed)
            {
                throw new InvalidOperationException("Worker can not be run twice.");
            }

            WriteOriginalFileSize(inputFileStream, outputArchiveFileStream);

            var threads = new Thread[_processorsCount];

            for (var i = 0; i < _processorsCount; i++)
            {
                var thread = new Thread(RunCompression);
                threads[i] = thread;
            }

            foreach (var thread in threads)
            {
                thread.Start(new RunCompressionParameters
                {
                    InputFileStream = inputFileStream,
                    OutputArchiveFileStream = outputArchiveFileStream
                });
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            _workerIsUsed = true;

            return GetWorkResult();
        }

        private void RunCompression(object obj)
        {
            try
            {
                var parameters = (RunCompressionParameters)obj;
                var inputArchiveFileStream = parameters.InputFileStream;
                var outputFileStream = parameters.OutputArchiveFileStream;

                var dataToCompress = new byte[FormatConstants.BlockSize];
                int numberOfBytesReadFromInputFileStream;
                int blockNumber;

                while (true)
                {
                    if (_hasPrivateError || _hasPublicError)
                    {
                        return;
                    }

                    lock (_readLocker)
                    {
                        numberOfBytesReadFromInputFileStream = inputArchiveFileStream.Read(dataToCompress, 0, FormatConstants.BlockSize);

                        if (numberOfBytesReadFromInputFileStream == 0)
                        {
                            break;
                        }

                        blockNumber = GetNextBlockNumber();
                    }

                    var compressedData = CompressDataBlock(dataToCompress, numberOfBytesReadFromInputFileStream);

                    WriteCompressedBlock(outputFileStream, blockNumber, compressedData);
                }
            }
            catch (Exception ex)
            {
                WritePrivateError(ex.Message + "\n" + ex.StackTrace);
            }
        }

        private int GetNextBlockNumber()
        {
            return _blockNumber++;
        }

        private void WriteOriginalFileSize(FileStream inputFileStream, FileStream outputArchiveFileStream)
        {
            var originalFileSize = inputFileStream.Length;

            var originalFileSizeBytes = BitConverter.GetBytes(originalFileSize);

            try
            {
                outputArchiveFileStream.Write(originalFileSizeBytes, 0, sizeof(long));
            }
            catch (IOException)
            {
                WritePublicError($"{outputArchiveFileStream.Name}: write error. Possible there is not enough disk space");
            }
        }

        private byte[] CompressDataBlock(byte[] dataToCompress, int numberOfBytesReadFromInputFileStream)
        {
            byte[] compressedData;
            using (var memoryStream = new MemoryStream())
            {
                using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                {
                    gZipStream.Write(dataToCompress, 0, numberOfBytesReadFromInputFileStream);
                }

                compressedData = memoryStream.ToArray();
            }

            return compressedData;
        }

        private void WriteCompressedBlock(FileStream outputFileStream, int blockNumber, byte[] compressedData)
        {
            lock (_writeLocker)
            {
                try
                {
                    var compressedDataLength = compressedData.Length;
                    outputFileStream.Write(BitConverter.GetBytes(blockNumber), 0, sizeof(int));
                    outputFileStream.Write(BitConverter.GetBytes(compressedDataLength), 0, sizeof(int));
                    outputFileStream.Write(compressedData, 0, compressedDataLength);
                }
                catch (IOException)
                {
                    WritePublicError($"{outputFileStream.Name}: write error. Possible there is not enough disk space");
                }
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

        private class RunCompressionParameters
        {
            public FileStream InputFileStream { get; set; }

            public FileStream OutputArchiveFileStream { get; set; }
        }
    }
}
