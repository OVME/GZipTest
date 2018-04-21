using System;

namespace GZipTest
{
    internal class GZipTestPublicException : Exception
    {
        public GZipTestPublicException()
        {
        }

        public GZipTestPublicException(string message) : base(message)
        {
        }
    }
}
