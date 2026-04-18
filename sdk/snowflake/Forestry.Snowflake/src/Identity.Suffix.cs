using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Forestry.Snowflake.Tests")]

namespace Forestry.Snowflake
{
    public readonly partial struct Identity
    {
        private static readonly ConcurrentDictionary<IdentityProfile, Suffix> _suffixes = new();

        internal sealed class Suffix
        {
            /// <summary>
            /// Identity profile for this suffix constraints lifetime, creation rate and nodes
            /// </summary>
            private readonly IdentityProfile _profile;

            /// <summary>
            /// Clock when creating timestamp part either in seconds or milliseconds
            /// </summary>
            private readonly IClock _clock;

            /// <summary>
            /// High bits = lastTimestamp (ms), low bits = creation rate counter
            /// </summary>
            /// <remarks>As Int64 with limit timestamp + counter to <= 63 bits</remarks>
            private long _state; // layout: (lastTimestamp << counterBits) | counter

            /// <summary>
            /// Initialize with identity profile
            /// </summary>
            /// <param name="profile"></param>
            internal Suffix(IdentityProfile profile, IClock clock)
            {
                _profile = profile;
                _clock = clock;

                _state = 0L;
            }

            /// <summary>
            /// Create new suffix
            /// </summary>
            /// <param name="nodeId"></param>
            /// <returns></returns>
            /// <exception cref="ArgumentOutOfRangeException"></exception>
            internal string NewSuffix(byte nodeId)
            {
                // When node id is greater than the maximum allowed for the profile
                if (_profile.NodesBits > 0 && (ulong)nodeId > _profile.NodesMask)
                    throw new ArgumentOutOfRangeException(nameof(nodeId), "Node id overflow for nodes constraint");

                while (true)
                {
                    long systemNow = _profile.UseTimestampMilliseconds
                        ? _clock.NowMilliseconds()
                        : _clock.NowSeconds();

                    if (systemNow < 0) throw new InvalidOperationException("Invalid system time");

                    // Get last timestamp and creation rate counter from state
                    long currentState = Volatile.Read(ref _state);
                    long lastTimestamp = currentState >> _profile.CreationRateBits;
                    long creationRateCounter = currentState & (long)_profile.CreationRateMask;

                    // Either using same creation rate window with rolling timestamp or moving to the next
                    long normalizedNow = systemNow & (long)_profile.TimestampMask;  // normalize by masking timestamp size and protect against forward VM clock drift
                    long now = Math.Max(normalizedNow, lastTimestamp);  // protects against backward VM clock drift

                    if ((ulong)now > _profile.TimestampMask)
                        throw new InvalidOperationException("Invalid current timestamp exceeding a max value from profile mask");

                    // Lifetime overflow
                    if (normalizedNow < lastTimestamp)
                    {
                        LifetimeOverflowEventSource.Log.NewOverflowNode(normalizedNow, lastTimestamp, nodeId);
                    }

                    // Delay when same creation rate window and creation rate counter maxed
                    bool sameTick = now == lastTimestamp;
                    ulong incremented = ((ulong)creationRateCounter + 1UL) & _profile.CreationRateMask;

                    if (sameTick && incremented == 0UL)
                    {
                        CreationRateOverflowEventSource.Log.NewOverflow();
                        CreationRateOverflowEventSource.Log.NewOverflowNode(nodeId);

                        Thread.SpinWait(1);
                        continue;
                    }

                    // Define next state creation rate window and counter
                    long newState = ((now << _profile.CreationRateBits) | (long)incremented);

                    // Set next state after creating a suffix
                    if (Interlocked.CompareExchange(ref _state, newState, currentState) == currentState)  // clamp against same timestamp + counter pair and VM clock drift
                    {
                        // Define packable (timestamp|node|remaining|counter)
                        UInt128 packable = (UInt128)(ulong)now;
                        packable <<= (_profile.NodesBits + _profile.RemainingBits + _profile.CreationRateBits);

                        // Pack node
                        if (_profile.NodesBits > 0)
                        {
                            UInt128 node = (UInt128)((ulong)nodeId & _profile.NodesMask);
                            node <<= (_profile.RemainingBits + _profile.CreationRateBits);
                            packable |= node;
                        }

                        // Pack remaining
                        if (_profile.RemainingBits > 0)
                        {
                            ulong remainingMask = (1UL << _profile.RemainingBits) - 1UL;

                            Span<byte> bucket = stackalloc byte[8];
                            RandomNumberGenerator.Fill(bucket);
                            ulong leftover = BitConverter.ToUInt64(bucket) & remainingMask; // rolling

                            UInt128 remaining = (UInt128)leftover;
                            remaining <<= _profile.CreationRateBits;

                            packable |= remaining;
                        }

                        // Pack creation rate counter
                        packable |= (UInt128)incremented;

                        // Unpack to base 32 string with padding to suffix length
                        int len = _profile.SuffixLength;
                        Span<char> buffer = len <= 128 ? stackalloc char[len] : new char[len];

                        UnpackSuffix(packable, buffer, len);
                        return new string(buffer);
                    }
                }
            }
        }

        /// <summary>
        /// Get node using the suffix and profile constraints (lifetime, creation rate and nodes)
        /// </summary>
        /// <param name="suffix"></param>
        /// <param name="profile"></param>
        /// <returns></returns>
        /// <exception cref="FormatException">When suffix has invalid character</exception>
        internal static byte GetNode(string suffix, IdentityProfile profile)
        {
            // Decode Base32 → UInt128
            int invalidCharacter = 0;
            UInt128 packed = PackSuffix(suffix, ref invalidCharacter);

            if (invalidCharacter != 0)
            {
                throw new FormatException(Messages.Identity.IdentityInvalidSuffixCharacter);
            }

            // Remove creation-rate counter + remaining bits
            int shift = profile.RemainingBits + profile.CreationRateBits;
            UInt128 shifted = packed >> shift;

            // Mask out node bits
            ulong node = (ulong)shifted & profile.NodesMask;

            return (byte)node;
        }
    }

    /// <summary>
    /// Either seconds or milliseconds are used 
    /// by the timestamp part of the structured identity (i.e. the lifetime)
    /// </summary>
    internal interface IClock
    {
        /// <summary>
        /// Seconds
        /// </summary>
        /// <returns></returns>
        long NowSeconds();

        /// <summary>
        /// Milliseconds
        /// </summary>
        /// <returns></returns>
        long NowMilliseconds();
    }

    /// <summary>
    /// Internal clock using UTC time which does not handle summer (DST) 
    /// or winter time changes (by design).  UTC is a fixed and continuous 
    /// time standard with no:
    /// 
    /// - Daylight Saving Time (DST) adjustments
    /// - Summer/winter adjustments
    /// - Time zone shifts
    /// 
    /// UTC has always the same offset (UTC+00:00)
    /// </summary>
    internal class Clock : IClock
    {
        /// <summary>
        /// UTC Now in seconds since Unix epoch (January 1, 1970, 00:00:00 UTC)
        /// </summary>
        /// <returns></returns>
        public long NowSeconds() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        /// <summary>
        /// UTC Now in milliseconds since Unix epoch (January 1, 1970, 00:00:00 UTC)
        /// </summary>
        /// <returns></returns>
        public long NowMilliseconds() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}