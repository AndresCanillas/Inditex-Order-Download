using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts
{
    public class ConcurrentQueue2<T>: IEnumerable<T>
    {
        private object syncObj = new object();
        private LinkedList<T> list = new LinkedList<T>();

        public ConcurrentQueue2()
        {

        }

        public int Count
        {
            get
            {
                lock (syncObj) return list.Count;
            }
        }

        public void Enqueue(T item)
        {
            lock (syncObj)
            {
                list.AddLast(item);
            }
        }

        public bool TryDequeue(out T item)
        {
            item = default(T);
            lock(syncObj)
            {
                if (list.Count > 0)
                {
                    item = list.First.Value;
                    list.RemoveFirst();
                    return true;
                }
            }
            return false;
        }

        public bool FindAndDequeueItem(Predicate<T> p, out T item)
        {
            lock (syncObj)
            {
                if (list.Count > 0)
                {
                    var node = list.First;
                    do
                    {
                        if (p(node.Value))
                        {
                            item = node.Value;
                            list.Remove(node);
                            return true;
                        }
                        node = node.Next;
                    } while (node != null);
                }
            }
            item = default(T);
            return false;
        }

        public bool FindItem(Predicate<T> p, out T item)
        {
            lock (syncObj)
            {
                if(list.Count > 0)
                {
                    var node = list.First;
                    do
                    {
                        if (p(node.Value))
                        {
                            item = node.Value;
                            return true;
                        }
                        node = node.Next;
                    } while (node != null);
                }
            }
            item = default(T);
            return false;
        }

        public IEnumerator<T> GetEnumerator()
        {
            List<T> snapshot;
            lock(syncObj)
                snapshot = new List<T>(list);
            return snapshot.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Remove(T item)
        {
			lock (syncObj)
			{
				var node = list.Find(item);
				list.Remove(node);
			}
        }
    }
}
