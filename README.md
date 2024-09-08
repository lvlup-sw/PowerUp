# PowerUp

PowerUp offers a curated selection of essential utilities and extensions designed to extend common functionality and simplify complex operations.

## Features

* **Sorting Algorithms:** Efficient implementations of various sorting algorithms for quick and easy data organization.
* **Cache Key Hash Generator:** Generate unique hash codes for cache keys, ensuring optimal cache performance.
* **Fast In-Memory Cache:** A high-performance in-memory caching solution to improve application responsiveness.

## Getting Started

1. **Install the NuGet package:**

   ```bash
   Install-Package lvlup.PowerUp
   ```

2. **Usage:**
Here are some examples of how to use "PowerUp" in your C# projects:

```csharp
using PowerUp;

// Sorting an array using QuickSort
List<long> numbers = [5, 2, 9, 1, 7];
numbers.QuickSort();

// Generating a cache key hash
MyDataModel data = new();
string key = "myCacheKey";
string hash = CacheKeyGenerator.GenerateCacheKey(data, key);

// Using the in-memory cache
var cache = new FastMemCache<int, int>();
cache.AddOrUpdate(42, 42, TimeSpan.FromMilliseconds(100));
int cachedValue = cache.TryGet(42, out i);
```

## License

PowerUp is licensed under the [MIT License](https://opensource.org/licenses/MIT).