using System;

namespace GZipTest.Common
{
    [Flags]
    public enum OperationResultType
    {
        Success = 1,
        PrivateError = 2,
        PublicError = 4,
        BothErrors = PrivateError | PublicError
    }
}
