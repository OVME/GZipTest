using System;
using GZipTest.Compress;
using GZipTest.Decompress;

namespace GZipTest.Common.MultiThreading
{
    public class MultiThreadWorkerFactory : IMultiThreadWorkerFactory
    {
        public MultiThreadWorker GetWorker(OperationType operationType)
        {
            switch (operationType)
            {
                case OperationType.Compress:
                    return new MultiThreadCompressionWorker(Environment.ProcessorCount);
                case OperationType.Decompress:
                    return new MultiThreadDecompressionWorker(Environment.ProcessorCount);
                default:
                    throw new ArgumentOutOfRangeException(nameof(operationType), operationType, null);
            }
        }
    }
}
