using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Forestry.Deserialize
{
    /// <summary>
    /// Dimensions associated with a <see cref="Value"/>
    /// </summary>
    public readonly struct Dimensions : IEnumerable<Dimension>
    {
        private readonly Value _value;

        internal Dimensions(Value value)
        {
            _value = value;
        }

        public DateTimeOffset? Date => TryGetValue(Dimension.Names.Date, out var value) ?
            DateTimeOffset.Parse(value, CultureInfo.InvariantCulture) :
            null;

        public string? RawValueType => TryGetValue(Dimension.Names.RawValueType, out string? value) ?
            value : 
            null;

        public int? RawValueLength
        {
            get
            {
                if (!TryGetValue(Dimension.Names.RawValueLength, out string? value))
                {
                    return null;
                }

                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number))
                {
                    throw new OverflowException($"'{Dimension.Names.RawValueLength}' header: '{value}' exceeds {int.MaxValue}");
                }

                return number; 
            }
        }  


        public bool TryGetValue(string name, [NotNullWhen(true)] out string? value)
        {
            return _value.TryGetDimension(name, out value);
        }

        public bool TryGetValues(string name, [NotNullWhen(true)] out IEnumerable<string>? values)
        {
            return _value.TryGetDimensions(name, out values); 
        }        

        public bool Contains(string name)
        {
            return _value.ContainsDimension(name);
        }


        public IEnumerator<Dimension> GetEnumerator()
        {
            return _value.EnumerateDimensions().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }              
    }
}