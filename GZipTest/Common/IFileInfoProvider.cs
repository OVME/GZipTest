using System.IO;

namespace GZipTest.Common
{
    public interface IFileInfoProvider
    {
        FileInfo GetInputFileInfo(string inputFileName);
        FileInfo GetOutputFileInfo(string outputFileName);
    }
}
