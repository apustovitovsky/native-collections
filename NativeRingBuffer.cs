using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;


public struct NativeRingBuffer<T> : INativeDisposable where T : unmanaged
{
    [NoAlias] public NativeArray<T> Buffer;
    private int position;

    public readonly int Capacity;
    public int Count { get; private set; }
    public int TotalCount { get; private set; }

    public NativeRingBuffer(int capacity, Allocator allocator)
    {
        Capacity = capacity;
        Buffer = new NativeArray<T>(capacity, allocator);
        position = -1;
        Count = 0;
        TotalCount = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Push(in T item)
    {
        position++;
        if (position == Capacity) position = 0;
        Buffer[position] = item;
        if (Count < Capacity) Count++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryPop(out T item)
    {
        if (Count == 0)
        {
            item = default;
            return false;
        }
        item = Buffer[position];
        position--;
        if (position < 0) position = Capacity - 1;
        Count--;
        return true;
    }

    public readonly bool TryPeek(out T item)
    {
        if (Count == 0)
        {
            item = default;
            return false;
        }

        item = Buffer[position];
        return true;
    }

    public readonly bool Any()
    {
        return Count != 0;
    }

    public JobHandle Dispose(JobHandle dependencies)
    {
        if (Buffer.IsCreated)
            return Buffer.Dispose(dependencies);

        return dependencies;
    }

    public void Dispose()
    {
        if (Buffer.IsCreated)
            Buffer.Dispose();
    }
}

