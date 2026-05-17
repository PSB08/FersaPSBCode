using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Scripts.Enemies.Astar
{
    public class PriorityQueue<T> where T : IComparable<T>
    {
        public List<T> heap = new List<T>();
        
        public int Count => heap.Count;

        public void Clear()
        {
            heap?.Clear();
        }

        public T Contains(T key)
        {
            int idx = heap.IndexOf(key);
            if(idx < 0) return default;
            return heap[idx];
        }

        public void Push(T data)
        {
            heap.Add(data); 
            
            int now = heap.Count - 1; 
            while (now > 0)
            {
                int next = (now - 1) / 2; 
                if(heap[now].CompareTo(heap[next]) < 0) 
                {
                    break; 
                }
                
                (heap[now], heap[next]) = (heap[next], heap[now]);
                now = next;
            }
        }

        public T Pop()
        {
            T ret = heap[0];
            int lastIdx = heap.Count - 1;
            heap[0] = heap[lastIdx];
            heap.RemoveAt(lastIdx);
            lastIdx--;

            int now = 0;
            while (true)
            {
                int left = now * 2 + 1;
                int right = now * 2 + 2;

                int next = now;
                
                if (left <= lastIdx && heap[next].CompareTo(heap[left]) < 0) 
                    next = left;
                if (right <= lastIdx && heap[next].CompareTo(heap[right]) < 0) 
                    next = right;

                if (next == now)
                    break;
                
                (heap[now], heap[next]) = (heap[next], heap[now]);
                now = next;
            }
            
            return ret;
        }

        public T Peek()
        {
            return heap.Count == 0 ? default : heap[0];
        }
        
    }
}