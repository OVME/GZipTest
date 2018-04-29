using System;
using System.IO;
using GZipTest.IO;

namespace GZipTest.Compress
{
    internal class GZipCompressor : ICompressor
    {
        public void Compress(string inputFileName, string outputArchiveName)
        {
            var fileInfoProvider = new FileInfoProvider();

            var inputFileInfo = fileInfoProvider.GetInputFileInfo(inputFileName);

            var outputFileInfo = fileInfoProvider.GetOutputFileInfo(outputArchiveName);
            
            CompressInternal(inputFileInfo, outputFileInfo);
        }

        private void CompressInternal(FileInfo inputFileInfo, FileInfo outputFileInfo)
        {
            var compressionWorker = new MultiThreadCompressionWorker(Environment.ProcessorCount);

            try
            {
                using (var inputFileStream = inputFileInfo.OpenRead())
                {
                    try
                    {
                        using (var outputFileStream = outputFileInfo.Create())
                        {
                            compressionWorker.Compress(inputFileStream, outputFileStream);
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        throw new GZipTestPublicException($"{inputFileInfo.FullName}: have no permissiom to create file");
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                throw new GZipTestPublicException($"{inputFileInfo.FullName}: can not get access to file");
            }
        }
    }
}
