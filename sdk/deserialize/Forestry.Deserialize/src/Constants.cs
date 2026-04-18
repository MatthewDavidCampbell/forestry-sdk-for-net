using System.Collections;

namespace Forestry.Deserialize
{
    public static class Constants
    {
        /// <summary>
        /// Empty async <see cref="Value"/> enumerator
        /// </summary>
        /// <typeparam name="Value"></typeparam>
        /// <returns></returns>
        public static IAsyncEnumerator<Value> EmptyAsync<Value>() => EmptyAsyncEnumerator<Value>.Instance;

        /// <summary>
        /// Empty <see cref="Value"/> enumerator
        /// </summary>
        /// <typeparam name="Value"></typeparam>
        /// <returns></returns>
        public static IEnumerator<Value> Empty<Value>() => EmptyEnumerator<Value>.Instance;

        /// <summary>
        /// Empty async enumerator
        /// </summary>
        private sealed class EmptyAsyncEnumerator<T>: IAsyncEnumerator<T>
        {
            public static readonly EmptyAsyncEnumerator<T> Instance = new();

            private EmptyAsyncEnumerator() { }

            public T Current => default!;

            public ValueTask<bool> MoveNextAsync() => new(false);

            public ValueTask DisposeAsync() => default;
        }

        /// <summary>
        /// Empty enumerator
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private sealed class EmptyEnumerator<T> : IEnumerator<T>
        {
            public static readonly EmptyEnumerator<T> Instance = new();

            private EmptyEnumerator() {}

            public T Current => default!;

            object IEnumerator.Current => Current!;

            public void Dispose() {}

            public bool MoveNext() => false;

            public void Reset() {}
        }
    }
}