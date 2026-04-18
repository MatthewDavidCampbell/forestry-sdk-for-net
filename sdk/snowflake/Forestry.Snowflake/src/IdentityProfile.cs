namespace Forestry.Snowflake {
    /// <summary>
    /// An identity profile creates restrictions on lifetime,
    /// creation rate, and nodes based on the allowed suffix length.
    /// 
    /// Type decisions:
    ///   - masks are unsigned integers since they are patterns used for overflow
    ///   - bits are integers since they are counts
    /// </summary>
    /// <param name="Prefix"></param>
    /// <param name="SuffixLength"></param>
    /// <param name="TotalBits"></param>
    /// <param name="TimestampBits"></param>
    /// <param name="CreationRateBits"></param>
    /// <param name="NodesBits"></param>
    /// <param name="TimestampMask"></param>
    /// <param name="CreationRateMask"></param>
    /// <param name="NodesMask"></param>
    /// <param name="UseTimestampMilliseconds"></param>
    public readonly record struct IdentityProfile(
        string Prefix,
        byte SuffixLength,
        int TotalBits,
        int TimestampBits,
        int CreationRateBits,
        int NodesBits,
        int RemainingBits,
        ulong TimestampMask,
        ulong CreationRateMask,
        ulong NodesMask,
        bool UseTimestampMilliseconds
    );
}