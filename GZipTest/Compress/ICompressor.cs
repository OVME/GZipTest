namespace GZipTest.Compress
{
    internal interface ICompressor
    {
        void Compress(string inputFileName, string outputArchiveName);
    }
}
