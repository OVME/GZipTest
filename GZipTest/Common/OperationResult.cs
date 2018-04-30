using System.Collections.Generic;

namespace GZipTest.Common
{
    public class OperationResult
    {
        public OperationResult(OperationResultType operationResultType, ICollection<string> privateErrors = null, ICollection<string> publicErrors = null)
        {
            OperationResultType = operationResultType;
            PrivateErrors = privateErrors;
            PublicErrors = publicErrors;
        }

        public OperationResultType OperationResultType { get; }

        public ICollection<string> PrivateErrors { get; }

        public ICollection<string> PublicErrors { get; }
    }
}
