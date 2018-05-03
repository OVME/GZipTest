using System;
using System.Collections.Generic;
using System.Threading;

namespace GZipTest.Common.MultiThreading
{
    public abstract class MultiThreadWorker
    {
        private bool _workerIsUsed = false;
        private volatile bool _hasPrivateError = false;
        private volatile bool _hasPublicError = false;
        private readonly List<string> _privateErrors = new List<string>();
        private readonly List<string> _publicErrors = new List<string>();

        // I could use _privateErrors.Any() for those properties and not use _hasPrivateError field at all.
        // I'm not sure which option is better. It's not really important how fast thread will get an actual error appearance information in this case.
        public bool HasPrivateError => _hasPrivateError;
        public bool HasPublicError => _hasPublicError;

        private readonly int _processorsCount;

        private readonly object _privateErrorWritingLocker = new object();
        private readonly object _publicErrorWritingLocker = new object();

        protected MultiThreadWorker(int processorsCount)
        {
            _processorsCount = processorsCount;
        }

        public OperationResult Work(MultiThreadWorkerParameters parameters)
        {
            if (_workerIsUsed)
            {
                throw new InvalidOperationException("Worker can not be run twice.");
            }

            WorkInternal(parameters);

            _workerIsUsed = true;

            return GetWorkResult();
        }

        protected abstract void WorkInternal(MultiThreadWorkerParameters parameters);

        protected Thread[] GetThreads(ParameterizedThreadStart threadJob)
        {
            var threads = new Thread[_processorsCount];

            for (var i = 0; i < _processorsCount; i++)
            {
                var thread = new Thread(threadJob);
                threads[i] = thread;
            }

            return threads;
        }

        protected void WritePublicError(string errorText)
        {
            lock (_publicErrorWritingLocker)
            {
                _hasPublicError = true;
                _publicErrors.Add(errorText);
            }
        }

        protected void WritePrivateError(string errorText)
        {
            lock (_privateErrorWritingLocker)
            {
                _hasPrivateError = true;
                _privateErrors.Add(errorText);
            }
        }

        private OperationResult GetWorkResult()
        {
            if (!_hasPrivateError && !_hasPublicError)
            {
                return new OperationResult(OperationResultType.Success);
            }

            OperationResultType resultType = 0;

            if (_hasPrivateError)
            {
                resultType |= OperationResultType.PrivateError;
            }

            if (_hasPublicError)
            {
                resultType |= OperationResultType.PublicError;
            }

            return new OperationResult(resultType, _privateErrors, _publicErrors);
        }
    }
}
