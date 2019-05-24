using System.Collections;
using System.Collections.Generic;

namespace Apkd
{
    public struct ReadOnlySet<T> : IReadOnlyCollection<T>
    {
        readonly HashSet<T> set;

        public ReadOnlySet(HashSet<T> set)
            => this.set = set;

        public ReadOnlySet(IEnumerable<T> collection)
            => this.set = new HashSet<T>(collection);

        public int Count
            => set.Count;

        public IEqualityComparer<T> Comparer
            => set.Comparer;

        public bool Contains(T item)
            => set.Contains(item);

        public void CopyTo(T[] array, int arrayIndex)
            => set.CopyTo(array, arrayIndex);

        public void CopyTo(T[] array, int arrayIndex, int count)
            => set.CopyTo(array, arrayIndex, count);

        public void CopyTo(T[] array)
            => set.CopyTo(array);

        public bool IsProperSubsetOf(IEnumerable<T> other)
            => set.IsProperSubsetOf(other);

        public bool IsProperSupersetOf(IEnumerable<T> other)
            => set.IsProperSupersetOf(other);

        public bool IsSubsetOf(IEnumerable<T> other)
            => set.IsSubsetOf(other);

        public bool IsSupersetOf(IEnumerable<T> other)
            => set.IsSupersetOf(other);

        public bool Overlaps(IEnumerable<T> other)
            => set.Overlaps(other);

        public bool SetEquals(IEnumerable<T> other)
            => set.SetEquals(other);

        public HashSet<T>.Enumerator GetEnumerator()
            => set.GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public IReadOnlyCollection<T> Collection => set;

        public static implicit operator ReadOnlySet<T>(HashSet<T> set)
            => new ReadOnlySet<T>(set);
    }
}