using System;

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
            }
            catch (Exception e)
            {
                WriteExceptionToConsole(e);
                return;
            }
        }

        private static void WriteExceptionToConsole(Exception e)
        {
            Console.Error.WriteLine(e.Message);
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void ValidateParameters(string[] args)
        {
            if (args.Length != 3)
            {
                throw new ArgumentException("Invalid parameters number");
            }

            if (args[0] != CompressCommand && args[0] != DecompressCommand)
            {
                throw new ArgumentException($"Invalid command parameter. Should be {CompressCommand} or {DecompressCommand}");
            }
        }
    }
}
