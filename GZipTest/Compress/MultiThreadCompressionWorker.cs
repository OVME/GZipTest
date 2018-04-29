using System;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace GZipTest.Compress
{
    public class MultiThreadCompressionWorker
    {
        private readonly int _processorsCount;

        private readonly object _readLocker = new object();

        private readonly object _writeLocker = new object();

        private int _blockNumber = 0;

        // TODO: handle exceptions in threads, mb make class instanse once-used
        public MultiThreadCompressionWorker(int processorsCount)
        {
            _processorsCount = processorsCount;
        }

        public void Compress(FileStream inputArchiveFileStream, FileStream outputFileStream)
        {
            WriteOriginalFileSize(inputArchiveFileStream, outputFileStream);

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
                    InputArchiveFileStream = inputArchiveFileStream,
                    OutputFileStream = outputFileStream
                });
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }
        }

        private void RunCompression(object obj)
        {
            var parameters = (RunCompressionParameters) obj;

            var inputArchiveFileStream = parameters.InputArchiveFileStream;
            var outputFileStream = parameters.OutputFileStream;

            var dataToCompress = new byte[FormatConstants.BlockSize];
            int numberOfBytesReadFromInputFileStream;
            int blockNumber;

            while (true)
            {
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

                var compressedDataLength = compressedData.Length;

                lock (_writeLocker)
                {
                    WriteCompressedBlock(outputFileStream, blockNumber, compressedDataLength, compressedData);
                }
            }
        }

        private int GetNextBlockNumber()
        {
            return _blockNumber++;
        }

        private void WriteOriginalFileSize(FileStream inputArchiveFileStream, FileStream outputFileStream)
        {
            var originalFileSize = inputArchiveFileStream.Length;

            var originalFileSizeBytes = BitConverter.GetBytes(originalFileSize);

            outputFileStream.Write(originalFileSizeBytes, 0, sizeof(long));
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

        private void WriteCompressedBlock(FileStream outputFileStream, int blockNumber, int compressedDataLength,
            byte[] compressedData)
        {
            outputFileStream.Write(BitConverter.GetBytes(blockNumber), 0, sizeof(int));
            outputFileStream.Write(BitConverter.GetBytes(compressedDataLength), 0, sizeof(int));
            outputFileStream.Write(compressedData, 0, compressedDataLength);
        }

        private class RunCompressionParameters
        {
            public FileStream InputArchiveFileStream { get; set; }

            public FileStream OutputFileStream { get; set; }
        }
    }
}
