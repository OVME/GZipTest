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
            
            var result = worker.Work(new MultiThreadWorkerParameters(inputFileInfo, outputFileInfo));

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
                // There are several possible solutions for case when we are getting public and private errors both.
                // I will show public error, so user, may be will be able to fix problem that he can.
                // May be it have sense to create new GZipTestCombinedException type with separated properties 
                // containing private errros and public errors, so private can be logged and public can be shown.
                // Or private exception can be thrown so errors will be only logged.
                case OperationResultType.BothErrors:
                    var combinedErrorMessage = string.Join(";\n", result.PublicErrors.Distinct());
                    throw new GZipTestPublicException(combinedErrorMessage);
                default:
                    throw new ArgumentOutOfRangeException($"Unknown value: {result.OperationResultType}");
            }
        }
    }
}
