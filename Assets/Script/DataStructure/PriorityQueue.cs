using System;
using System.Collections.Generic;

namespace Script.DataStructure
{
    public class PriorityQueue<T>
    {
        private readonly List<T> _data;
        private readonly Comparison<T> _comparison;

        public int Count => _data.Count;

        public PriorityQueue(Comparison<T> comparison)
        {
            this._data = new List<T>();
            this._comparison = comparison ?? Comparer<T>.Default.Compare;
        }

        public void Enqueue(T item)
        {
            _data.Add(item);
            int childIndex = _data.Count - 1;
            while (childIndex > 0)
            {
                int parentIndex = (childIndex - 1) / 2;
                if (_comparison(_data[childIndex], _data[parentIndex]) >= 0) break;
                Swap(childIndex, parentIndex);
                childIndex = parentIndex;
            }
        }

        public T Dequeue()
        {
            int lastIndex = _data.Count - 1;
            T frontItem = _data[0];
            _data[0] = _data[lastIndex];
            _data.RemoveAt(lastIndex);

            int parentIndex = 0;
            while (true)
            {
                int leftChildIndex = parentIndex * 2 + 1;
                if (leftChildIndex >= _data.Count) break;

                int rightChildIndex = leftChildIndex + 1;
                int minIndex = (rightChildIndex < _data.Count && 
                                _comparison(_data[rightChildIndex], _data[leftChildIndex]) < 0) 
                    ? rightChildIndex : leftChildIndex;

                if (_comparison(_data[parentIndex], _data[minIndex]) <= 0) break;
                Swap(parentIndex, minIndex);
                parentIndex = minIndex;
            }
            return frontItem;
        }

        public bool Contains(T item) => _data.Contains(item);
    
        public void Remove(T item)
        {
            int index = _data.IndexOf(item);
            if (index == -1) return;

            int lastIndex = _data.Count - 1;
            _data[index] = _data[lastIndex];
            _data.RemoveAt(lastIndex);

            // Rebuild heap from the removed index
            Heapify(index);
        }

        public T Peek()
        {
            if (_data.Count == 0)
                throw new InvalidOperationException("Queue is empty");
            return _data[0];
        }

        private void Heapify(int index)
        {
            int smallest = index;
            int left = 2 * index + 1;
            int right = 2 * index + 2;

            if (left < _data.Count && _comparison(_data[left], _data[smallest]) < 0)
                smallest = left;
            if (right < _data.Count && _comparison(_data[right], _data[smallest]) < 0)
                smallest = right;

            if (smallest != index)
            {
                Swap(index, smallest);
                Heapify(smallest);
            }
        }

        private void Swap(int a, int b)
        {
            T temp = _data[a];
            _data[a] = _data[b];
            _data[b] = temp;
        }
    }
}