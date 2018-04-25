using System;
using System.IO;

namespace GZipTest.IO
{
    public class FileInfoProvider : IFileInfoProvider
    {
        public FileInfo GetInputFileInfo(string inputFileName)
        {
            FileInfo inputFileInfo;

            try
            {
                inputFileInfo = new FileInfo(inputFileName);
            }
            catch (UnauthorizedAccessException)
            {
                throw new GZipTestPublicException($"{inputFileName}: cannot get access to file.");
            }
            catch (ArgumentException)
            {
                throw new GZipTestPublicException($"{inputFileName}: invalid file name.");
            }
            catch (NotSupportedException)
            {
                throw new GZipTestPublicException($"{inputFileName}: invalid file name.");
            }

            if (!inputFileInfo.Exists)
            {
                throw new GZipTestPublicException($"{inputFileName}: file does not exist.");
            }

            return inputFileInfo;
        }

        public FileInfo GetOutputFileInfo(string outputFileName)
        {
            FileInfo outputFileInfo;

            try
            {
                outputFileInfo = new FileInfo(outputFileName);
            }
            catch (ArgumentException)
            {
                throw new GZipTestPublicException($"{outputFileName}: invalid file name.");
            }
            catch (NotSupportedException)
            {
                throw new GZipTestPublicException($"{outputFileName}: invalid file name.");
            }

            var directory = outputFileInfo.Directory;
            if (directory.Exists)
            {
                try
                {
                    directory.GetAccessControl();
                }
                catch (UnauthorizedAccessException)
                {
                    throw new GZipTestPublicException($"{directory.FullName}: can not get access to directory.");
                }
            }

            if (outputFileInfo.Exists)
            {
                throw new GZipTestPublicException($"{outputFileName}: file already exists.");
            }

            return outputFileInfo;
        }
    }
}
