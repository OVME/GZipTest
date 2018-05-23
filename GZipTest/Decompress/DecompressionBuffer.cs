using System.Collections.Generic;
using System.Linq;

namespace GZipTest.Decompress
{
    public class DecompressionBuffer<T> where T : class
    {
        private readonly int _bufferSize;

        private readonly int _edgeNumberOfItems;

        private readonly object _pushPullLocker = new object();

        private readonly List<T> _itemsList;

        public DecompressionBuffer(int bufferSize)
        {
            _bufferSize = bufferSize;
            _itemsList = new List<T>(_bufferSize);
            _edgeNumberOfItems = _bufferSize / 2;
        }

        public void Push(T item)
        {
            lock (_pushPullLocker)
            {
                _itemsList.Add(item);
            }
        }

        public T Pull()
        {
            lock (_pushPullLocker)
            {
                var item = _itemsList.FirstOrDefault();
                if (item != null)
                {
                    _itemsList.Remove(item);
                }

                return item;
            }
        }

        public bool NeedFilling()
        {
            return _itemsList.Count <= _edgeNumberOfItems;
        }

        public bool IsFull()
        {
            return _itemsList.Count >= _bufferSize;
        }
    }
}
