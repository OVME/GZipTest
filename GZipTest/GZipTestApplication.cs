using System;
using GZipTest.Common;

namespace GZipTest
{
    public class GZipTestApplication : IGZipTestApplication
    {
        private const string CompressCommand = "compress";
        private const string DecompressCommand = "decompress";

        private readonly ICommandProcessor _commandProcessor;

        public GZipTestApplication(ICommandProcessor commandProcessor)
        {
            _commandProcessor = commandProcessor;
        }

        public void Run(string[] args)
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
            catch (Exception e)
            {
                // Logger code should be here
                WriteUnexpectedErrorMessageToConsole();
            }
        }

        private void ExecuteCommand(string commandString, string inputFile, string outputFile)
        {
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
                    throw new ArgumentException($"Unexpected {nameof(commandString)} parameter value");
            }

            _commandProcessor.Process(command);
        }

        private void WriteUnexpectedErrorMessageToConsole()
        {
            Console.Error.WriteLine("An unexpected error occured. Application will be closed.");
        }

        private void WritePublicExceptionToConsole(GZipTestPublicException e)
        {
            Console.Error.WriteLine(e.Message);
        }

        private void ValidateParameters(string[] args)
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
