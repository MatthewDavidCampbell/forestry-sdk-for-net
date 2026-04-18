using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;

namespace Forestry.Deserialize
{
    /// <summary>
    /// <see cref="TypeDefinition"/> cache when options are readonly i.e. locked
    /// </summary>
    public abstract partial class DeserializeOptions
    {
        /// <summary>
        /// Get <see cref="TypeDefinition"/> keeping the latest around as LRU cache
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public TypeDefinition GetTypeDefinition(
            Type type
        ) {
            ArgumentNullException.ThrowIfNull(type);

            TypeDefinition? typeDefinition = lastTypeDefinition;

            if (typeDefinition?.Type != type)
            {
                lastTypeDefinition = typeDefinition = ThrowingGetTypeDefinition(type);
            }

            return typeDefinition;
        }

        /// <summary>
        /// Last deserializable type definition non-thread safe LRU 
        /// </summary>
        private volatile TypeDefinition? lastTypeDefinition;

        /// <summary>
        /// Throw when get type definition is null
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal TypeDefinition ThrowingGetTypeDefinition(
            Type type
        ) {
            TypeDefinition? typeDefinition = null;

            // TODO: Logic when not initialized (mutable) without caching

            if (IsReadOnly)
            {
                typeDefinition = Cache.GetOrAdd(type);
                typeDefinition?.SetConfiguration();
            }

            if (typeDefinition is null)
            {
                throw new InvalidOperationException();  // TODO:
            }

            return typeDefinition;
        }

        /// <summary>
        /// Try get type definition
        /// </summary>
        /// <param name="type"></param>
        /// <param name="typeDefinition"></param>
        /// <returns></returns>
        internal bool TryGetTypeDefinition(
            Type type,
            [NotNullWhen(true)] out TypeDefinition? typeDefinition
        ) {
            if (_cache is null)
            {
                typeDefinition = null;
                return false;
            }

            return _cache.TryGet(type, out typeDefinition);
        }

        /// <summary>
        /// Create <see cref="TypeDefinition"/>
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private TypeDefinition? CreateTypeDefinition(Type type)
        {
            TypeDefinition? typeDefinition = _typeDefinitionProvider.GetTypeDefinition(type, this);

            // TODO: Assert type + _options compatibility when exists else try create fallback object type definition

            return typeDefinition;
        }

        /// <summary>
        /// When <see cref="TypeDefinition"/> is any object bypassing the cache
        /// </summary>
        /// <value></value>
        internal TypeDefinition ObjectTypeDefintion
        {
            get
            {
                Debug.Assert(IsReadOnly);
                return _objectTypeDefintion ??= ThrowingGetTypeDefinition(Deserialization._objectType); 
            }
        }

        private TypeDefinition? _objectTypeDefintion;

        /// <summary>
        /// Equivalent <see cref="DeserializeOptions"/> share a <see cref="TypeDefinition"/> cache
        /// </summary>
        internal TypeDefinitionCache Cache { 
            get {
                Debug.Assert(IsReadOnly);
                return _cache ?? GetOrCreate();

                TypeDefinitionCache GetOrCreate()
                {
                    TypeDefinitionCache cache = TypeDefinitionCacheFactory.GetOrCreate(this);
                    return Interlocked.CompareExchange(ref _cache, cache, null) ?? cache;
                }
            } 
        }

        private TypeDefinitionCache? _cache;

        /// <summary>
        /// <see cref="TypeDefinition"/> cache initialized from a particular <see cref="DeserializeOptions"/> reference
        /// </summary>
        internal sealed class TypeDefinitionCache
        {
            private readonly ConcurrentDictionary<Type, NullableValue> _cache = new();

            private readonly Func<Type, NullableValue> _nullableValueFactory;

            /// <summary>
            /// Nullable <see cref="TypeDefinition"/> value
            /// </summary>
            private sealed class NullableValue
            {
                public NullableValue(TypeDefinition? typeDefinition)
                {
                    TypeDefinition = typeDefinition;
                    HasValue = typeDefinition is not null;
                }

                public NullableValue(ExceptionDispatchInfo exceptionDispatchInfo)
                {
                    ExceptionDispatchInfo = exceptionDispatchInfo;
                    HasValue = true;
                }

                /// <summary>
                /// <see cref="TypeDefinition"/>
                /// </summary>
                public readonly TypeDefinition? TypeDefinition;

                /// <summary>
                /// Assert when not nullable
                /// </summary>
                public readonly bool HasValue;

                /// <summary>
                /// Exception with state when captured
                /// </summary>
                public readonly ExceptionDispatchInfo? ExceptionDispatchInfo;

                /// <summary>
                /// Throw when has captured exception otherwise return <see cref="TypeDefinition"/>
                /// </summary>
                /// <returns></returns>
                public TypeDefinition? ThrowingGet()
                {
                    ExceptionDispatchInfo?.Throw();
                    return TypeDefinition;
                }
            }

            /// <summary>
            /// Cached value from <see cref="TypeDefinition"/> initialization
            /// </summary>
            /// <param name="type"></param>
            /// <param name="cache"></param>
            /// <returns></returns>
            private static NullableValue NullableValueFactory(Type type, TypeDefinitionCache cache)
            {
                try
                {
                    TypeDefinition? typeDefinition = cache.Options.CreateTypeDefinition(type);
                    return new NullableValue(typeDefinition);
                } catch (Exception e) {
                    ExceptionDispatchInfo captured = ExceptionDispatchInfo.Capture(e);
                    return new NullableValue(captured);
                }
            }
            
            public TypeDefinitionCache(DeserializeOptions options, int id)
            {
                Options = options;
                Id = id;

                _nullableValueFactory = type => NullableValueFactory(type, this);
            }

            /// <summary>
            /// Options
            /// </summary>
            public DeserializeOptions Options { get; }

            /// <summary>
            /// Hash code from <see cref="EqualityComparer{T}"/> where T == <see cref="DeserializeOptions"/>
            /// </summary>
            public int Id { get; }

            /// <summary>
            /// Get from || Add to cache
            /// </summary>
            /// <param name="type"></param>
            /// <returns></returns>
            public TypeDefinition? GetOrAdd(Type type)
            {
                // TODO: Type bases

                NullableValue value = _cache.GetOrAdd(type, _nullableValueFactory);
                return value.ThrowingGet();
            }

            /// <summary>
            /// Try get from cache
            /// </summary>
            /// <param name="type"></param>
            /// <param name="typeDefinition"></param>
            /// <returns></returns>
            public bool TryGet(Type type, [NotNullWhen(true)] out TypeDefinition? typeDefinition)
            {
                _cache.TryGetValue(type, out var value);
                typeDefinition = value?.TypeDefinition;

                return typeDefinition is not null;
            }

            /// <summary>
            /// Clear cache
            /// </summary>
            public void Clear()
            {
                _cache.Clear();
            }
        }

        /// <summary>
        /// <see cref="TypeDefinitionCache"/> factory creating caches when <see cref="DeserializeOptions"/> differ
        /// </summary>
        internal static class TypeDefinitionCacheFactory
        {
            private const int MaxTypeDefinitionCaches = 64;  // i.e. 64 difference _options instances (an unlikely high number)

            private static readonly WeakReference<TypeDefinitionCache>?[] typeDefinitionCaches = new WeakReference<TypeDefinitionCache>[MaxTypeDefinitionCaches]; // Weak References do not stop GC

            private static readonly DeserializedOptionsEqualityComparer optionsComparer = new();

            /// <summary>
            /// Get || create <see cref="TypeDefinitionCache"/> from <see cref="DeserializeOptions"/>
            /// </summary>
            /// <param name="options"></param>
            /// <returns></returns>
            public static TypeDefinitionCache GetOrCreate(DeserializeOptions options)
            {
                int id = optionsComparer.GetHashCode(options);

                // When getting (equality || maxed cache)
                if (TryGet(options, id, out int next, out TypeDefinitionCache? cache))
                {
                    return cache;
                } else if (next < 0)  // Negative == maxed cache
                {
                    return new TypeDefinitionCache(options, id); 
                }

                // When creating (not maxed cache i.e. next not negative)
                lock (typeDefinitionCaches)
                {
                    // Assert after locking
                    if (TryGet(options, id, out next, out cache))
                    {
                        return cache;
                    }

                    TypeDefinitionCache other = new TypeDefinitionCache(options, id);

                    // Assert has free space
                    if (next >= 0)
                    {
                        ref WeakReference<TypeDefinitionCache>? weakReference = ref typeDefinitionCaches[next];

                        if (weakReference is null)
                        {
                            weakReference = new(other);
                        } else
                        {
                            Debug.Assert(weakReference.TryGetTarget(out _) is false);
                            weakReference.SetTarget(other);
                        }
                    }

                    return other;
                }
            }

            /// <summary>
            /// Try get <see cref="TypeDefinitionCache"/> when _options equality otherwise set next 
            /// free index if the cache has space (next == -1 == maxed cached)
            /// </summary>
            /// <param name="options"></param>
            /// <param name="id"></param>
            /// <param name="next"></param>
            /// <param name="cache"></param>
            /// <returns></returns>
            private static bool TryGet(
                DeserializeOptions options,
                int id,
                out int next,
                [NotNullWhen(true)] out TypeDefinitionCache? cache
            ) {
                WeakReference<TypeDefinitionCache>?[] caches = typeDefinitionCaches;

                next = -1;
                for (int index = 0; index < caches.Length; index++)
                {
                    WeakReference<TypeDefinitionCache>? weakReference = caches[index];

                    // When next is not set and an index is free
                    if (weakReference is null || !weakReference.TryGetTarget(out TypeDefinitionCache? other))
                    {
                        if (next < 0) { 
                            next = index; 
                        }  
                    } 
                    // When return other if _options match
                    else if (id == other.Id && optionsComparer.Equals(options, other.Options))
                    {
                        cache = other;
                        return true;
                    }
                }

                cache = null;
                return false;
            }
        }
        
        /// <summary>
        /// Reference equality based on members
        /// </summary>
        private sealed class DeserializedOptionsEqualityComparer : IEqualityComparer<DeserializeOptions>
        {
            public bool Equals(DeserializeOptions? left, DeserializeOptions? right)
            {
                Debug.Assert(left != null && right != null);

                return true;
            }

            public int GetHashCode(DeserializeOptions options) {
                HashCode hc = default;

                return hc.ToHashCode();
            }
        }
    }
}