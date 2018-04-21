using System;
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

        private static void ExecuteCommand(string command, string inputFile, string outputFile)
        {
            // TODO: if stateless compressor and decompressor are possible, mb use composition
            switch (command)
            {
                case CompressCommand:
                    var compressor = new GZipCompressor();
                    compressor.Compress(inputFile, outputFile);
                    break;
                case DecompressCommand:
                    var decompressor = new GZipDecompressor();
                    decompressor.Decompress(inputFile, outputFile);
                    break;
                default:
                    throw new ArgumentException($"Unexpected {nameof(command)} parameter value");
            }
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
