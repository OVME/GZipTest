namespace GZipTest.Common.MultiThreading
{
    public interface IMultiThreadWorkerFactory
    {
        MultiThreadWorker GetWorker(OperationType operationType);
    }
}
