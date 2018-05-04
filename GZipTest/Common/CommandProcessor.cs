using System;
using System.IO;
using System.Linq;
using GZipTest.Common.MultiThreading;

namespace GZipTest.Common
{
    public class CommandProcessor : ICommandProcessor
    {
        private readonly IFileInfoProvider _fileInfoProvider;
        private readonly IMultiThreadWorkerFactory _workerFactory;

        public CommandProcessor(IFileInfoProvider fileInfoProvider, IMultiThreadWorkerFactory workerFactory)
        {
            _fileInfoProvider = fileInfoProvider;
            _workerFactory = workerFactory;
        }

        public void Process(Command command)
        {
            var inputFileName = command.InputFileName;
            var outputFileName = command.OutputFileName;

            var inputFileInfo = _fileInfoProvider.GetInputFileInfo(inputFileName);

            var outputArchiveFileInfo = _fileInfoProvider.GetOutputFileInfo(outputFileName);

            ProcessInternal(command.OperationType, inputFileInfo, outputArchiveFileInfo);
        }

        private void ProcessInternal(OperationType operationType, FileInfo inputFileInfo, FileInfo outputFileInfo)
        {
            var worker = _workerFactory.GetWorker(operationType);
            OperationResult result;

            try
            {
                using (var inputFileStream = inputFileInfo.OpenRead())
                {
                    try
                    {
                        using (var outputFileStream = outputFileInfo.Create())
                        {
                            result = worker.Work(new MultiThreadWorkerParameters(inputFileStream, outputFileStream));
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        throw new GZipTestPublicException($"{outputFileInfo.FullName}: have no permissiom to create file");
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                throw new GZipTestPublicException($"{inputFileInfo.FullName}: can not get access to file");
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
