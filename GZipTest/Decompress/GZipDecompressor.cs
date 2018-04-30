using System;
using System.IO;
using System.IO.Compression;
using GZipTest.Common;

namespace GZipTest.Decompress
{
    public class GZipDecompressor : IDecompressor
    {
        public void Decompress(string inputArchiveName, string outputFileName)
        {
            var fileInfoProvider = new FileInfoProvider();

            var inputArchiveInfo = fileInfoProvider.GetInputFileInfo(inputArchiveName);

            var outputFileInfo = fileInfoProvider.GetOutputFileInfo(outputFileName);

            DecompressInternal(inputArchiveInfo, outputFileInfo);
        }

        private void DecompressInternal(FileInfo inputArchiveInfo, FileInfo outputFileInfo)
        {
            using (var inputArchiveFileStream = inputArchiveInfo.OpenRead())
            {
                using (var outputFileStream = outputFileInfo.Create())
                {
                    var followingCompressedDataBlockSizeBytes = new byte[sizeof(int)];
                    int numberOfBytesRead;
                    var decompressedData = new byte[FormatConstants.BlockSize];

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
                                var numberOfBytesDecompressed = gZipStream.Read(decompressedData, 0, FormatConstants.BlockSize);
                                outputFileStream.Write(decompressedData, 0, numberOfBytesDecompressed);
                            }
                        }
                    }
                }
            }
        }
    }
}
