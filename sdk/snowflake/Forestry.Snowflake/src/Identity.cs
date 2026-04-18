using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Forestry.Snowflake
{
    /// <summary>
    /// Identity creation without external resources that 
    /// adheres to application profile constraints on 
    /// creation rate, lifetime without duplicates and multi-node
    /// deployments.
    /// </summary>
    public readonly partial struct Identity : IEquatable<Identity>
    {
        /// <summary>
        /// Prefix encoded as base 36 (requires 6 bit packing)
        /// </summary>
        private readonly uint _prefix;

        /// <summary>
        /// Suffix encoded as base 32 (requires 5 bit packing)
        /// </summary>
        private readonly UInt128 _suffix;

        /// <summary>
        /// Prefix length (1-7 characters)
        /// </summary>
        private readonly byte _prefixLength;

        /// <summary>
        /// Suffix length (1-26 characters)
        /// </summary>
        private readonly byte _suffixLength;

        /// <summary>
        /// Base 36 alphabet with latin versal letters and digits
        /// </summary>
        /// <remarks>6 bit packing per character</remarks>
        public static readonly char[] Alphabet =
        [
            '0','1','2','3','4','5','6','7','8','9', // 0–9
            'A','B','C','D','E','F','G','H','I',     // 10–18
            'J','K','L','M','N','O','P','Q','R',     // 19–27
            'S','T','U','V','W','X','Y','Z'          // 28–35
        ];

        /// <summary>
        /// Base 32 alphabet <see cref="https://www.crockford.com/base32.html"/> excluding characters 
        /// that can easily be confused (I, L, O, U) 
        /// </summary>
        /// <remarks>5 bit packing per character</remarks>
        private static readonly char[] _crockfordAlphabet =
        [
            '0','1','2','3','4','5','6','7','8','9', // 0–9
            'A','B','C','D','E','F','G','H',         // 10–17
            'J','K','M','N','P','Q','R','S',         // 18–25
            'T','V','W','X','Y','Z'                  // 26–31
        ];

        /// <summary>
        /// Maximum 5 characters derives from base 36 alphabet where 2 raised 6 == 64 covering the alphabet
        /// </summary>
        public const byte MaxPrefixLength = 5;

        /// <summary>
        /// Maximum 21 derives from base 32 alphabet raised 128/6 == 21 (i.e. UInt128 space)
        /// </summary>
        public const byte MaxSuffixLength = 21;

        /// <summary>
        /// Create identity from a prefix and suffix seperated by either a dash or whitespace
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="FormatException"></exception>
        /// <remarks>Any starting or trailing whitespaces are ignored</remarks>
        public Identity(string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            // split by first '-' or whitespaces
            ReadOnlySpan<char> span = value.AsSpan();
            int separator = span.IndexOf('-');
            if (separator < 0)
            {
                separator = span.IndexOfAny([' ', '\t', '\r', '\n']);
            }

            if (separator < 0) throw new FormatException(Messages.Identity.IdentityUnrecognized);

            var prefix = span[..separator];
            var suffix = span[(separator + 1)..];

            var result = new IdentityResult(IdentityParseThrowStyle.All);
            bool success = TryParseIdentity(prefix, suffix, ref result);
            Debug.Assert(success, "IdentityParseThrowStyle.All means throw on all failures");

            this = result.ToIdentity();
        }

        /// <summary>
        /// Create identity from encoded prefix (base 36) and suffix (base 32)
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="suffix"></param>
        /// <param name="prefixLength"></param>
        /// <param name="suffixLength"></param>
        private Identity(uint prefix, UInt128 suffix, byte prefixLength, byte suffixLength)
        {
            _prefix = prefix;
            _suffix = suffix;
            _prefixLength = prefixLength;
            _suffixLength = suffixLength;
        }

        /// <summary>
        /// Try parse prefix and suffix into an identity result that
        /// optionally throwing parsing failures
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="suffix"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private static bool TryParseIdentity(ReadOnlySpan<char> prefix, ReadOnlySpan<char> suffix, ref IdentityResult result)
        {
            prefix = SpanTrim(prefix);
            suffix = SpanTrim(suffix);

            if (!(prefix.Length >= 1 && prefix.Length <= MaxPrefixLength))
            {
                result.SetFailure(ParseFailure.Format_WrongPrefixLength);
                return false;
            }

            if (!(suffix.Length >= 1 && suffix.Length <= MaxSuffixLength))
            {
                result.SetFailure(ParseFailure.Format_WrongSuffixLength);
                return false;
            }

            return TryParseExact(prefix, suffix, ref result);
        }

        /// <summary>
        /// Trim starting || ending whitespaces
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static ReadOnlySpan<char> SpanTrim(ReadOnlySpan<char> value)
        {
            int start = 0;
            int end = value.Length - 1;

            // Trim start
            while (start <= end && char.IsWhiteSpace(value[start]))
                start++;

            // Trim end
            while (end >= start && char.IsWhiteSpace(value[end]))
                end--;

            return value.Slice(start, end - start + 1);
        }

        /// <summary>
        /// Parsing with failure handling
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="suffix"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private static bool TryParseExact(ReadOnlySpan<char> prefix, ReadOnlySpan<char> suffix, ref IdentityResult result)
        {
            if (!(prefix.Length >= 1 && prefix.Length <= MaxPrefixLength))
            {
                result.SetFailure(ParseFailure.Format_WrongPrefixLength);
                return false;
            }
            result._prefixLength = (byte)prefix.Length;

            if (!(suffix.Length >= 1 && suffix.Length <= MaxSuffixLength))
            {
                result.SetFailure(ParseFailure.Format_WrongSuffixLength);
                return false;
            }

            result._suffixLength = (byte)suffix.Length;

            int invalidCharacter = 0;

            result._prefix = PackPrefix(prefix, ref invalidCharacter);

            if (invalidCharacter != 0)
            {
                result.SetFailure(ParseFailure.Format_InvalidPrefixCharacter);
                return false;
            }

            result._suffix = PackSuffix(suffix, ref invalidCharacter);

            if (invalidCharacter != 0)
            {
                result.SetFailure(ParseFailure.Format_InvalidSuffixCharacter);
                return false;
            }

            return true;
        }
        

        /// <summary> 
        /// Pack prefix as 6 bits without branching (e.g. for-loops)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="invalidCharacter"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint PackPrefix(ReadOnlySpan<char> value, ref int invalidCharacter)
        {
            uint v0 = value.Length > 0 ? Encode36(value[0], ref invalidCharacter) : 0u; // empty when false
            uint v1 = value.Length > 1 ? Encode36(value[1], ref invalidCharacter) : 0u;
            uint v2 = value.Length > 2 ? Encode36(value[2], ref invalidCharacter) : 0u;
            uint v3 = value.Length > 3 ? Encode36(value[3], ref invalidCharacter) : 0u;
            uint v4 = value.Length > 4 ? Encode36(value[4], ref invalidCharacter) : 0u;

            // shift (pack) each encoding by 6
            return
                (v0 << 0) |
                (v1 << 6) |
                (v2 << 12) |
                (v3 << 18) |
                (v4 << 24) ;
        }

        /// <summary>
        /// Encode character base 36 alphabet as unsigned integer
        /// </summary>
        /// <param name="c"></param>
        /// <param name="invalidCharacter"></param>
        /// <returns></returns>
        /// <remarks>Lowercase mapped to uppercase</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Encode36(char c, ref int invalidCharacter)
        {
            // normalize ASCII lowercase to uppercase by clearing bit 0x20 when in a..z
            uint uc = (uint)c;
            uint lowerMask = (uint)((uc - 'a') <= ('z' - 'a') ? 0xFFFFFFFFu : 0u);
            uc = (uc & ~((lowerMask) & 0x20u)); // convert a..z -> A..Z (branchless)

            uint dig = uc - (uint)'0';           // 0..?
            uint alpha = uc - (uint)'A';         // 0..?

            uint digMask = (uint)((dig <= 9) ? 0xFFFFFFFFu : 0u);
            uint alphaMask = (uint)((alpha <= 25) ? 0xFFFFFFFFu : 0u);

            uint value = (dig & digMask) | ((alpha + 10u) & alphaMask);

            // mark invalid without clearing prior marks
            if (((digMask | alphaMask) == 0u))
            {
                invalidCharacter |= 1;
            }

            return value;
        }

        /// <summary>
        /// Pack suffix as 5 bits without branching (e.g. no for-loops)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="invalidCharacter"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt128 PackSuffix(ReadOnlySpan<char> value, ref int invalidCharacter)
        {
            UInt128 v0 = value.Length > 0 ? Encode32(value[0], ref invalidCharacter) : 0u;
            UInt128 v1 = value.Length > 1 ? Encode32(value[1], ref invalidCharacter) : 0u;
            UInt128 v2 = value.Length > 2 ? Encode32(value[2], ref invalidCharacter) : 0u;
            UInt128 v3 = value.Length > 3 ? Encode32(value[3], ref invalidCharacter) : 0u;
            UInt128 v4 = value.Length > 4 ? Encode32(value[4], ref invalidCharacter) : 0u;
            UInt128 v5 = value.Length > 5 ? Encode32(value[5], ref invalidCharacter) : 0u;
            UInt128 v6 = value.Length > 6 ? Encode32(value[6], ref invalidCharacter) : 0u;
            UInt128 v7 = value.Length > 7 ? Encode32(value[7], ref invalidCharacter) : 0u;
            UInt128 v8 = value.Length > 8 ? Encode32(value[8], ref invalidCharacter) : 0u;
            UInt128 v9 = value.Length > 9 ? Encode32(value[9], ref invalidCharacter) : 0u;
            UInt128 v10 = value.Length > 10 ? Encode32(value[10], ref invalidCharacter) : 0u;
            UInt128 v11 = value.Length > 11 ? Encode32(value[11], ref invalidCharacter) : 0u;
            UInt128 v12 = value.Length > 12 ? Encode32(value[12], ref invalidCharacter) : 0u;
            UInt128 v13 = value.Length > 13 ? Encode32(value[13], ref invalidCharacter) : 0u;
            UInt128 v14 = value.Length > 14 ? Encode32(value[14], ref invalidCharacter) : 0u;
            UInt128 v15 = value.Length > 15 ? Encode32(value[15], ref invalidCharacter) : 0u;
            UInt128 v16 = value.Length > 16 ? Encode32(value[16], ref invalidCharacter) : 0u;
            UInt128 v17 = value.Length > 17 ? Encode32(value[17], ref invalidCharacter) : 0u;
            UInt128 v18 = value.Length > 18 ? Encode32(value[18], ref invalidCharacter) : 0u;
            UInt128 v19 = value.Length > 19 ? Encode32(value[19], ref invalidCharacter) : 0u;
            UInt128 v20 = value.Length > 20 ? Encode32(value[20], ref invalidCharacter) : 0u;

            return
                (v0 << 0) |
                (v1 << 5) |
                (v2 << 10) |
                (v3 << 15) |
                (v4 << 20) |
                (v5 << 25) |
                (v6 << 30) |
                (v7 << 35) |
                (v8 << 40) |
                (v9 << 45) |
                (v10 << 50) |
                (v11 << 55) |
                (v12 << 60) |
                (v13 << 65) |
                (v14 << 70) |
                (v15 << 75) |
                (v16 << 80) |
                (v17 << 85) |
                (v18 << 90) |
                (v19 << 95) |
                (v20 << 100);
        }

        /// <summary>
        /// Encodes character to base 32 alphabet (i.e. assigns the Crockford 
        /// alphabet to integers)
        /// </summary>
        /// <param name="c"></param>
        /// <param name="invalidCharacter"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Encode32(char c, ref int invalidCharacter)
        {
            // normalize ASCII lowercase to uppercase by clearing bit 0x20 when in a..z
            uint uc = (uint)c;
            uint lowerMask = (uint)((uc - 'a') <= ('z' - 'a') ? 0xFFFFFFFFu : 0u);
            uc = (uc & ~(lowerMask & 0x20u)); // convert a..z -> A..Z

            // digit range
            uint dig = uc - '0';
            uint digMask = (uint)((dig <= 9) ? 0xFFFFFFFFu : 0u);

            // alpha range (A–Z)
            uint alpha = uc - 'A';
            uint alphaRangeMask = (uint)((alpha <= 25) ? 0xFFFFFFFFu : 0u);

            // Crockford remapping table for A–Z (branchless)
            // Maps A..Z → Crockford values or 0xFF for invalid letters (I,L,O,U)
            ReadOnlySpan<byte> map =
            [
                10,11,12,13,14,15,16,17, // A–H
                0xFF,                    // I (invalid)
                18,19,                   // J,K
                0xFF,                    // L (invalid)
                20,21,                   // M,N
                0xFF,                    // O (invalid)
                22,23,24,25,26,          // P,Q,R,S,T
                0xFF,                    // U (invalid)
                27,28,29,30,31           // V,W,X,Y,Z
            ];

            uint safeAlpha = alpha & alphaRangeMask; // 0..25 or 0  
            uint alphaMappedValue = map[(int)safeAlpha];

            // alphaMappedValue == 0xFF means invalid letter
            uint alphaMask = (uint)((alphaRangeMask != 0 && alphaMappedValue != 0xFF) ? 0xFFFFFFFFu : 0u);

            // combine digit or alpha result
            uint value = (dig & digMask) | (alphaMappedValue & alphaMask);

            // mark invalid
            if ((digMask | alphaMask) == 0u)
                invalidCharacter |= 1;

            return value;
        }


        /// <summary>
        /// Unpack internal prefix without branching
        /// </summary>
        /// <remarks>0x3F == 6-bit mask</remarks>
        /// <param name="packed"></param>
        /// <param name="value"></param>
        private static void UnpackPrefix(uint packed, Span<char> value, int length)
        {
            for (int i = 0; i < length; i++)
            {
                value[i] = Decode36((packed >> (i * 6)) & 0x3Fu);
            }
        }

        /// <summary>
        /// Decode using the base 36 alphabet to character
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static char Decode36(uint value)
        {
            return Alphabet[(int)value];
        }

        /// <summary>
        /// Unpack suffix without branching 
        /// </summary>
        /// <remarks>0x1F == 0 to 31 mask</remarks>
        /// <param name="packed"></param>
        /// <param name="value"></param>
        private static void UnpackSuffix(UInt128 packed, Span<char> value, int length)
        {
            if (length > 0) value[0] = Decode32((packed >> 0) & 0x1Fu);
            if (length > 1) value[1] = Decode32((packed >> 5) & 0x1Fu);
            if (length > 2) value[2] = Decode32((packed >> 10) & 0x1Fu);
            if (length > 3) value[3] = Decode32((packed >> 15) & 0x1Fu);
            if (length > 4) value[4] = Decode32((packed >> 20) & 0x1Fu);
            if (length > 5) value[5] = Decode32((packed >> 25) & 0x1Fu);
            if (length > 6) value[6] = Decode32((packed >> 30) & 0x1Fu);
            if (length > 7) value[7] = Decode32((packed >> 35) & 0x1Fu);
            if (length > 8) value[8] = Decode32((packed >> 40) & 0x1Fu);
            if (length > 9) value[9] = Decode32((packed >> 45) & 0x1Fu);
            if (length > 10) value[10] = Decode32((packed >> 50) & 0x1Fu);
            if (length > 11) value[11] = Decode32((packed >> 55) & 0x1Fu);
            if (length > 12) value[12] = Decode32((packed >> 60) & 0x1Fu);
            if (length > 13) value[13] = Decode32((packed >> 65) & 0x1Fu);
            if (length > 14) value[14] = Decode32((packed >> 70) & 0x1Fu);
            if (length > 15) value[15] = Decode32((packed >> 75) & 0x1Fu);
            if (length > 16) value[16] = Decode32((packed >> 80) & 0x1Fu);
            if (length > 17) value[17] = Decode32((packed >> 85) & 0x1Fu);
            if (length > 18) value[18] = Decode32((packed >> 90) & 0x1Fu);
            if (length > 19) value[19] = Decode32((packed >> 95) & 0x1Fu);
            if (length > 20) value[20] = Decode32((packed >> 100) & 0x1Fu);
        }


        /// <summary>
        /// Decode base 32 value to character using Crockford alphabet
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static char Decode32(UInt128 value)
        {
            // value must be 0..31
            return _crockfordAlphabet[(int)value];
        }

        /// <summary>
        /// Identity result is an intermediate struct used when parsing 
        /// and initials the readonly fields of the identity struct when 
        /// initializing (e.g. readonly fields have to be set in the constructor, 
        /// but parsing is a multi-step process that requires mutability)
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct IdentityResult
        {
            internal uint _prefix;

            internal UInt128 _suffix;

            internal byte _prefixLength;

            internal byte _suffixLength;

            private readonly IdentityParseThrowStyle _throwStyle;

            internal IdentityResult(IdentityParseThrowStyle throwStyle) : this()
            {
                _prefix = 0;
                _suffix = 0;

                _prefixLength = 0;
                _suffixLength = 0;

                _throwStyle = throwStyle;
            }

            internal readonly void SetFailure(ParseFailure failureKind)
            {
                if (_throwStyle == IdentityParseThrowStyle.None) return;

                throw new FormatException(failureKind switch
                {
                    ParseFailure.Format_NullOrEmpty => Messages.Identity.IdentityEmpty,
                    ParseFailure.Format_Whitespace => Messages.Identity.IdentityWhitespace,
                    ParseFailure.Format_WrongPrefixLength => Messages.Identity.IdentityWrongPrefixLength,
                    ParseFailure.Format_InvalidPrefixCharacter => Messages.Identity.IdentityInvalidPrefixCharacter,
                    ParseFailure.Format_WrongSuffixLength => Messages.Identity.IdentityWrongSuffixLength,
                    ParseFailure.Format_InvalidSuffixCharacter => Messages.Identity.IdentityInvalidSuffixCharacter,
                    _ => Messages.Identity.IdentityUnrecognized
                });
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly Identity ToIdentity()
            {
                return new Identity(_prefix, _suffix, (byte)_prefixLength, (byte)_suffixLength);
            }
        }

        /// <summary>
        /// Create new identity from profile (single node deployments)
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        public static Identity NewIdentity(IdentityProfile profile)
        {
            string prefix = profile.Prefix.ToUpperInvariant();
            string suffix = _suffixes.GetOrAdd(profile, p => new Suffix(p, new Clock())).NewSuffix(0);

            return new Identity($"{prefix}-{suffix}");
        }

        /// <summary>
        /// Create new identity from profile (multi node deployments)
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        public static Identity NewIdentity(IdentityProfile profile, byte nodeId)
        {
            string prefix = profile.Prefix.ToUpperInvariant();
            string suffix = _suffixes.GetOrAdd(profile, p => new Suffix(p, new Clock())).NewSuffix(nodeId);

            return new Identity($"{prefix}-{suffix}");
        }

        /// <summary>
        /// Identity as string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            int pLen = _prefixLength;
            int sLen = _suffixLength;

            if (pLen < 1 && sLen < 1) return string.Empty;

            char[] combined = new char[pLen + sLen];

            if (pLen > 0)
                UnpackPrefix(_prefix, combined.AsSpan(0, pLen), pLen);

            if (sLen > 0)
                UnpackSuffix(_suffix, combined.AsSpan(pLen, sLen), sLen);

            return new string(combined);
        }

        /// <summary>
        /// Identity as formated string
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        public string ToString(string format)
        {
            if (string.IsNullOrEmpty(format)) return ToString();

            switch (format[0] | 0x20)
            {
                case 'd':
                    return ToString('-');
                case 's':
                    return ToString(' ');
                default:
                    throw new FormatException($"Format specifier '{format}' is not supported.");
            }
        }

        /// <summary>
        /// Identity as string where the separator is between the prefix and suffix
        /// </summary>
        /// <param name="separator"></param>
        /// <returns></returns>
        private string ToString(char separator)
        {
            int prefixLength = _prefixLength;
            int suffixLength = _suffixLength;

            if (prefixLength <= 0 && suffixLength <= 0) return string.Empty;

            // allocate exact sized buffers
            char[] prefixBuffer = prefixLength > 0 ? new char[prefixLength] : [];
            char[] suffixBuffer = suffixLength > 0 ? new char[suffixLength] : [];

            if (prefixLength > 0) UnpackPrefix(_prefix, prefixBuffer, prefixLength);
            if (suffixLength > 0) UnpackSuffix(_suffix, suffixBuffer, suffixLength);

            char[] combined = new char[prefixLength + 1 + suffixLength];
            Array.Copy(prefixBuffer, 0, combined, 0, prefixLength);

            combined[prefixLength] = separator;

            if (suffixLength > 0) Array.Copy(suffixBuffer, 0, combined, prefixLength + 1, suffixLength);
            return new string(combined);
        }

        /// <summary>
        /// Equality
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Identity other)
        {
            return _prefix == other._prefix
                && _suffix == other._suffix
                && _prefixLength == other._prefixLength
                && _suffixLength == other._suffixLength;
        }

        /// <summary>
        /// Equality
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals([NotNullWhen(true)] object? obj) => obj is Identity other && Equals(other);

        /// <summary>
        /// Hash code
        /// </summary>
        /// <returns></returns>
        /// <remarks>Usage dictionaries</remarks>
        public override int GetHashCode()
        {
            unchecked
            {
                // combine prefix (uint), suffix (UInt128), and lengths
                int hash = (int)_prefix;
                hash = (hash * 31) ^ (int)(_suffix & 0xFFFFFFFF);
                hash = (hash * 31) ^ (int)(_suffix >> 32);
                hash = (hash * 31) ^ _prefixLength;
                hash = (hash * 31) ^ _suffixLength;
                return hash;
            }
        }

        public static bool operator ==(Identity left, Identity right) => left.Equals(right);

        public static bool operator !=(Identity left, Identity right) => !left.Equals(right);

        /// <summary>
        /// Throw failure flag when parsing
        /// </summary>
        private enum IdentityParseThrowStyle : byte
        {
            None = 0,
            All = 1
        }

        /// <summary>
        /// Possible parsing failures
        /// </summary>
        private enum ParseFailure
        {
            Format_NullOrEmpty,
            Format_Whitespace,
            Format_WrongPrefixLength,
            Format_InvalidPrefixCharacter,
            Format_WrongSuffixLength,
            Format_InvalidSuffixCharacter,
            Format_Unrecognized
        }
    }
}