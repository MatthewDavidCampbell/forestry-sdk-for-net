using System.Diagnostics.CodeAnalysis;

namespace Forestry.Deserialize
{
    /// <summary>
    /// Enumeration during deserialization returns a value with dimensions specific 
    /// to the media (e.g. XML, JSON, etc.)
    /// </summary>
    public class Value: IDisposable
    {
        private string? _name;

        private byte[] _rawValue = [];

        private Delegate? _valueGetter;

        private readonly Dictionary<IgnoreCaseName, object> _dimensions = [];

        /// <summary>
        /// Name
        /// </summary>
        /// <value></value>
        public virtual string? Name
        {
            get => _name;
            set
            {
                ArgumentException.ThrowIfNullOrEmpty(nameof(Name), value);
                _name = value;
            }
        }

        /// <summary>
        /// Raw value
        /// </summary>
        /// <value></value>
        public virtual ReadOnlySpan<byte> RawValue { get => _rawValue; }

        /// <summary>
        /// Set raw value
        /// </summary>
        /// <param name="value"></param>
        protected internal virtual void SetRawValue(byte[] value)
        {
            _rawValue = value;
        }

        /// <summary>
        /// Set raw value getter
        /// </summary>
        /// <param name="getter"></param>
        protected internal virtual void SetValueGetter(Delegate? getter)
        {
            _valueGetter = getter;
        }

        /// <summary>
        /// Dimensions
        /// </summary>
        /// <returns></returns>
        public virtual Dimensions Dimensions => new(this);

        /// <summary>
        /// Dispose raw value and dimensions
        /// </summary>
        public void Dispose()
        {
            // TODO: Dispose dimensions
            // TODO: Dispose raw value if later rented
        }

        /// <summary>
        /// Set dimension by name ignoring case
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        protected internal virtual void SetDimension(string name, string value)
        {
            _dimensions[new IgnoreCaseName(name)] = value;
        }

        /// <summary>
        /// Add dimension by name ignoring case
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        protected internal virtual void AddDimension(string name, string value)
        {
            if (_dimensions.TryGetValue(new IgnoreCaseName(name), out var other))
            {
                switch (other)
                {
                    case string stringValue:
                        _dimensions[new IgnoreCaseName(name)] = new List<string> { stringValue };
                        break;
                    case List<string> listValue:
                        listValue.Add(value);
                        break;
                }
            } else
            {
                _dimensions[new IgnoreCaseName(name)] = value;
            }
        }

        /// <summary>
        /// Try get dimension by name ignoring case
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected internal virtual bool TryGetDimension(string name, [NotNullWhen(true)] out string? value)
        {
            if (_dimensions.TryGetValue(new IgnoreCaseName(name), out var other))
            {
                value = GetDimensionAsString(name, other);
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Try get dimensions
        /// </summary>
        /// <param name="name"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        protected internal virtual bool TryGetDimensions(string name, [NotNullWhen(true)] out IEnumerable<string>? values)
        {
            if (_dimensions.TryGetValue(new IgnoreCaseName(name), out var value))
            {
                values = value switch
                {
                    string stringValue => new[] { stringValue },
                    List<string> listValue => listValue,
                    _ => throw new InvalidOperationException($"Unexpected value type [{value?.GetType()}] for dimension [{name}]")
                };
                return true;
            }

            values = default;
            return false;
        }

        /// <summary>
        /// Contains dimension
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected internal virtual bool ContainsDimension(string name) => _dimensions.ContainsKey(new IgnoreCaseName(name));

        /// <summary>
        /// Dimensions enumerable
        /// </summary>
        /// <returns></returns>
        protected internal virtual IEnumerable<Dimension> EnumerateDimensions()
        {
            foreach (var key in _dimensions.Keys)
            {
                var value = _dimensions[key];
                yield return new Dimension(key, GetDimensionAsString(key, value));
            }
        }

        /// <summary>
        /// Get dimension as string
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string GetDimensionAsString(string name, object value) => value switch
        {
            string stringValue => stringValue,
            List<string> listValue => string.Join(",", listValue),
            _ => throw new InvalidOperationException($"Unexpected value type [{value?.GetType()}] for dimension [{name}]")
        };

        /// <summary>
        /// Equal || not equal ignoring case
        /// </summary>
        /// <param name="value"></param>
        private readonly struct IgnoreCaseName(string value) : IEquatable<IgnoreCaseName>
        {
            private readonly string _value = value;

            public bool Equals(IgnoreCaseName other) => string.Equals(_value, other._value, StringComparison.OrdinalIgnoreCase);

            public override bool Equals(object? obj) => obj is IgnoreCaseName other && Equals(other);

            public static bool operator ==(IgnoreCaseName left, IgnoreCaseName right) => left.Equals(right);

            public static bool operator !=(IgnoreCaseName left, IgnoreCaseName right) => !left.Equals(right);

            public static implicit operator string(IgnoreCaseName other) => other._value;

            public override int GetHashCode() => _value.GetHashCode();
        }
    }
}