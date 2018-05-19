using System.IO;

namespace GZipTest.Common.MultiThreading
{
    public class MultiThreadWorkerParameters
    {
        public MultiThreadWorkerParameters(FileInfo inputFileInfo, FileInfo outputFileInfo)
        {
            InputFileInfo = inputFileInfo;
            OutputFileInfo = outputFileInfo;
        }

        public FileInfo InputFileInfo { get; }

        public FileInfo OutputFileInfo { get; }
    }
}
