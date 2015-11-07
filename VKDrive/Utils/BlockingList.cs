using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VKDrive.Utils
{
    public class BlockingList<T> : BlockingCollection<T>, IList<T>
    {
        public T this[int index]
        {
            get
            {
                if(index < 0 || index >= this.Count() )
                {
                    throw new ArgumentOutOfRangeException();
                }
                int i = 0;
                foreach (T el in this)
                {
                    if (i == index)
                        return el;
                    i++;
                }
                throw new ArgumentOutOfRangeException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public void Clear()
        {
            while(this.Count > 0)
            {
                this.Take();
            }
        }

        public bool Contains(T item)
        {
            return IndexOf(item) != -1;
        }

        public int IndexOf(T item)
        {
            int i = 0;
            foreach(T el in this)
            {
                if (el.Equals(item))
                {
                    return i;
                }
                i++;
            }
            return -1;
        }

        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            return TryTake(out item);
        }

        public void RemoveAt(int index)
        {
            Remove(this[index]);
        }
    }
}
