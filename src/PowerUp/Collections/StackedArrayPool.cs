using System.Buffers;
using System.Collections.Concurrent;

namespace PowerUp.Collections
{
    public class StackedArrayPool<T> : ArrayPool<T>
    {
        [ThreadStatic]
        private static T[]? t_tlsArray;
        private readonly ConcurrentDictionary<int, ConcurrentQueue<T[]>> _buckets =
            new ConcurrentDictionary<int, ConcurrentQueue<T[]>>();

        private readonly int _maxPoolSize;
        private readonly int[] _bucketSizes;

        public StackedArrayPool(int maxPoolSize = 1000, int maxArrayLength = 1024 * 1024)
        {
            _maxPoolSize = maxPoolSize;

            // Generate bucket sizes using Enumerable.Range.Select
            _bucketSizes = Enumerable.Range(0, 30)
                .Select(i => (int)Math.Pow(2, i))
                .Where(size => size <= maxArrayLength)
                .ToArray();
        }

        public override T[] Rent(int minimumLength)
        {
            // Try to get from thread-local storage first
            if (t_tlsArray != null && t_tlsArray.Length >= minimumLength)
            {
                var array = t_tlsArray;
                t_tlsArray = null;
                return array;
            }

            var bucketIndex = Array.BinarySearch(_bucketSizes, minimumLength);
            if (bucketIndex < 0)
            {
                bucketIndex = ~bucketIndex; // Get the next larger bucket size
            }

            // Next, try to get an array from one of the per-core stacks
            if (bucketIndex < _bucketSizes.Length)
            {
                var bucketSize = _bucketSizes[bucketIndex];
                if (_buckets.TryGetValue(bucketSize, out var queue) && queue.TryDequeue(out var array))
                {
                    return array;
                }
            }

            // If no suitable array is found, allocate a new one
            return new T[minimumLength];
        }

        public override void Return(T[] array, bool clearArray = false)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (clearArray)
            {
                Array.Clear(array, 0, array.Length);
            }

            // Try to store in thread-local storage first
            if (t_tlsArray == null)
            {
                t_tlsArray = array;
                return;
            }

            var bucketIndex = Array.BinarySearch(_bucketSizes, array.Length);
            if (bucketIndex < 0)
            {
                return; // Array is too large or too small, discard it
            }

            var bucketSize = _bucketSizes[bucketIndex];
            _buckets.GetOrAdd(bucketSize, _ => new ConcurrentQueue<T[]>()).Enqueue(array);
        }
    }
}
