using System;
using System.IO;
using System.Linq;
using GZipTest.Common;

namespace GZipTest.Compress
{
    internal class GZipCompressor : ICompressor
    {
        public void Compress(string inputFileName, string outputArchiveName)
        {
            // TODO: place to constructor
            var fileInfoProvider = new FileInfoProvider();

            var inputFileInfo = fileInfoProvider.GetInputFileInfo(inputFileName);

            var outputArchiveFileInfo = fileInfoProvider.GetOutputFileInfo(outputArchiveName);
            
            CompressInternal(inputFileInfo, outputArchiveFileInfo);
        }

        private void CompressInternal(FileInfo inputFileInfo, FileInfo outputArchiveFileInfo)
        {
            var compressionWorker = new MultiThreadCompressionWorker(Environment.ProcessorCount);
            OperationResult result;

            try
            {
                using (var inputFileStream = inputFileInfo.OpenRead())
                {
                    try
                    {
                        using (var outputFileStream = outputArchiveFileInfo.Create())
                        {
                            result = compressionWorker.Compress(inputFileStream, outputFileStream);
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        throw new GZipTestPublicException($"{outputArchiveFileInfo.FullName}: have no permissiom to create file");
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                throw new GZipTestPublicException($"{inputFileInfo.FullName}: can not get access to file");
            }

            HandleResult(result, outputArchiveFileInfo);
        }

        // GZipDecompressor contains this method as well. 
        // It is not placed to base class because Compressor and Decompressor have different purpose, so they can't have base class.
        // These methods are equal just because it is test project and for simplicity I created one OperationResult class.
        // In serious project Compressor and Decompressor should get different results, one class for each operation,
        // containing more detailed specific data for compression and decompression.
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
                    // There are several possible solutions for case when we are getting public and private errors both.
                    // I will show public error, so user, may be will be able to fix problem that he can.
                    // May be it have sense to create new GZipTestCombinedException type with separated properties 
                    // containing private errros and public errors, so private can be logged and public can be shown.
                    // Or private exception can be thrown so errors will be only logged.    
                    var combinedErrorMessage = string.Join(";\n", result.PublicErrors.Distinct());
                    throw new GZipTestPublicException(combinedErrorMessage);
                default:
                    throw new ArgumentOutOfRangeException($"Unknown value: {result.OperationResultType}");
            }
        }
    }
}
