using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
            var decompressionWorker = new MultiThreadDecompressionWorker(Environment.ProcessorCount);
            OperationResult result;

            try
            {
                using (var inputArchiveFileStream = inputArchiveInfo.OpenRead())
                {
                    try
                    {
                        using (var outputFileStream = outputFileInfo.Create())
                        {
                            result = decompressionWorker.Decompress(inputArchiveFileStream, outputFileStream);
                        }
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        throw new GZipTestPublicException($"{outputFileInfo.FullName}: have no permissiom to create file");
                    }
                }
            }
            catch (UnauthorizedAccessException e)
            {
                throw new GZipTestPublicException($"{inputArchiveInfo.FullName}: can not get access to archive file");
            }

            HandleResult(result, outputFileInfo);
        }

        private void HandleResult(OperationResult result, FileInfo outputFileInfo)
        {
            if (result.OperationResultType.HasFlag(OperationResultType.PublicError) ||
                result.OperationResultType.HasFlag(OperationResultType.PrivateError))
            {
                outputFileInfo.Delete();
            }

            switch (result.OperationResultType)
            {
                case OperationResultType.Success:
                    break;
                case OperationResultType.PrivateError:
                    var privateErrorMessage = string.Join(";\n", result.PrivateErrors.Distinct());
                    throw new GZipTestException(privateErrorMessage);
                case OperationResultType.PublicError:
                    var publicErrorMessage = string.Join(";\n", result.PublicErrors.Distinct());
                    throw new GZipTestPublicException(publicErrorMessage);
                case OperationResultType.BothErrors:
                    var combinedErrorMessage = string.Join(";\n", result.PublicErrors.Distinct());
                    throw new GZipTestPublicException(combinedErrorMessage);
                default:
                    throw new ArgumentOutOfRangeException($"Unknown value: {result.OperationResultType}");
            }
        }
    }
}
