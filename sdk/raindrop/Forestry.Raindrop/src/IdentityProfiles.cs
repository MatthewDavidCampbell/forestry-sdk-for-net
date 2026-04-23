
namespace Forestry.Raindrop
{
    /// <summary>
    /// Default profiles for "result", "delivery" and "delivery time"
    /// </summary>
    public static class IdentityProfiles
    {
        /// <summary>
        /// Production (PF) has P as a project identifier for results in the Forestry BIO prefix
        /// </summary>
        public const string ResultPrefix = "BIOP";

        /// <summary>
        /// Production (PF) has S as a project identifier for delivery + delivery time in the Forestry BIO prefix
        /// </summary>
        public const string DeliveryPrefix = "BIOS";

        /// <summary>
        /// Result suffix length
        /// </summary>
        public const byte ResultSuffixLength = 8;

        /// <summary>
        /// Delivery suffix length
        /// </summary>
        public const byte DeliverySuffixLength = 8;

        /// <summary>
        /// Delivery time suffix length
        /// </summary>
        public const byte DeliveryTimeSuffixLength = 16;

        /// <summary>
        /// 8.5 years in seconds (8.5 * 365 * 24 * 60 * 60)
        /// </summary>
        public const int Lifetime_8_Years = 268056000;

        /// <summary>
        /// 17 years in seconds (17 * 365 * 24 * 60 * 60)
        /// </summary>
        public const int Lifetime_17_Years = 536112466;

        /// <summary>
        /// 17 years in seconds (34 * 365 * 24 * 60 * 60)
        /// </summary>
        public const int Lifetime_34_Years = 1072224000;

        /// <summary>
        /// Creation rate before delaying to next second when 8 year lifetime
        /// </summary>
        public const int CreationRate_8_Years = 64;

        /// <summary>
        /// Creation rate before delaying to next second when 17 year lifetime
        /// </summary>
        public const int CreationRate_17_Years = 32;

        /// <summary>
        /// Creation rate before delaying to next second when 34 year lifetime
        /// </summary>
        public const int CreationRate_34_Years = 16;

        /// <summary>
        /// Nodes
        /// </summary>
        public const int Nodes = 64;  // Max 65 nodes when 0 is a valid node id

        /// <summary>
        /// Smaller profile with constraints for 8 base 32 character suffix, 17 year lifetime, 32 creations per second and 64 nodes
        /// </summary>
        public static readonly IdentityProfile SmallerYearProfile = CreateIdentityProfile(ResultPrefix, ResultSuffixLength, Lifetime_17_Years, CreationRate_17_Years, Nodes);

        /// <summary>
        /// Larger profile with constraints for 16 base 32 character suffix, 34 year lifetime, 32 creations per second and 64 nodes
        /// </summary>
        public static readonly IdentityProfile LargerYearProfile = CreateIdentityProfile(DeliveryPrefix, DeliveryTimeSuffixLength, Lifetime_34_Years, CreationRate_17_Years, Nodes);

        /// <summary>
        /// Identity profiles have the following characteristics:
        ///    - Prefix denotes the company and service
        ///    - Suffix length places constraints on lifetime, creation rate and nodes
        ///    - Lifetime in seconds before a collision is acceptable
        ///    - Creation rate is throughput in seconds before delaying to the next second
        ///    - Nodes prevents duplicates from application replicas in a cluster
        /// </summary>
        /// <param name="Prefix"></param>
        /// <param name="SuffixLength"></param>
        /// <param name="Lifetime"></param>
        /// <param name="CreationRate"></param>
        /// <param name="NodeCount"></param>
        /// <returns></returns>
        public static IdentityProfile CreateIdentityProfile(
            string prefix,
            byte suffixLength,
            int lifetime,
            int creationRate,
            int nodes
        )
        {
            if (suffixLength == 0 || suffixLength > Identity.MaxSuffixLength)
                throw new ArgumentException($"Suffix length must be between 1 and {Identity.MaxSuffixLength}");

            int _totalBits = suffixLength * 5; // Base 32 encoding means 5 bits per character

            // Node count bits
            int _nodes = Math.Max(1, nodes);
            int _nodesBits = (int)Math.Ceiling(Math.Log(_nodes, 2));
            if (_nodesBits < 0) _nodesBits = 0;

            // Timestamp bits either seconds or milliseconds based on lifetime
            long lifetimeSeconds = Math.Max(1, (long)lifetime); 
            long lifetimeMilliseconds = lifetimeSeconds * 1000L;

            int timestampBitsSeconds = (int)Math.Ceiling(Math.Log(lifetimeSeconds, 2)); 
            int timestampBitsMilliseconds = (int)Math.Ceiling(Math.Log(lifetimeMilliseconds, 2));

            // Creation rate bits
            int _creationRateBits = (int)Math.Ceiling(Math.Log(Math.Max(1, creationRate), 2));

            // Use milliseconds and seconds flags
            bool canUseMilliseconds = timestampBitsMilliseconds + _creationRateBits + _nodesBits <= _totalBits;
            bool canUseSeconds = timestampBitsSeconds + _creationRateBits + _nodesBits <= _totalBits;

            // Seconds preference over milliseconds
            bool _useMilliseconds = canUseMilliseconds && !canUseSeconds;

            int _timestampBits = _useMilliseconds ? timestampBitsMilliseconds : timestampBitsSeconds;

            // Sanity check total bits
            if (_timestampBits + _creationRateBits + _nodesBits > _totalBits)
                throw new InvalidOperationException("Adjust profile for lifetime, creation rate and nodes to fit inside suffix");

            // Remaining
            int _remainingBits = _totalBits - _timestampBits - _creationRateBits - _nodesBits;

            // Masks
            ulong _timestampMask = (_timestampBits >= 64) ? ulong.MaxValue : ((1UL << _timestampBits) - 1UL);
            ulong _creationRateMask = (_creationRateBits >= 64) ? ulong.MaxValue : ((1UL << _creationRateBits) - 1UL);
            ulong _nodesMask = (_nodesBits == 0) ? 0UL : ((_nodesBits >= 64) ? ulong.MaxValue : ((1UL << _nodesBits) - 1UL));

            IdentityProfile profile = new(
                prefix, 
                suffixLength, 
                _totalBits, 
                _timestampBits, 
                _creationRateBits, 
                _nodesBits,
                _remainingBits,
                _timestampMask,
                _creationRateMask,
                _nodesMask,
                _useMilliseconds
            );

            return profile;
        }
    }
}