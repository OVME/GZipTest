using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using GZipTest.Common;
using GZipTest.Common.MultiThreading;

namespace GZipTest.Compress
{
    public class MultiThreadCompressionWorker : MultiThreadWorker
    {
        private readonly object _readLocker = new object();

        private readonly object _writeLocker = new object();

        private readonly object _writeToBufferLocker = new object();

        private readonly int _blocksLimitInBuffer;

        private volatile int _blockNumber = 0;

        private int _blocksAmount;

        private int _writtenBlocksAmount = 0;

        private List<CompressedBlock> _buffer;

        public MultiThreadCompressionWorker(int processorsCount) : base(processorsCount)
        {
            _blocksLimitInBuffer = processorsCount;
        }

        protected override void WorkInternal(MultiThreadWorkerParameters parameters)
        {
            SetBlocksAmount(parameters.InputFileInfo);
            InitializeBuffer();
            var threads = GetThreads(RunCompression);

            try
            {
                using (var outputArchiveFileStream = parameters.OutputFileInfo.OpenWrite())
                {
                    WriteOriginalFileSize(parameters.InputFileInfo, outputArchiveFileStream);

                    foreach (var thread in threads)
                    {
                        thread.Start(new RunCompressionParameters
                        {
                            InputFileInfo = parameters.InputFileInfo,
                            OutputFileStream = outputArchiveFileStream
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

        private void InitializeBuffer()
        {
            _buffer = new List<CompressedBlock>(_blocksLimitInBuffer);
        }

        private void SetBlocksAmount(FileInfo inputFileInfo)
        {
            _blocksAmount = (int)(inputFileInfo.Length / FormatConstants.BlockSize);
            if (inputFileInfo.Length % FormatConstants.BlockSize > 0)
            {
                _blocksAmount++;
            }
        }

        private void RunCompression(object obj)
        {
            try
            {
                var parameters = (RunCompressionParameters)obj;
                var inputFileInfo = parameters.InputFileInfo;
                var outputFileStream = parameters.OutputFileStream;
                try
                {
                    using (var inputFileStream = inputFileInfo.OpenRead())
                    {
                        while (true)
                        {
                            if (HasPrivateError || HasPublicError)
                            {
                                return;
                            }

                            var block = GetNextBlock(inputFileStream);

                            if (block == null)
                            {
                                break;
                            }

                            var compressedData = CompressDataBlock(block.Data, block.DataLength);

                            WriteCompressedBlock(outputFileStream, block.BlockNumber, compressedData);
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    WritePublicError($"{inputFileInfo.FullName}: can not get access to file");
                }
            }
            catch (Exception ex)
            {
                WritePrivateError(ex.Message + "\n" + ex.StackTrace);
            }
        }

        private BlockInfo GetNextBlock(FileStream inputFileStream)
        {
            var blockData = new byte[FormatConstants.BlockSize];
            int blockNumber;

            lock (_readLocker)
            {
                blockNumber = GetNextBlockNumber();
            }

            if (blockNumber >= _blocksAmount)
            {
                return null;
            }

            SetInputFileStreamPosition(inputFileStream, blockNumber);

            var numberOfBytesReadFromInputFileStream = inputFileStream.Read(blockData, 0, FormatConstants.BlockSize);

            if (numberOfBytesReadFromInputFileStream == 0)
            {
                return null;
            }

            return new BlockInfo
            {
                BlockNumber = blockNumber,
                Data = blockData,
                DataLength = numberOfBytesReadFromInputFileStream
            };
        }

        private void SetInputFileStreamPosition(FileStream inputFileStream, int blockNumber)
        {
            var position = blockNumber*FormatConstants.BlockSize;

            inputFileStream.Position = position;
        }

        private int GetNextBlockNumber()
        {
            return _blockNumber++;
        }

        private void WriteOriginalFileSize(FileInfo inputFileInfo, FileStream outputArchiveFileStream)
        {
            var originalFileSize = inputFileInfo.Length;

            var originalFileSizeBytes = BitConverter.GetBytes(originalFileSize);

            try
            {
                outputArchiveFileStream.Write(originalFileSizeBytes, 0, sizeof(long));
            }
            catch (IOException)
            {
                throw new GZipTestPublicException($"{outputArchiveFileStream.Name}: write error. Possible there is not enough disk space");
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
            var compressedBlock = new CompressedBlock
            {
                BlockNumber = blockNumber,
                CompressedData = compressedData
            };

            List<CompressedBlock> buffer = null;

            lock (_writeToBufferLocker)
            {
                _buffer.Add(compressedBlock);
                _writtenBlocksAmount++;
                if (BufferIsFilled())
                {
                    buffer = _buffer;
                    InitializeBuffer();
                }
            }

            if (buffer != null)
            {
                WriteBufferToOutputFileStream(buffer, outputFileStream);
            }

            if (buffer == null && CurrentBlockIsLast())
            {
                WriteBufferToOutputFileStream(_buffer, outputFileStream);
            }
        }

        private bool CurrentBlockIsLast()
        {
            return _writtenBlocksAmount == _blocksAmount;
        }

        private void WriteBufferToOutputFileStream(List<CompressedBlock> buffer, FileStream outputFileStream)
        {
            lock (_writeLocker)
            {
                try
                {
                    foreach (var compressedBlock in buffer)
                    {
                        var compressedDataLength = compressedBlock.CompressedData.Length;
                        outputFileStream.Write(BitConverter.GetBytes(compressedBlock.BlockNumber), 0, sizeof(int));
                        outputFileStream.Write(BitConverter.GetBytes(compressedDataLength), 0, sizeof(int));
                        outputFileStream.Write(compressedBlock.CompressedData, 0, compressedDataLength);
                    }
                }
                catch (IOException)
                {
                    WritePublicError($"{outputFileStream.Name}: write error. Possible there is not enough disk space");
                }
            }
        }

        private bool BufferIsFilled()
        {
            return _buffer.Count == _blocksLimitInBuffer;
        }

        private class BlockInfo
        {
            public byte[] Data { get; set; }

            public int DataLength { get; set; }

            public int BlockNumber { get; set; }
        }

        private class CompressedBlock
        {
            public byte[] CompressedData { get; set; }

            public int BlockNumber { get; set; }
        }

        private class RunCompressionParameters
        {
            public FileInfo InputFileInfo { get; set; }
            
            public FileStream OutputFileStream { get; set; } 
        }
    }
}
