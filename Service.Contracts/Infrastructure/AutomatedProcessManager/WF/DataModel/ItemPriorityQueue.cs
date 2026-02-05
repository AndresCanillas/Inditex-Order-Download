using Service.Contracts.WF;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Service.Contracts.WF
{
    class ItemPriorityQueue : IReadOnlyCollection<ItemData>
    {
        private readonly object syncObj = new object();
        private readonly LinkedList<ItemData> highestPriorityQueue;
        private readonly LinkedList<ItemData> highPriorityQueue;
        private readonly LinkedList<ItemData> normalPriorityQueue;

        public ItemPriorityQueue()
        {
            highestPriorityQueue = new LinkedList<ItemData>();
            highPriorityQueue = new LinkedList<ItemData>();
            normalPriorityQueue = new LinkedList<ItemData>();
        }

        public int Count
        {
            get
            {
                lock(syncObj)
                {
                    return highestPriorityQueue.Count + highPriorityQueue.Count + normalPriorityQueue.Count;
                }
            }
        }

        public void Enqueue(IEnumerable<ItemData> items)
        {
            lock(syncObj)
            {
                foreach(var item in items)
                    InternalEnqueue(item);
            }
        }

        public void Enqueue(ItemData item)
        {
            lock(syncObj)
            {
                InternalEnqueue(item);
            }
        }

        private void InternalEnqueue(ItemData item)
        {
            switch(item.ItemPriority)
            {
                case ItemPriority.Highest:
                    highestPriorityQueue.AddLast(item);
                    break;
                case ItemPriority.High:
                    highPriorityQueue.AddLast(item);
                    break;
                default:
                    normalPriorityQueue.AddLast(item);
                    break;
            }
        }


        // NOTE: The objective of ReEnqueue is to put the item behind other items in the execution queue.
        // This is part of a "collaborative" strategy implemented in some tasks that are known to take a
        // long time to execute when the order file is large; in these cases ReEnqueue is used to avoid
        // one single item from taking over a task, blocking other (smaller/faster) items from advancing
        // in the workflow.
        // For this to be effective, ReEnqueue must make sure to put the item behind other items, this is
        // important when the item has a high/highest priority, as we can easily end making the item
        // the next in line for execution, defeating the purpose of using ReEnqueue in the first place.
        public void ReEnqueue(ItemData item)
        {
            if(item.ItemPriority == ItemPriority.Highest && highestPriorityQueue.Count > 0)
            {
                highestPriorityQueue.AddLast(item);
            }
            else if((int)item.ItemPriority >= (int)ItemPriority.High && highPriorityQueue.Count > 0)
            {
                highPriorityQueue.AddLast(item);
            }
            else
            {
                normalPriorityQueue.AddLast(item);
            }
        }

        public ItemData Dequeue()
        {
            lock(syncObj)
            {
                if(InternalTryDequeue(out var item))
                    return item;
                else
                    throw new InvalidOperationException("Queue is empty");
            }
        }

        public bool TryDequeue(out ItemData item)
        {
            lock(syncObj)
            {
                return InternalTryDequeue(out item);
            }
        }

        private bool InternalTryDequeue(out ItemData item)
        {
            item = null;
            if(highestPriorityQueue.First != null)
            {
                item = highestPriorityQueue.First.Value;
                highestPriorityQueue.RemoveFirst();
                return true;
            }
            if(highPriorityQueue.First != null)
            {
                item = highPriorityQueue.First.Value;
                highPriorityQueue.RemoveFirst();
                return true;
            }
            if(normalPriorityQueue.First != null)
            {
                item = normalPriorityQueue.First.Value;
                normalPriorityQueue.RemoveFirst();
                return true;
            }
            return false;
        }

        public IEnumerator<ItemData> GetEnumerator()
        {
            foreach(var item in highestPriorityQueue)
                yield return item;
            foreach(var item in highPriorityQueue)
                yield return item;
            foreach(var item in normalPriorityQueue)
                yield return item;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool TryRemove(Func<ItemData, bool> predicate, out ItemData item)
        {
            item = null;

            lock(syncObj)
            {
                if(FindNode(highestPriorityQueue, predicate, out item))
                    return true;
            
                if(FindNode(highPriorityQueue, predicate, out item))
                    return true;
                
                if(FindNode(normalPriorityQueue, predicate, out item))
                    return true;

                return false;
            }
        }

        private bool FindNode(LinkedList<ItemData> list, Func<ItemData, bool> predicate, out ItemData item)
        {
            item = null;
            foreach(var node in list)
            {
                if(predicate(node))
                {
                    item = node;
                    list.Remove(node);
                    return true;
                }
            }
            return false;
        }
    }
}
