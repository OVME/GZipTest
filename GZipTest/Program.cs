using System;
using GZipTest.Common;
using GZipTest.Common.MultiThreading;
using GZipTest.Compress;
using GZipTest.Decompress;

namespace GZipTest
{
    public class Program
    {
        private const string CompressCommand = "compress";
        private const string DecompressCommand = "decompress";

        static void Main(string[] args)
        {
            try
            {
                ValidateParameters(args);

                var command = args[0];
                var inputFile = args[1];
                var outputFile = args[2];

                ExecuteCommand(command, inputFile, outputFile);
            }
            catch (GZipTestPublicException e)
            {
                WritePublicExceptionToConsole(e);
            }
            catch (Exception)
            {
                // Logger code should be here
                WriteUnexpectedErrorMessageToConsole();
            }
        }

        private static void ExecuteCommand(string commandString, string inputFile, string outputFile)
        {
            IFileInfoProvider fileInfoProvider = new FileInfoProvider();
            IMultiThreadWorkerFactory workerFactory = new MultiThreadWorkerFactory();
            ICommandProcessor commandProcessor = new CommandProcessor(fileInfoProvider, workerFactory);
            var command = new Command { InputFileName = inputFile, OutputFileName = outputFile };
            switch (commandString)
            {
                case CompressCommand:
                    command.OperationType = OperationType.Compress;
                    break;
                case DecompressCommand:
                    command.OperationType = OperationType.Decompress;
                    break;
                default:
                    throw new ArgumentException($"Unexpected {nameof(command)} parameter value");
            }

            commandProcessor.Process(command);
        }

        private static void WriteUnexpectedErrorMessageToConsole()
        {
            Console.Error.WriteLine("An unexpected error occured. Application will be closed.");
        }

        private static void WritePublicExceptionToConsole(GZipTestPublicException e)
        {
            Console.Error.WriteLine(e.Message);
        }

        private static void ValidateParameters(string[] args)
        {
            if (args.Length != 3)
            {
                throw new GZipTestPublicException("Invalid parameters number");
            }

            if (args[0] != CompressCommand && args[0] != DecompressCommand)
            {
                throw new GZipTestPublicException($"Invalid command parameter. Should be {CompressCommand} or {DecompressCommand}");
            }
        }
    }
}
