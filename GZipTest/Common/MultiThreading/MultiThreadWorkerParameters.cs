using System.IO;

namespace GZipTest.Common.MultiThreading
{
    public class MultiThreadWorkerParameters
    {
        public MultiThreadWorkerParameters(FileStream inputFileStream, FileStream outputFileStream)
        {
            InputFileStream = inputFileStream;
            OutputFileStream = outputFileStream;
        }

        public FileStream InputFileStream { get; }

        public FileStream OutputFileStream { get; }
    }
}
