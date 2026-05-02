namespace Forestry.Raindrop {
    /// <summary>
    /// New identities are declared from profiles and 
    /// profiles are meant to be constants by use cases. 
    /// 
    /// The profile hard codes the prefix and shapes the 
    /// length of the suffix plus counters (bits) for each segment 
    /// with overflow help from masks.
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
    public readonly record struct Profile(
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
    ) {
        /// <summary>
        /// Identity profiles have the following characteristics:
        ///    - Prefix
        ///    - Suffix length constrained by policies on lifetime, creation rate and nodes
        ///    - Lifetime in seconds || milliseconds before duplicates are possible
        ///    - Creation rate == throughput per second before duplicates are avoided by delays
        ///    - Nodes prevent duplicates between clustered applications
        /// </summary>
        /// <param name="Prefix"></param>
        /// <param name="SuffixLength"></param>
        /// <param name="Lifetime"></param>
        /// <param name="CreationRate"></param>
        /// <param name="NodeCount"></param>
        /// <returns></returns>
        public static Profile Create(
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

            Profile profile = new(
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