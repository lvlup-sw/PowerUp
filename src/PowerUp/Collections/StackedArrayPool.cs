using System.Buffers;
using System.Collections.Concurrent;

namespace PowerUp.Collections
{
    public class StackArrayPool<T> : ArrayPool<T>
    {
        private const int MinPowOf2 = 4;
        private const int MaxPowOf2 = 30;
    
        [ThreadStatic]
        private static T[][] t_threadLocalCache;
    
        private readonly ConcurrentDictionary<int, ConcurrentQueue<T[]>> _buckets;
        private readonly int[] _bucketSizes;
    
        public StackArrayPool(int preallocate = 0)
        {
            // Generate bucket sizes using Enumerable.Range.Select
            _bucketSizes = Enumerable.Range(MinPowOf2, MaxPowOf2 - MinPowOf2 + 1)
                .Select(i => (int)Math.Pow(2, i))
                .ToArray();
    
            _buckets = new ConcurrentDictionary<int, ConcurrentQueue<T[]>>();
    
            // Preallocate buckets
            for (int i = 0; i < _bucketSizes.Length; i++)
            {
                var queue = new ConcurrentQueue<T[]>();
                for (int j = 0; j < preallocate; j++)
                {
                    queue.Enqueue(new T[_bucketSizes[i]]);
                }
                _buckets[_bucketSizes[i]] = queue;
            }
        }
    
        public override T[] Rent(int minimumLength)
        {
            var bucketIndex = GetBucketIndex(minimumLength);
            if (bucketIndex < 0 || bucketIndex >= _bucketSizes.Length)
            {
                return new T[minimumLength];
            }
    
            // Initialize thread-local cache if it's not already initialized
            if (t_threadLocalCache == null)
            {
                t_threadLocalCache = new T[_bucketSizes.Length][];
            }
    
            // Try to get from thread-local storage first
            var localArray = t_threadLocalCache[bucketIndex];
            if (localArray != null && localArray.Length >= minimumLength)
            {
                t_threadLocalCache[bucketIndex] = null;
                return localArray;
            }
    
            // Next, try to get an array from one of the per-core stacks
            if (_buckets.TryGetValue(_bucketSizes[bucketIndex], out var queue) && queue.TryDequeue(out var array))
            {
                return array;
            }
    
            // If no suitable array is found, allocate a new one
            return new T[_bucketSizes[bucketIndex]];
        }
    
        public override void Return(T[] array, bool clearArray = false)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
    
            if (clearArray)
            {
                Array.Clear(array, 0, array.Length);
            }
    
            var bucketIndex = GetBucketIndex(array.Length);
            if (bucketIndex < 0 || bucketIndex >= _bucketSizes.Length)
            {
                return; // Array is too large or too small, discard it
            }
    
            // Try to store in thread-local storage first
            if (t_threadLocalCache[bucketIndex] == null)
            {
                t_threadLocalCache[bucketIndex] = array;
                return;
            }
    
            _buckets[_bucketSizes[bucketIndex]].Enqueue(array);
        }
    
        private int GetBucketIndex(int length)
        {
            // Since the bucket sizes are powers of 2, we can use log2 to find the appropriate bucket index
            return (int)Math.Log(length, 2) - MinPowOf2;
        }
    }
}
