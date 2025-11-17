using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Jobs;


public struct NativePriorityQueue<T> : INativeDisposable
    where T : unmanaged, IComparable<T>
{
    private NativeArray<T> m_Values;
    private NativeArray<int> m_IndexToHeap;
    private NativeArray<int> m_HeapToIndex;
    private int m_Length;

    public NativePriorityQueue(int capacity, Allocator allocator)
        : this()
    {
        m_Values = new NativeArray<T>(capacity, allocator);
        m_IndexToHeap = new NativeArray<int>(capacity, allocator);
        m_HeapToIndex = new NativeArray<int>(capacity, allocator);
        m_Length = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int TryPush(int index, in T value)
    {
        int stored = m_IndexToHeap[index];
        if (stored == 0)
            return TryPushNew(index, value);

        int heapIndex = stored - 1;
        T oldValue = m_Values[index];
        m_Values[index] = value;

        if (value.CompareTo(oldValue) < 0)
            HeapifyUp(heapIndex);
        else HeapifyDown(heapIndex);

        return index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int TryPushNew(int index, in T value)
    {
        int length = m_Length;
        if (length >= m_HeapToIndex.Length)
            return -1;

        m_Values[index] = value;
        MoveItem(index, length);

        m_Length = length + 1;
        HeapifyUp(length);

        return index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int TryPop(out T value)
    {
        int length = m_Length;
        if (length == 0)
        {
            value = default;
            return -1;
        }
        m_Length = --length;

        int rootIndex = m_HeapToIndex[0];
        value = m_Values[rootIndex];

        if (length > 0)
        {
            int lastIndex = m_HeapToIndex[length];
            MoveItem(lastIndex, 0);
            HeapifyDown(0);
        }
        m_IndexToHeap[rootIndex] = 0;

        return rootIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void HeapifyUp(int heapIndex)
    {
        int index = m_HeapToIndex[heapIndex];
        T value = m_Values[index];
        int parent = (heapIndex - 1) >> 1;

        while (heapIndex > 0)
        {
            int parentIndex = m_HeapToIndex[parent];
            T parentValue = m_Values[parentIndex];

            if (value.CompareTo(parentValue) >= 0)
                break;

            MoveItem(parentIndex, heapIndex);
            heapIndex = parent;
            parent = (heapIndex - 1) >> 1;
        }
        MoveItem(index, heapIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void HeapifyDown(int heapIndex)
    {
        int index = m_HeapToIndex[heapIndex];
        T value = m_Values[index];
        int length = m_Length;

        while (true)
        {
            int left = (heapIndex << 1) + 1;
            if (left >= length) break;

            int right = left + 1;
            int smallest = left;

            int leftIndex = m_HeapToIndex[left];
            T leftValue = m_Values[leftIndex];

            if (right < length)
            {
                int rightIndex = m_HeapToIndex[right];
                T rightValue = m_Values[rightIndex];

                if (rightValue.CompareTo(leftValue) < 0)
                {
                    smallest = right;
                    leftIndex = rightIndex;
                    leftValue = rightValue;
                }
            }
            if (value.CompareTo(leftValue) <= 0)
                break;

            MoveItem(leftIndex, heapIndex);
            heapIndex = smallest;
        }
        MoveItem(index, heapIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void MoveItem(int index, int heapIndex)
    {
        m_IndexToHeap[index] = heapIndex + 1;
        m_HeapToIndex[heapIndex] = index;
    }

    public readonly bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_Length == 0;
    }

    public readonly int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_Length;
    }

    public void Dispose()
    {
        if (m_IndexToHeap.IsCreated)
            m_IndexToHeap.Dispose();

        if (m_HeapToIndex.IsCreated)
            m_HeapToIndex.Dispose();

        if (m_Values.IsCreated)
            m_Values.Dispose();
    }

    public JobHandle Dispose(JobHandle inputDeps)
    {
        return JobHandle.CombineDependencies(
            m_Values.Dispose(inputDeps),
            m_HeapToIndex.Dispose(inputDeps),
            m_IndexToHeap.Dispose(inputDeps)
        );
    }
}
