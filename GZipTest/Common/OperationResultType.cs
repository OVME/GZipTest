using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
