using System;
using System.IO;
using System.IO.Compression;
using GZipTest.IO;

namespace GZipTest.Compress
{
    internal class GZipCompressor : ICompressor
    {
        private const int BlockSize = 1048576;

        public void Compress(string inputFileName, string outputArchiveName)
        {
            var fileInfoProvider = new FileInfoProvider();

            var inputFileInfo = fileInfoProvider.GetInputFileInfo(inputFileName);

            var outputFileInfo = fileInfoProvider.GetOutputFileInfo(outputArchiveName);
            
            CompressInternal(inputFileInfo, outputFileInfo);
        }

        private void CompressInternal(FileInfo inputFileInfo, FileInfo outputFileInfo)
        {
            using (var inputFileStream = inputFileInfo.OpenRead())
            {
                using (var outputFileStream = outputFileInfo.Create())
                {
                    var dataToCompress = new byte[BlockSize];
                    int numberOfBytesReadFromInputFileStream;

                    while ((numberOfBytesReadFromInputFileStream = inputFileStream.Read(dataToCompress, 0, BlockSize)) != 0)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                            {
                                gZipStream.Write(dataToCompress, 0, numberOfBytesReadFromInputFileStream);
                            }

                            //TODO: may be clear dataToCompress? 
                            var compressedData = memoryStream.ToArray();
                            var compressedDataLength = compressedData.Length;
                            outputFileStream.Write(BitConverter.GetBytes(compressedDataLength), 0, sizeof(int));
                            outputFileStream.Write(compressedData, 0, compressedDataLength);
                        }
                    }
                }
            }
        }
    }
}
