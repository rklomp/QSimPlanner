using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace QSP.LibraryExtension
{
    /// <summary>
    /// A hash map allowing duplicate keys.
    /// </summary>
    public class MultiMap<TKey, TValue> : IEnumerable
    {
        private struct Entry
        {
            // Lower 31 bits of hash code, -1 if unused
            public int hashCode;
            // Index of next entry, -1 if last
            public int nextIndex;
            // Key of entry
            public TKey key;
            // Value of entry
            public TValue value;
        }

        private int[] buckets;
        private Entry[] entries;
        private int elemCount;
        private int freeList;
        private int freeCount;

        private IEqualityComparer<TKey> keyComparer;
        private IEqualityComparer<TValue> valueComparer;

        public MultiMap() : this(0, null)
        { }

        public MultiMap(int capacity) : this(capacity, null)
        { }

        public MultiMap(IEqualityComparer<TKey> comparer) : this(0, comparer)
        { }

        public MultiMap(
            IEqualityComparer<TKey> keyComparer,
            IEqualityComparer<TValue> valueComparer)
            : this(0, keyComparer, valueComparer)
        { }

        public MultiMap(int capacity, IEqualityComparer<TKey> comparer)
            : this(capacity, comparer, null)
        { }

        public MultiMap(
            int capacity,
            IEqualityComparer<TKey> keyComparer,
            IEqualityComparer<TValue> valueComparer)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(
                    "Capacity cannot be smaller than 0.");
            }

            if (capacity > 0)
            {
                Initialize(capacity);
            }

            this.keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;

            this.valueComparer =
                valueComparer ?? EqualityComparer<TValue>.Default;
        }

        public int Count
        {
            get { return elemCount - freeCount; }
        }

        /// <summary>
        /// Find any entry with the given key.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public TValue FindAny(TKey key)
        {
            int i = FindEntry(key);

            if (i >= 0)
            {
                return entries[i].value;
            }

            throw new ArgumentException("Key not found.");
        }

        /// <summary>
        /// Add the key and value.
        /// </summary>
        /// <exception cref="ArgumentException">key is null</exception>
        public void Add(TKey key, TValue value)
        {
            Insert(key, value);
        }

        public void Clear()
        {
            if (elemCount > 0)
            {
                for (int i = 0; i < buckets.Length; i++)
                {
                    buckets[i] = -1;
                }
                Array.Clear(entries, 0, elemCount);
                freeList = -1;
                elemCount = 0;
                freeCount = 0;
            }
        }

        public bool ContainsKey(TKey key)
        {
            return FindEntry(key) >= 0;
        }

        public bool ContainsValue(TValue value)
        {
            if (value == null)
            {
                for (int i = 0; i < elemCount; i++)
                {
                    if (entries[i].hashCode >= 0 && entries[i].value == null)
                    {
                        return true;
                    }
                }
            }
            else
            {
                var c = EqualityComparer<TValue>.Default;

                for (int i = 0; i < elemCount; i++)
                {
                    if (entries[i].hashCode >= 0 &&
                        c.Equals(entries[i].value, value))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Finds all entries with the given key.
        /// Returns an empty List if none is found.
        /// </summary>
        public List<TValue> FindAll(TKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException();
            }

            var result = new List<TValue>();

            if (buckets != null)
            {
                int hashCode = keyComparer.GetHashCode(key) & 0x7fffffff;
                int i = buckets[hashCode % buckets.Length];

                while (i >= 0)
                {
                    if (entries[i].hashCode == hashCode &&
                        keyComparer.Equals(entries[i].key, key))
                    {
                        result.Add(entries[i].value);
                    }

                    i = entries[i].nextIndex;
                }
            }

            return result;
        }

        private int FindEntry(TKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException();
            }

            if (buckets != null)
            {
                int hashCode = keyComparer.GetHashCode(key) & 0x7fffffff;
                int i = buckets[hashCode % buckets.Length];

                while (i >= 0)
                {
                    if (entries[i].hashCode == hashCode &&
                        keyComparer.Equals(entries[i].key, key))
                    {
                        return i;
                    }

                    i = entries[i].nextIndex;
                }
            }
            return -1;
        }

        /// <summary>
        /// Finds the entry with the given key and value.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public TValue Find(TKey key, TValue value)
        {
            if (key == null)
            {
                throw new ArgumentNullException();
            }

            if (buckets != null)
            {
                int hashCode = keyComparer.GetHashCode(key) & 0x7fffffff;
                int i = buckets[hashCode % buckets.Length];

                while (i >= 0)
                {
                    if (entries[i].hashCode == hashCode &&
                        keyComparer.Equals(entries[i].key, key) &&
                        valueComparer.Equals(entries[i].value, value))
                    {
                        return entries[i].value;
                    }

                    i = entries[i].nextIndex;
                }
            }

            throw new ArgumentException("Not found.");
        }

        private void Initialize(int capacity)
        {
            int size = HashHelpers.GetPrime(capacity);
            buckets = new int[size];
            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i] = -1;
            }
            entries = new Entry[size];
            freeList = -1;
        }

        private void Insert(TKey key, TValue value)
        {
            if (key == null)
            {
                throw new ArgumentNullException();
            }

            if (buckets == null)
            {
                Initialize(0);
            }

            int hashCode = keyComparer.GetHashCode(key) & 0x7fffffff;
            int targetBucket = hashCode % buckets.Length;

            int i = buckets[targetBucket];

            int index = 0;
            if (freeCount > 0)
            {
                index = freeList;
                freeList = entries[index].nextIndex;
                freeCount--;
            }
            else
            {
                if (elemCount == entries.Length)
                {
                    Resize();
                    targetBucket = hashCode % buckets.Length;
                }
                index = elemCount;
                elemCount++;
            }

            entries[index].hashCode = hashCode;
            entries[index].nextIndex = buckets[targetBucket];
            entries[index].key = key;
            entries[index].value = value;
            buckets[targetBucket] = index;
        }

        private void Resize()
        {
            Resize(HashHelpers.ExpandPrime(elemCount), false);
        }

        private void Resize(int newSize, bool forceNewHashCodes)
        {
            Contract.Assert(newSize >= entries.Length);
            int[] newBuckets = new int[newSize];

            for (int i = 0; i < newBuckets.Length; i++)
            {
                newBuckets[i] = -1;
            }

            Entry[] newEntries = new Entry[newSize];
            Array.Copy(entries, 0, newEntries, 0, elemCount);

            if (forceNewHashCodes)
            {
                for (int i = 0; i < elemCount; i++)
                {
                    if (newEntries[i].hashCode != -1)
                    {
                        newEntries[i].hashCode =
                            keyComparer.GetHashCode(newEntries[i].key) &
                            0x7fffffff;
                    }
                }
            }
            for (int i = 0; i < elemCount; i++)
            {
                if (newEntries[i].hashCode >= 0)
                {
                    int bucket = newEntries[i].hashCode % newSize;
                    newEntries[i].nextIndex = newBuckets[bucket];
                    newBuckets[bucket] = i;
                }
            }
            buckets = newBuckets;
            entries = newEntries;
        }

        private enum RemoveParameter
        {
            RemoveAllMatchingKey = -2,     // Only compare key.
            RemoveFirstMatchingKey = -1,
            RemoveFirst = 1,
            RemoveAll = 2
        }

        public enum RemoveOption
        {
            RemoveFirst = 1,
            RemoveAll = 2
        }

        /// <summary>
        /// Remove the item(s) with the given key.
        /// </summary>
        public bool Remove(TKey key, RemoveOption para)
        {
            return Remove(key, default(TValue), (RemoveParameter)(-((int)para)));
        }

        /// <summary>
        /// Remove the item(s) with the given key and value.
        /// </summary>
        public bool Remove(TKey key, TValue value, RemoveOption para)
        {
            return Remove(key, value, (RemoveParameter)para);
        }

        private bool Remove(TKey key, TValue value, RemoveParameter para)
        {
            if (key == null)
            {
                throw new ArgumentNullException();
            }

            bool removed = false;

            if (buckets != null)
            {
                int hashCode = keyComparer.GetHashCode(key) & 0x7fffffff;
                int bucket = hashCode % buckets.Length;
                int last = -1;
                int i = buckets[bucket];

                while (i >= 0)
                {
                    if (entries[i].hashCode == hashCode &&
                        keyComparer.Equals(entries[i].key, key) &&
                        para < 0 ? true : valueComparer.Equals(entries[i].value, value))
                    {
                        if (last < 0)
                        {
                            buckets[bucket] = entries[i].nextIndex;
                        }
                        else
                        {
                            entries[last].nextIndex = entries[i].nextIndex;
                        }

                        int j = entries[i].nextIndex;

                        entries[i].hashCode = -1;
                        entries[i].nextIndex = freeList;
                        entries[i].key = default(TKey);
                        entries[i].value = default(TValue);
                        freeList = i;
                        freeCount++;

                        last = i;
                        i = j;

                        if (para == RemoveParameter.RemoveFirst ||
                            para == RemoveParameter.RemoveFirstMatchingKey)
                        {
                            return true;
                        }

                        removed = true;
                    }
                    else
                    {
                        last = i;
                        i = entries[i].nextIndex;
                    }
                }
            }

            return removed;
        }

        internal TValue GetValueOrDefault(TKey key)
        {
            int i = FindEntry(key);

            if (i >= 0)
            {
                return entries[i].value;
            }

            return default(TValue);
        }

        public IEnumerator GetEnumerator()
        {
            return new Enumerator(this, 2);
        }

        [Serializable()]
        public struct Enumerator : IEnumerator
        {
            [NonSerialized()]
            private MultiMap<TKey, TValue> dictionary;
            private int index;
            private KeyValuePair<TKey, TValue> m_current;
            private int getEnumeratorRetType;
            // What should Enumerator.Current return?
            internal const int DictEntry = 1;
            internal const int KeyValuePair = 2;

            internal Enumerator(MultiMap<TKey, TValue> dictionary, int getEnumeratorRetType)
            {
                this.dictionary = dictionary;
                index = 0;
                this.getEnumeratorRetType = getEnumeratorRetType;
                m_current = new KeyValuePair<TKey, TValue>();
            }

            public bool MoveNext()
            {
                // Use unsigned comparison since we set index to dictionary.count+1 when the enumeration ends.
                // dictionary.count+1 could be negative if dictionary.count is Int32.MaxValue
                while (Convert.ToUInt32(index) < Convert.ToUInt32(dictionary.elemCount))
                {
                    if (dictionary.entries[index].hashCode >= 0)
                    {
                        m_current = new KeyValuePair<TKey, TValue>(
                            dictionary.entries[index].key, dictionary.entries[index].value);
                        index++;
                        return true;
                    }
                    index++;
                }

                index = dictionary.elemCount + 1;
                m_current = new KeyValuePair<TKey, TValue>();
                return false;
            }

            public KeyValuePair<TKey, TValue> Current
            {
                get { return m_current; }
            }

            public void Dispose()
            {
            }

            private object IEnumerator_Current
            {
                get
                {
                    if (index == 0 || (index == dictionary.elemCount + 1))
                    {
                        throw new InvalidOperationException();
                    }

                    if (getEnumeratorRetType == DictEntry)
                    {
                        return new DictionaryEntry(m_current.Key, m_current.Value);
                    }
                    else
                    {
                        return new KeyValuePair<TKey, TValue>(m_current.Key, m_current.Value);
                    }
                }
            }
            object IEnumerator.Current
            {
                get { return IEnumerator_Current; }
            }

            private void IEnumerator_Reset()
            {
                index = 0;
                m_current = new KeyValuePair<TKey, TValue>();
            }
            void IEnumerator.Reset()
            {
                IEnumerator_Reset();
            }
        }

        internal sealed class HashHelpers
        {
            private HashHelpers()
            {
            }

            // Table of prime numbers to use as hash table sizes. 
            // A typical resize algorithm would pick the smallest prime number in this array
            // that is larger than twice the previous capacity. 
            // Suppose our Hashtable currently has capacity x and enough elements are added 
            // such that a resize needs to occur. Resizing first computes 2x then finds the 
            // first prime in the table greater than 2x, i.e. if primes are ordered 
            // p_1, p_2, ..., p_i, ..., it finds p_n such that p_n-1 < 2x < p_n. 
            // Doubling is important for preserving the asymptotic complexity of the 
            // hashtable operations such as add.  Having a prime guarantees that double 
            // hashing does not lead to infinite loops.  IE, your hash function will be 
            // h1(key) + i*h2(key), 0 <= i < size.  h2 and the size must be relatively prime.


            const int HashPrime = 101;
            public static readonly int[] primes =
                {3,7,11,17,23,29,37,47,59,71,89,107,131,163,197,239,293,353,431,521,631,761,919,1103,
                 1327,1597,1931,2333,2801,3371,4049,4861,5839,7013,8419,10103,12143,14591,
                17519,21023,25229,30293,36353,43627,52361,62851,75431,90523,108631,130363,
                156437,187751,225307,270371,324449,389357,467237,560689,672827,807403,
                968897,1162687,1395263,1674319,2009191,2411033,2893249,3471899,4166287,4999559,5999471,7199369};

            public static bool IsPrime(int candidate)
            {
                if ((candidate & 1) != 0)
                {
                    int limit = Convert.ToInt32(Math.Sqrt(candidate));
                    for (int divisor = 3; divisor <= limit; divisor += 2)
                    {
                        if ((candidate % divisor) == 0)
                        {
                            return false;
                        }
                    }
                    return true;
                }
                return (candidate == 2);
            }

            public static int GetPrime(int min)
            {
                if (min < 0)
                {
                    throw new ArgumentException("Input cannot be negative.");
                }
                Contract.EndContractBlock();

                for (int i = 0; i < primes.Length; i++)
                {
                    int prime = primes[i];
                    if (prime >= min)
                    {
                        return prime;
                    }
                }

                //outside of our predefined table. 
                //compute the hard way. 
                for (int i = (min | 1); i < int.MaxValue; i += 2)
                {
                    if (IsPrime(i) && ((i - 1) % HashPrime != 0))
                    {
                        return i;
                    }
                }
                return min;
            }

            public static int GetMinPrime()
            {
                return primes[0];
            }

            // Returns size of hashtable to grow to.
            public static int ExpandPrime(int oldSize)
            {
                int newSize = 2 * oldSize;

                // Allow the hashtables to grow to maximum possible size (~2G elements) before encoutering capacity overflow.
                // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
                if (Convert.ToUInt32(newSize) > MaxPrimeArrayLength && MaxPrimeArrayLength > oldSize)
                {
                    Contract.Assert(MaxPrimeArrayLength == GetPrime(MaxPrimeArrayLength), "Invalid MaxPrimeArrayLength");
                    return MaxPrimeArrayLength;
                }

                return GetPrime(newSize);
            }

            // This is the maximum prime smaller than Array.MaxArrayLength

            public const int MaxPrimeArrayLength = 0x7feffffd;
        }
    }
}