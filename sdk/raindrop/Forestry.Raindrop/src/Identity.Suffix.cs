using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Forestry.Raindrop.Tests")]

namespace Forestry.Raindrop
{
    /// <summary>
    /// Identity suffix creation using the CrockFord alphabet and policies on lifetime, creation rate and the number 
    /// of runtime nodes.
    /// 
    /// The code can be hairy so with any problems copy in Claud or Co-pilot for a better explanation than 
    /// the comments can yield.
    /// </summary>
    public readonly partial struct Identity
    {
        /// <summary>
        /// Retain suffix state by profile avoiding unneeded rollover and expensive declaration 
        /// </summary>
        /// <returns></returns>
        private static readonly ConcurrentDictionary<Profile, Suffix> _suffixes = new();

        /// <summary>
        /// Suffix state
        /// </summary>
        internal sealed class Suffix
        {
            /// <summary>
            /// Identity profile for this suffix constraints lifetime, creation rate and nodes
            /// </summary>
            private readonly Profile _profile;

            /// <summary>
            /// Clock when creating timestamp part either in seconds or milliseconds
            /// </summary>
            private readonly IClock _clock;

            /// <summary>
            /// High bits = lastTimestamp (ms), low bits = created counter
            /// </summary>
            /// <remarks>As Int64 with limit timestamp + counter to <= 63 bits</remarks>
            private long _state; // layout: (lastTimestamp << counterBits) | counter

            /// <summary>
            /// Initialize with identity profile
            /// </summary>
            /// <param name="profile"></param>
            internal Suffix(Profile profile, IClock clock)
            {
                _profile = profile;
                _clock = clock;

                _state = 0L;
            }

            /// <summary>
            /// Create new suffix using profile policies with a node id
            /// </summary>
            /// <param name="nodeId"></param>
            /// <returns></returns>
            /// <exception cref="ArgumentOutOfRangeException"></exception>
            internal string NewSuffix(byte nodeId)
            {
                // When node id is greater than the maximum allowed for the profile
                if (_profile.NodesBits > 0 && (ulong)nodeId > _profile.NodesMask)
                    throw new ArgumentOutOfRangeException(nameof(nodeId), "Node id overflow for runtime nodes policy");

                while (true)
                {
                    // Get system timestamp either in seconds (default) or milliseconds
                    long systemNow = _profile.UseTimestampMilliseconds
                        ? _clock.NowMilliseconds()
                        : _clock.NowSeconds();

                    if (systemNow < 0) throw new InvalidOperationException("Invalid system time");

                    // Get last timestamp and created counter from state
                    long currentState = Volatile.Read(ref _state);
                    long lastTimestamp = currentState >> _profile.CreationRateBits;
                    long createdCounter = currentState & (long)_profile.CreationRateMask;

                    // Get current timestamp
                    long normalizedNow = systemNow & (long)_profile.TimestampMask;  // protects against forward VM clock drift
                    long now = Math.Max(normalizedNow, lastTimestamp);  // protects against backward VM clock drift

                    if ((ulong)now > _profile.TimestampMask)
                        throw new InvalidOperationException("Current timestamp exceeds the maximum lifetime policy");

                    // Lifetime overflow
                    if (normalizedNow < lastTimestamp)
                    {
                        LifetimeOverflowEventSource.Log.NewOverflowNode(normalizedNow, lastTimestamp, nodeId);
                    }

                    // Delay when same tick (second || millisecond) and created counter overflow i.e. duplicate protection
                    bool sameTick = now == lastTimestamp;
                    ulong incremented = ((ulong)createdCounter + 1UL) & _profile.CreationRateMask;

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
                        // Pack current timestamp then define what comes next i.e. node, remaining, created counter
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

                        // Pack created counter
                        packable |= (UInt128)incremented;

                        // Unpack to base 32 alphabet with padding
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
        internal static byte GetNode(string suffix, Profile profile)
        {
            // Decode Base32 → UInt128
            int invalidCharacter = 0;
            UInt128 packed = PackSuffix(suffix, ref invalidCharacter);

            if (invalidCharacter != 0)
            {
                throw new FormatException(Messages.Identity.InvalidSuffixCharacter);
            }

            // Remove creation-rate counter + remaining bits
            int shift = profile.RemainingBits + profile.CreationRateBits;
            UInt128 shifted = packed >> shift;

            // Mask out node bits
            ulong node = (ulong)shifted & profile.NodesMask;

            return (byte)node;
        }
    }
}