namespace Forestry.Turn
{
    /// <summary>
    /// Dimensions are useful as collective name-value pairs 
    /// </summary>
    public readonly struct Dimension : IEquatable<Dimension>
    {
        public Dimension(
            string name,
            string value
        )
        {
            ArgumentNullException.ThrowIfNullOrEmpty(name, nameof(name));

            Name = name;
            Value = value;
        }

        /// <summary>
        /// Dimension name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Dimension value
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Default value as collection delimeter
        /// </summary>
        public const string DefaultDelimeter = ",";

        /// <summary>
        /// Equivalent <see cref="Dimension.Name"/> string as bytes to another
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Dimension other)
        {
            return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Object is an equivalent <see cref="Dimension"/>
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public override bool Equals(object? other)
        {
            return other is Dimension dimension && Equals(dimension);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Value);  // TODO: is ordinal equivalent
        }

        public override string ToString()
        {
            return $"{Name}:{Value}";
        }

        /// <summary>
        /// Generic dimension names
        /// </summary>
        public static class Names
        {
            public static string Date => "Date";

            public static string ContentType => "Content-Type";

            public static string ContentLength => "Content-Length";

            public static string ETag => "ETag";

            public static string Authorization => "Authorization";
        }
    }
}
