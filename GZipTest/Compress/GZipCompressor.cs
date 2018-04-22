using System;
using System.IO;
using System.IO.Compression;

namespace GZipTest.Compress
{
    internal class GZipCompressor : ICompressor
    {
        private const int BlockSize = 1048576;

        public void Compress(string inputFileName, string outputArchiveName)
        {
            FileInfo inputFileInfo;

            try
            {
                inputFileInfo = new FileInfo(inputFileName);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new GZipTestPublicException($"{inputFileName}: cannot get access to file.");
            }

            if (!inputFileInfo.Exists)
            {
                throw new GZipTestPublicException($"{inputFileName}: file does not exist.");
            }

            // TODO: handle write file unauthorized access exception and case when file with same name exist
            using (var inputFileStream = inputFileInfo.OpenRead())
            {
                using (var outputFileStream = new FileStream(outputArchiveName, FileMode.Create))
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
