namespace GZipTest.Decompress
{
    internal interface IDecompressor
    {
        void Decompress(string inputArchiveName, string outputFileName);
    }
}
