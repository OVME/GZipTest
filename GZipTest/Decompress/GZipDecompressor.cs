using System;
using System.IO;
using System.IO.Compression;

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
                    var followingCompressedDataBlockSizeBytes = new byte[sizeof(int)];
                    int numberOfBytesRead;
                    var decompressedData = new byte[BlockSize];

                    while ((numberOfBytesRead = inputArchiveFileStream.Read(followingCompressedDataBlockSizeBytes, 0, sizeof(int))) != 0)
                    {
                        if (numberOfBytesRead != sizeof(int))
                        {
                            throw new FormatException(
                                $"Wrong archive format. Block with information about following compressed data block length should be {sizeof(int)} bytes size, but was {numberOfBytesRead} bytes.");
                        }

                        var compressedDataBlockSize = BitConverter.ToInt32(followingCompressedDataBlockSizeBytes, 0);

                        var compressedData = new byte[compressedDataBlockSize];

                        inputArchiveFileStream.Read(compressedData, 0, compressedDataBlockSize);

                        using (var memoryStream = new MemoryStream(compressedData))
                        {
                            using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                            {
                                var numberOfBytesDecompressed = gZipStream.Read(decompressedData, 0, BlockSize);
                                outputFileStream.Write(decompressedData, 0, numberOfBytesDecompressed);
                            }
                        }
                    }
                }
            }
        }
    }
}
