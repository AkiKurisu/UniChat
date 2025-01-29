using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Sentis;
namespace UniChat
{
    public static class XXHash
    {
        private const uint PRIME32_1 = 2654435761U;
        
        private const uint PRIME32_2 = 2246822519U;
        
        private const uint PRIME32_3 = 3266489917U;
        
        private const uint PRIME32_4 = 668265263U;
        
        private const uint PRIME32_5 = 374761393U;
        
        public static uint CalculateHash(TensorFloat tensorFloat, int d1 = 0)
        {
            tensorFloat.MakeReadable();
            int length = tensorFloat.shape[1];
            var buffer = new NativeArray<float>(length, Allocator.Temp);
            for (int j = 0; j < length - 1; ++j)
            {
                buffer[j] = tensorFloat[d1, j];
            }
            uint hash = CalculateHash(buffer);
            buffer.Dispose();
            return hash;
        }
        
        public static uint CalculateHash<T>(NativeArray<T> data) where T : struct
        {
            return CalculateHash(data.Reinterpret<byte>(UnsafeUtility.SizeOf<T>()));
        }
        
        public static uint CalculateHash(string input)
        {
            var bytes = new FixedString4096Bytes(input).AsFixedList().ToNativeArray(Allocator.Temp);
            //Must aligned to *4
            int remainder = bytes.Length % 4;
            if (remainder != 0)
            {
                var slice1 = new NativeSlice<byte>(bytes);
                NativeArray<byte> newArray = new(bytes.Length + 4 - remainder, Allocator.Temp);
                var slice2 = new NativeSlice<byte>(newArray, 0, bytes.Length);
                slice2.CopyFrom(slice1);
                bytes.Dispose();
                bytes = newArray;
            }
            try
            {
                return CalculateHash(bytes);
            }
            finally
            {
                bytes.Dispose();
            }
        }
        
        [BurstCompile]
        public static uint CalculateHash(NativeArray<byte> data)
        {
            uint seed = 0;
            uint hash = seed + PRIME32_5;
            int currentIndex = 0;
            int remainingBytes = data.Length;
            while (remainingBytes >= 4)
            {
                uint currentUint = data.Reinterpret<uint>(UnsafeUtility.SizeOf<byte>())[currentIndex / 4];
                currentUint *= PRIME32_3;
                currentUint = RotateLeft(currentUint, 17) * PRIME32_4;
                hash ^= currentUint;
                hash = RotateLeft(hash, 19);
                hash = hash * PRIME32_1 + PRIME32_4;
                currentIndex += 4;
                remainingBytes -= 4;
            }

            while (remainingBytes > 0)
            {
                hash ^= data[currentIndex] * PRIME32_5;
                hash = RotateLeft(hash, 11) * PRIME32_1;
                currentIndex++;
                remainingBytes--;
            }

            hash ^= (uint)data.Length;
            hash ^= hash >> 15;
            hash *= PRIME32_2;
            hash ^= hash >> 13;
            hash *= PRIME32_3;
            hash ^= hash >> 16;

            return hash;
        }

        private static uint RotateLeft(uint value, int count)
        {
            return (value << count) | (value >> (32 - count));
        }
    }

}