using System.IO;

namespace GZipTest.Compress
{
    public class MultiThreadCompressionWorker
    {
        private const int BlockSize = 1048576;

        private readonly int _processorsCount;

        private readonly object _locker = new object();

        private readonly FileStream _inputFileStream;

        private readonly FileStream _outputFileStream;

        private int _blockNumber = 0;

        // TODO: handle exceptions in threads, mb make class instanse once-used
        public MultiThreadCompressionWorker(FileStream inputFileStream, FileStream outputFileStream, int processorsCount)
        {
            _inputFileStream = inputFileStream;
            _outputFileStream = outputFileStream;
            _processorsCount = processorsCount;
        }

        public void Compress()
        {
            
        }

        private int GetNextBlockNumber()
        {
            return _blockNumber++;
        }
    }
}
