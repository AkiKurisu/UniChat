using Unity.Collections;
namespace UniChat
{
    public static class NativeExtensions
    {
        public static void DisposeSafe<T>(ref this NativeArray<T> array) where T : unmanaged
        {
            if (array.IsCreated)
                array.Dispose();
        }
        public static void DisposeSafe<T>(ref this NativeList<T> array) where T : unmanaged
        {
            if (array.IsCreated)
                array.Dispose();
        }
        public static void Resize<T>(ref this NativeArray<T> array, int size, Allocator allocator = Allocator.Persistent, NativeArrayOptions options = NativeArrayOptions.ClearMemory) where T : unmanaged
        {
            if (array.IsCreated == false || array.Length < size)
            {
                array.DisposeSafe();
                array = new NativeArray<T>(size, allocator, options);
            }
        }
    }
}
