using System.Collections.Generic;

namespace AdvancedSafety
{
    public class PriorityQueue<T>
    {
        private readonly List<T> myBackingStorage;
        private readonly IComparer<T> myComparer;

        public PriorityQueue(IComparer<T> comparer)
        {
            myComparer = comparer;
            myBackingStorage = new List<T>();
        }

        public void Enqueue(T value)
        {
            myBackingStorage.Add(value);
            SiftUp(myBackingStorage.Count - 1);
        }
        
        public T Dequeue()
        {
            if (myBackingStorage.Count == 0)
                return default(T);
            Swap(0, myBackingStorage.Count - 1);
            var result = myBackingStorage[myBackingStorage.Count - 1];
            myBackingStorage.RemoveAt(myBackingStorage.Count - 1);
            SiftDown(0);
            return result;
        }

        public T Peek()
        {
            if (myBackingStorage.Count == 0)
                return default(T);
            return myBackingStorage[0];
        }

        public int Count => myBackingStorage.Count;

        private void Swap(int i1, int i2)
        {
            var value1 = myBackingStorage[i1];
            var value2 = myBackingStorage[i2];
            myBackingStorage[i1] = value2;
            myBackingStorage[i2] = value1;
        }

        private void SiftDown(int i)
        {
            var childIndex1 = i * 2 + 1;
            var childIndex2 = i * 2 + 2;
            if (childIndex1 >= myBackingStorage.Count)
                return;
            var child1 = myBackingStorage[childIndex1];
            if (childIndex2 >= myBackingStorage.Count)
            {
                var compared = myComparer.Compare(myBackingStorage[i], child1);
                if (compared > 0)
                {
                    Swap(i, childIndex1);
                }
                return;
            }
            var child2 = myBackingStorage[childIndex2];
            var compared1 = myComparer.Compare(myBackingStorage[i], child1);
            var compared2 = myComparer.Compare(myBackingStorage[i], child2);
            if (compared1 > 0 || compared2 > 0)
            {
                var compared12 = myComparer.Compare(child1, child2);
                if (compared12 > 0)
                {
                    Swap(i, childIndex2);
                    SiftDown(childIndex2);
                }
                else
                {
                    Swap(i, childIndex1);
                    SiftDown(childIndex1);
                }
            }
        }

        private void SiftUp(int i)
        {
            if (i == 0)
                return;
            var parentIndex = (i - 1) / 2;
            var compared = myComparer.Compare(myBackingStorage[i], myBackingStorage[parentIndex]);
            if (compared < 0)
            {
                Swap(i, parentIndex);
                SiftUp(parentIndex);
            }
        }
    }
}
