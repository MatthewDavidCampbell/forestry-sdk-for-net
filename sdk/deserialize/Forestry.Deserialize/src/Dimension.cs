namespace Forestry.Deserialize
{
    /// <summary>
    /// Dimensions are name-value pairs to enrich <see cref="Value"/>
    /// </summary>
    public readonly partial struct Dimension : IEquatable<Dimension>
    {
        public Dimension(string name, string value)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name)); // TODO:
            }

            Name = name;
            Value = value;
        }

        public string Name { get; }

        public string Value { get; }

        public bool Equals(Dimension? other)
        {
            if (other is Dimension dimension)
            {
                return Equals(dimension);
            }

            return false;
        }

        public bool Equals(Dimension other)
        {
            return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase) && Value.Equals(other.Value, StringComparison.Ordinal);
        }

        public override string ToString() => $"{Name}:{Value}";

        public override int GetHashCode()
        {
            return default(int);  // TODO:
        }
    }
}