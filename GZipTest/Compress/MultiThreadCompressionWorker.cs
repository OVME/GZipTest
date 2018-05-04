using System;
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

        private volatile int _blockNumber = 0;

        public MultiThreadCompressionWorker(int processorsCount) : base(processorsCount)
        {
        }

        protected override void WorkInternal(MultiThreadWorkerParameters parameters)
        {
            var inputFileStream = parameters.InputFileStream;
            var outputArchiveFileStream = parameters.OutputFileStream;

            WriteOriginalFileSize(inputFileStream, outputArchiveFileStream);

            var threads = GetThreads(RunCompression);

            foreach (var thread in threads)
            {
                thread.Start(parameters);
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }
        }

        private void RunCompression(object obj)
        {
            try
            {
                var parameters = (MultiThreadWorkerParameters)obj;
                var inputArchiveFileStream = parameters.InputFileStream;
                var outputFileStream = parameters.OutputFileStream;

                while (true)
                {
                    if (HasPrivateError || HasPublicError)
                    {
                        return;
                    }

                    var block = GetNextBlock(inputArchiveFileStream);

                    if (block == null)
                    {
                        break;
                    }

                    var compressedData = CompressDataBlock(block.Data, block.DataLength);

                    WriteCompressedBlock(outputFileStream, block.BlockNumber, compressedData);
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
            int numberOfBytesReadFromInputFileStream;
            int blockNumber;

            lock (_readLocker)
            {
                numberOfBytesReadFromInputFileStream = inputFileStream.Read(blockData, 0, FormatConstants.BlockSize);

                if (numberOfBytesReadFromInputFileStream == 0)
                {
                    return null;
                }

                blockNumber = GetNextBlockNumber();
            }

            return new BlockInfo
            {
                BlockNumber = blockNumber,
                Data = blockData,
                DataLength = numberOfBytesReadFromInputFileStream
            };
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

        private class BlockInfo
        {
            public byte[] Data { get; set; }

            public int DataLength { get; set; }

            public int BlockNumber { get; set; }
        }
    }
}
