namespace Forestry.Deserialize
{
    /// <summary>
    /// Dimension defaults generic to the media being deserialized
    /// </summary>
    public readonly partial struct Dimension : IEquatable<Dimension>
    {
        public static class Names
        {
            public static string Date => "Date";

            public static string RawValueType => "Raw-Value-Type";

            public static string RawValueLength => "Raw-Value-Length";
        }
    }
}