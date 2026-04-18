namespace Forestry.Deserialize
{
    /// <summary>
    /// Reader constraints
    /// </summary>
    public enum DeserializerKind: byte
    {
        None = 0x0,

        Object = 0x1,

        Value = 0x2,

        Enumerable = 0x8,

        Dictionary = 0x10
    }
}