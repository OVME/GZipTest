using System;
using System.IO;

namespace GZipTest.Decompress
{
    public class GZipDecompressor : IDecompressor
    {
        // TODO: mb replace to any static class
        private const int BlockSize = 1048576;

        public void Decompress(string inputArchiveName, string outputFileName)
        {
            FileInfo inputArchiveInfo;

            try
            {
                inputArchiveInfo = new FileInfo(inputArchiveName);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new GZipTestPublicException($"{inputArchiveName}: cannot get access to archive file.");
            }

            if (!inputArchiveInfo.Exists)
            {
                throw new GZipTestPublicException($"{inputArchiveName}: archive file does not exist.");
            }

            // TODO: cases from compressor + incorrect block format
            using (var inputArchiveFileStream = inputArchiveInfo.OpenRead())
            {
                using (var outputFileStream = new FileStream(outputFileName, FileMode.Create))
                {
                    var followingCompressedBlockSizeBytes = new byte[sizeof(int)];
                    int numbersOfBytesRead;
                    while (outputFileStream(followingCompressedBlockSizeBytes, 0, sizeof(int)) )
                }
            }
        }
    }
}
