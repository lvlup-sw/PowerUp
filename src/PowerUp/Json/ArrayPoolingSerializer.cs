using System.Text.Json;
using PowerUp.Collections;
using PowerUp.Json.Interfaces;

namespace PowerUp.Json
{
    public class ArrayPoolingSerializer<T> : IArrayPoolingSerializer<T> where T : class
    {
        private readonly StackArrayPool<byte> _arrayPool;
        private const int InitialBufferSize = 1024;

        public JsonDeserializerWithPooling(StackArrayPool<byte> arrayPool)
        {
            _arrayPool = arrayPool;
        }

        public async Task<T?> DeserializeAsync<T>(byte[] rawData)
        {
            int bufferSize = InitialBufferSize;
            byte[] buffer = _arrayPool.Rent(bufferSize);

            try
            {
                int dataLength = rawData.Length;

                int offset = 0;
                while (offset < dataLength)
                {
                    int bytesToRead = Math.Min(bufferSize, dataLength - offset);

                    // Copy the relevant portion of rawData into the buffer
                    Array.Copy(rawData, offset, buffer, 0, bytesToRead);

                    var jsonReaderOptions = new JsonReaderOptions { AllowTrailingCommas = true };
                    Utf8JsonReader reader = new Utf8JsonReader(new ReadOnlySpan<byte>(buffer, 0, bytesToRead), jsonReaderOptions);

                    if (JsonSerializer.Deserialize<T>(ref reader) is T result)
                    {
                        return result;
                    }

                    // Adaptive buffer resizing
                    _arrayPool.Return(buffer);
                    bufferSize *= 2;
                    buffer = _arrayPool.Rent(bufferSize);
                    offset += bytesToRead;
                }

                // Handle deserialization failure
                return default;
            }
            finally
            {
                _arrayPool.Return(buffer);
            }
        }
    }
}
