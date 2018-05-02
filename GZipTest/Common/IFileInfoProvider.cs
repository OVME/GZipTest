using System.IO;

namespace GZipTest.Common
{
    // TODO: remove interfaces. I won't do DI.
    public interface IFileInfoProvider
    {
        FileInfo GetInputFileInfo(string inputFileName);
        FileInfo GetOutputFileInfo(string outputFileName);
    }
}
