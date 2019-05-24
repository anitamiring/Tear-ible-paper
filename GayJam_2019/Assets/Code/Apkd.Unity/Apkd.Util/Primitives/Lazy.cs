using System;
using System.Threading;

namespace Apkd
{
    /// <summary>
    /// <see cref="System.Lazy{T}"> with implicit conversion to wrapped type and thread safety disabled by default.
    /// </summary>
	public sealed class Lazy<T> : System.Lazy<T>
	{
		public Lazy(bool isThreadSafe) : base(isThreadSafe) { }
		public Lazy(LazyThreadSafetyMode mode) : base(mode) { }
		public Lazy(Func<T> valueFactory) : base(valueFactory, isThreadSafe: false) { }
		public Lazy(Func<T> valueFactory, LazyThreadSafetyMode mode) : base(valueFactory, mode) { }
		public Lazy(Func<T> valueFactory, bool isThreadSafe) : base(valueFactory, isThreadSafe) { }

		public static implicit operator T(Lazy<T> lazy)
            => lazy.Value;
	}

    /// <summary>
    /// Utility methods for <see cref="Lazy{T}">.
    /// </summary>
    public static class Lazy
    {
        public static Lazy<T> From<T>(Func<T> valueFactory)
            => new Lazy<T>(valueFactory, isThreadSafe: false);
    }
}