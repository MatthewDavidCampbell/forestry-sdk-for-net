using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
using Xunit;
using static Forestry.Raindrop.Identity;

namespace Forestry.Raindrop.Tests
{
    /// <summary>
    /// Unit tests for identity creation should show:
    /// 
    /// - Prefix validation
    /// - Suffix validation (e.g. only CrockFord characters)
    /// - Profiles that create correct bit sizes for lifetimes, creation rate, nodes and their masks (i.e. max values)
    /// - Timestamp part with overflow handling (e.g. VM clock drift, rollover, no DST)
    /// - Creation rate part with overflow handling
    /// - Nodes part
    /// - Remaining part when suffix length is bigger than timestamp + creation rate + nodes bits
    /// - Formatting
    /// - Equality
    /// </summary>
    public class IdentityTests
    {
        #region Prefix
        /// <summary>
        /// Prefix must have a minimum 1 character
        /// </summary>
        [Fact]
        public void When_PrefixTooSmall_ItShould_ThrowFormatException()
        {
            // Arrange
            Profile profile = Profile.Create(string.Empty, 8, 268056000, 64, 64);

            // Act + Assert
            FormatException exception = Assert.Throws<FormatException>(() => NewIdentity(profile));
            Assert.Equal(Messages.Identity.IsEmpty, exception.Message);
        }

        /// <summary>
        /// Prefix may not be null
        /// </summary>
        [Fact]
        public void When_PrefixNull_ItShould_ThrowFormatException()
        {
            // Arrange
            Profile profile = Profile.Create(default!, 8, 268056000, 64, 64);

            // Act + Assert
            FormatException exception = Assert.Throws<FormatException>(() => NewIdentity(profile));
            Assert.Equal(Messages.Identity.IsEmpty, exception.Message);
        }

        /// <summary>
        /// Prefix must have a maximum of 5 characters
        /// </summary>
        [Fact]
        public void When_PrefixTooLarge_ItShould_ThrowFormatException()
        {
            // Arrange
            Profile profile = Profile.Create("ABCDEF", 8, 268056000, 64, 64);

            // Act + Assert (only 5 latin versals and digits allowed)
            Assert.Throws<FormatException>(() => NewIdentity(profile));
        }

        /// <summary>
        /// Prefix must only contain latin versals and digits
        /// </summary>
        [Fact]
        public void When_PrefixHasBadCharacter_ItShould_ThrowFormatException()
        {
            // Arrange
            Profile profile = Profile.Create("$BCDEF", 8, 268056000, 64, 64);  // Only latin versals and digits

            // Act + Assert
            Assert.Throws<FormatException>(() => NewIdentity(profile));
        }

        /// <summary>
        /// Prefix must not contain whitespaces
        /// </summary>
        [Fact]
        public void When_PrefixHasWhitespace_ItShould_ThrowFormatException()
        {
            // Arrange
            Profile profile = Profile.Create("ABC D", 8, 268056000, 64, 64);

            // Act + Assert
            FormatException exception = Assert.Throws<FormatException>(() => NewIdentity(profile));
            Assert.Equal(Messages.Identity.InvalidPrefixCharacter, exception.Message);
        }

        /// <summary>
        /// Prefix trims leading whitespaces
        /// </summary>
        [Fact]
        public void When_PrefixStartingWhitespace_ItShould_Ignore()
        {
            // Arrange
            Profile profile = Profile.Create(" ABCD", 8, 268056000, 64, 64);

            // Act + Assert
            Identity identity = NewIdentity(profile);
            Assert.True(identity.ToString("d").StartsWith('A'));
        }

        /// <summary>
        /// Prefix trims ending whitespaces
        /// </summary>
        [Fact]
        public void When_PrefixEndingWhitespace_ItShould_Ignore()
        {
            // Arrange
            Profile profile = Profile.Create("ABCD ", 8, 268056000, 64, 64);

            // Act + Assert
            Identity identity = NewIdentity(profile);
            Assert.True(identity.ToString("d")[3] == 'D');
        }

        /// <summary>
        /// Prefix converts lowercase to uppercase
        /// </summary>
        [Fact]
        public void When_PrefixLowercase_ItShould_ToUppercase()
        {
            // Arrange
            Profile profile = Profile.Create("abcd", 8, 268056000, 64, 64);

            // Act + Assert
            Identity identity = NewIdentity(profile);
            Assert.True(identity.ToString("d").StartsWith('A'));
        }

        /// <summary>
        /// Prefix only latin versals (i.e. A-Z, no Ä, Ö, Å etc.)
        /// </summary>
        [Fact]
        public void When_NonLatin_ItShould_ThrowFormatException()
        {
            // Arrange
            Profile profile = Profile.Create("ÄBC", 8, 268056000, 64, 64);

            // Act + Assert
            FormatException exception = Assert.Throws<FormatException>(() => NewIdentity(profile));
            Assert.Equal(Messages.Identity.InvalidPrefixCharacter, exception.Message);
        }
        #endregion

        #region Suffix
        /// <summary>
        /// CrockFord Base32 characters only (i.e. 0-9, A-Z except for O, U, I, L)
        /// </summary>
        [Fact]
        public void When_NewIdentity_ItShould_OnlyCrockFordSuffix()
        {
            // Arrange + Act + Assert
            Assert.Throws<FormatException>(() => new Identity("XXXX-OABC1234"));  // O
            Assert.Throws<FormatException>(() => new Identity("XXXX-UABC1234"));  // U
            Assert.Throws<FormatException>(() => new Identity("XXXX-IABC1234"));  // I
            Assert.Throws<FormatException>(() => new Identity("XXXX-LABC1234"));  // L
        }

        /// <summary>
        /// Suffix must have a maximum of 21 characters adhering to a Biometria policy 
        /// even though UInt128 has space for 25
        /// </summary>
        [Fact]
        public void When_SuffixTooLarge_ItShould_ThrowFormatException()
        {
            // Arrange
            FormatException exception = Assert.Throws<FormatException>(() => new Identity("XXXX-01234567890123456789012")); // 22 characters
            Assert.Equal(Messages.Identity.WrongSuffixLength, exception.Message);
        }
        #endregion

        #region Profiles
        /// <summary>
        /// Profile with timestamp (lifetime), creation rate and nodes within masks (i.e. max values)
        /// </summary>
        [Fact]
        public void When_NewIdentity_ItShould_HaveCorrectBitSizes()
        {
            // Arrange
            Profile profile = Profile.Create("ABCD", 8, 268056000, 64, 64);

            // Act + Assert
            Assert.Equal(28, profile.TimestampBits);   // 2^28 == 268 435 456 seconds / (60 * 60 * 24 * 365) == 8.5 years
            Assert.Equal(6, profile.CreationRateBits); // 2^6 == 64 max tick inside second window
            Assert.Equal(6, profile.NodesBits);        // 2^6 == 64 max nodes
        }

        /// <summary>
        /// Profile showing the creation and nodes masks (i.e. max values) and rolling 
        /// the timestamp over after lifetime (i.e. rollover after 8.5 years)
        /// </summary>
        [Fact]
        public void When_NewIdentity_ItShould_HaveCorrectMasks()
        {
            // Arrange (where 268056000 seconds == 8.5 years)
            Profile profile = Profile.Create("ABCD", 8, 268056000, 64, 64);

            // Act + Assert
            Assert.True(profile.CreationRateMask == (1UL << profile.CreationRateBits) - 1UL);  // mask == creation rate - 1 
            Assert.True(profile.NodesMask == (1UL << profile.NodesBits) - 1UL);                // mask == nodes - 1 

            Assert.Equal(0, (1u << 28) & (int)profile.TimestampMask); // rolling over (i.e. overflow) after 8.5 years
        }

        /// <summary>
        /// Profile uses UTC second windows when lifetime is small (i.e. 8.5 years) to make room for higher creation rate and nodes
        /// </summary>
        [Fact]
        public void When_NewIdentitySmallLifetime_ItShould_SecondsPreference()
        {
            // Arrange
            Profile profile = Profile.Create("ABCD", 8, 268056000, 64, 64);

            // Act + Assert
            Assert.False(profile.UseTimestampMilliseconds);
        }

        /// <summary>
        /// Bad profile when suffix length is too small to fit timestamp, creation rate and nodes bits
        /// </summary>
        [Fact]
        public void When_NewIdentitySuffixLengthTooSmall_ItShould_Throw()
        {
            // Arrange + Act + Arrange
            Assert.Throws<InvalidOperationException>(() => Profile.Create("ABCD", 7, 268056000, 64, 64));
        }

        /// <summary>
        /// Non multi-node profile could have zero nodes but still work by filling up the remaining part
        /// </summary>
        [Fact]
        public void When_NewIdentityWithoutNodes_ItShould_HaveZeroNodesBits()
        {
            // Arrange
            Profile profile = Profile.Create("ABCD", 8, 268056000, 64, -1);

            // Act + Assert
            Assert.Equal(0, profile.NodesBits);
        }

        [Fact]
        public void When_SampleProfile_ItShould_HavePrefixAndLength()
        {
            // Arrange
            Profile profile = IdentityProfiles.SampleProfile;

            // Act
            Identity identity = NewIdentity(profile);
            var value = identity.ToString();

            // Assert    
            Assert.StartsWith(IdentityProfiles.ForestryPrefix, value);
            Assert.Equal(11, value.Length);
        }
        #endregion

        #region Timestamp
        /// <summary>
        /// Timestamp rollover creating identities until the rollover point is reached again
        /// </summary>
        [Fact]
        public void When_TimestampRollover_ItShould_NotDecrease()
        {
            // Arrange
            FakeClock clock = new();
            Profile profile = Profile.Create("ABCD", 8, 268056000, 64, 64);
            Identity.Suffix suffix = new(profile, clock);

            long mask = (long)profile.TimestampMask; // last timestamp value 

            // Act
            clock.Value = mask;
            string before = suffix.NewSuffix(0);

            clock.Value = mask + 1; // rollover
            string after = suffix.NewSuffix(0);

            // Assert
            Assert.True(string.CompareOrdinal(after, before) > 0);
        }

        /// <summary>
        /// DST (summer and winter time) jumps don't effect identity creation
        /// </summary>
        [Fact]
        public void When_DSTJump_ItShould_NotEffectNewIdentities()
        {
            // Arrange
            FakeClock clock = new() { Value = 1711843200 };
            Profile profile = Profile.Create("ABCD", 8, 268056000, 64, 64);

            var suffix = new Suffix(profile, clock);

            // Act
            string before = suffix.NewSuffix(nodeId: 1);

            TimeZoneInfo.ClearCachedData(); // Simulate DST jump in local time but UTC not effected
            clock.Value = 1711843200;

            var after = suffix.NewSuffix(nodeId: 1);

            // Assert
            Assert.NotNull(before);
            Assert.NotNull(after);
            Assert.NotEqual(before, after); // creation rate counter incremented
        }

        /// <summary>
        /// Listener when timestamp rollover occurs (after 8.5 years) and duplicate identities are now possible
        /// </summary>
        [Fact]
        public void When_LifeTimeRollover_ItShould_FireEvent() { 
            // Arrange
            var listener = new LifetimeOverflowListener();
            Profile profile = Profile.Create("ABCD", 8, 268056000, 64, 64);

            FakeClock clock = new() { };
            var suffix = new Suffix(profile, clock);

            // Act
            clock.Value = 200; // last timestamp == 200
            suffix.NewSuffix(nodeId: 1);

            clock.Value = 199; // just before timestamp rollover
            suffix.NewSuffix(nodeId: 1);

            // Assert
            Assert.Contains(listener.Events, e => e.EventName == "NewOverflowNode" && e.EventSource.Name == LifetimeOverflowEventName);
        }
        #endregion

        #region Creation rate
        /// <summary>
        /// VM Clock drift going back should never decrease the suffix (i.e. creation rate should keep increasing until rollover to next timestamp window)
        /// </summary>
        [Fact]
        public void When_ClockDriftBack_ItShould_NeverDecreaseSuffix()
        {
            // Arrange
            FakeClock clock = new() { Value = 1000 };
            Profile profile = Profile.Create("ABCD", 8, 268056000, 64, 64);
            Suffix suffix = new(profile, clock);

            // Act
            string before = suffix.NewSuffix(0);
            clock.Value = 900; // clock drift back
            string after = suffix.NewSuffix(0);

            // Assert
            Assert.True(string.CompareOrdinal(after, before) > 0);
        }

        /// <summary>
        /// VM Clock drift going forward should stay in the same timestamp window (i.e. creation rate should keep increasing until rollover to next timestamp window) 
        /// and never decrease the suffix
        /// </summary>
        [Fact]
        public void When_ClockDriftForward_ItShould_StayInTimeWindow()
        {
            // Arrange
            FakeClock clock = new() { Value = 1000 };
            Profile profile = Profile.Create("ABCD", 8, 268056000, 64, 64);
            Suffix suffix = new(profile, clock);

            // Act
            string before = suffix.NewSuffix(0);
            clock.Value = long.MaxValue; ; // clock drift forward
            string after = suffix.NewSuffix(0);

            // Assert
            Assert.True(string.CompareOrdinal(after, before) > 0);
        }

        /// <summary>
        /// Creation rate rollover should wait for the next timestamp window (i.e. spin-wait until next second) and never decrease the suffix
        /// </summary>
        /// <remarks>Using an event source listener to move the next timestamp window</remarks>
        [Fact]
        public void When_CreationRateRollover_ItShould_WaitNextTimestamp()
        {
            // Arrange
            FakeClock clock = new() { Value = 1000 };

            Profile profile = Profile.Create("ABCD", 8, 268056000, 64, 64);
            Suffix suffix = new(profile, clock);

            CreationRateOverflowListener _ = new(clock);

            // Act
            int maximumCreationRate = (1 << profile.CreationRateBits);
            for (int i = 0; i < maximumCreationRate; i++)
            {
                suffix.NewSuffix(0); // fill up the creation rate counter
            }

            string after = suffix.NewSuffix(0);  // after timestamp overflow in listener

            // Assert (spin-wait to next timestamp window)
            Assert.NotNull(after);
        }

        /// <summary>
        /// Parrallel thread identity creation should have no duplicates even when creation 
        /// rate rolls over (i.e. multiple Rebus workers creating identities in parallel and hitting the same creation rate overflow at the same time)
        /// </summary>
        [Fact]
        public void When_ConcurrentCreationRates_ItShould_HaveNoDuplicates()
        {
            // Arrange
            FakeClock clock = new() { Value = 1000 };

            Profile profile = Profile.Create("ABCD", 8, 268056000, 64, 64);
            Suffix suffix = new(profile, clock);

            CreationRateOverflowListener _ = new(clock);

            // Act
            var identities = new ConcurrentDictionary<string, byte>();

            // Assert
            Parallel.For(0, 5000, _ => {
                var id = suffix.NewSuffix(0);
                Assert.True(identities.TryAdd(id, 0), "Duplicate ID detected");
            });
        }
        #endregion

        #region Nodes
        /// <summary>
        /// Node id should be within the nodes defined in the profile (i.e. 0-64 for 6 bits) and throw if not
        /// </summary>
        [Fact]
        public void When_NewIdentityNodeTooBig_ItShould_ThrowOutOfRange()
        {
            // Arrange
            Profile profile = Profile.Create("ABCD", 8, 268056000, 64, 64);

            // Act + Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => NewIdentity(profile, 66));
        }

        /// <summary>
        /// Node id should be extractable from the suffix and be the same as the one set when creating the identity
        /// </summary>
        [Fact]
        public void When_SettingNodeId_ItShould_ExtractTheSameId()
        {
            // Arrange
            Profile profile = Profile.Create("ABCD", 8, 268056000, 64, 64);

            // Act
            Identity identity = NewIdentity(profile, 5);

            // Assert
            Assert.Equal(5, GetNode(identity.ToString("s").Split()[1], profile));
        }
        #endregion

        #region Remaining
        [Fact]
        public void When_NewIdentityWithRemaining_ItShould_FillOut()
        {
            // Arrange
            Profile profile = Profile.Create("ABCD", 16, 268056000, 64, -1);

            // Act + Assert
            Assert.Equal(20, NewIdentity(profile, 5).ToString().Length);
        }
        #endregion

        #region Formatting
        /// <summary>
        /// Identity as string with a dash between the prefix and suffix
        /// </summary>
        [Fact]
        public void When_NewIdentityFormatDash_ItShould_HaveDash()
        {
            // Arrange
            Profile profile = Profile.Create("ABCD", 8, 268056000, 64, 64);

            // Act + Assert
            Identity identity = NewIdentity(profile);
            Assert.True(identity.ToString("d").Contains('-'));
        }

        /// <summary>
        /// Identity as string with a space between the prefix and suffix
        /// </summary>
        [Fact]
        public void When_NewIdentityFormatSpace_ItShould_HaveSpace()
        {
            // Arrange
            Profile profile = Profile.Create("ABCD", 8, 268056000, 64, 64);

            // Act + Assert
            Identity identity = NewIdentity(profile);
            Assert.True(identity.ToString("s").Contains(' '));
        }

        /// <summary>
        /// Identity with default format should have nothing between the prefix and suffix
        /// </summary>
        [Fact]
        public void When_NewIdentityFormatNone_ItShould_HaveNeitherDashOrSpaces()
        {
            // Arrange
            Profile profile = Profile.Create("ABCD", 8, 268056000, 64, 64);

            // Act + Assert
            Identity identity = NewIdentity(profile);
            Assert.True(identity.ToString()[4] != '-' || identity.ToString()[4] != ' ');
        }

        /// <summary>
        /// Identity as string should have same length as the suffix length defined in the profile
        /// </summary>
        [Fact]
        public void When_NewIdentity_ItShould_HaveSameSuffixLength()
        {
            // Arrange
            Profile profile = Profile.Create("ABCD", 8, 268056000, 64, 64);

            // Act + Assert
            Identity identity = NewIdentity(profile);
            Assert.Equal(8, identity.ToString("s").Split()[1].Length);
        }
        #endregion

        #region Equality
        /// <summary>
        /// Formats identity with a dash between the prefix and suffix then parses it back into another identity
        /// to check for equality
        /// </summary>
        [Fact]
        public void When_NewIdentity_ItShould_EqualOther()
        {
            // Arrange
            Identity identity = NewIdentity(IdentityProfiles.SampleProfile);

            // Act
            var other = new Identity(identity.ToString("d"));

            // Assert
            Assert.Equal(IdentityProfiles.ForestryPrefix.Length + IdentityProfiles.SuffixLength, identity.ToString().Length);
            Assert.True(identity == other);
        }

        /// <summary>
        /// Different identities should not be equal
        /// </summary>
        [Fact]
        public void When_NewIdentity_ItShould_NotEqualOther()
        {
            // Arrange
            Identity identity = NewIdentity(IdentityProfiles.SampleProfile);

            // Act
            var other = new Identity("XXXX-12345678");

            // Assert
            Assert.True(identity != other);
        }
        #endregion
    }

    #region Clock
    /// <summary>
    /// Fake clock allowing to test VM clock drift and creation rate rollover
    /// </summary>
    internal sealed class FakeClock : IClock
    {
        public long Value;

        public long NowSeconds() => Value;

        public long NowMilliseconds() => Value;
    }
    #endregion

    #region Event listeners
    /// <summary>
    /// Event listener to bump the fake clock a tick (next time window)
    /// </summary>
    internal sealed class CreationRateOverflowListener : EventListener
    {
        public CreationRateOverflowListener(FakeClock clock)
        {
            _clock = clock;
        }

        private readonly FakeClock _clock;

        protected override void OnEventSourceCreated(EventSource eventSource) { 
            if (eventSource.Name == CreationRateOverflowEventName) { 
                EnableEvents(eventSource, EventLevel.Warning); // enable listening to creation rate overflow events with leve == warning
            } 
        }

        protected override void OnEventWritten(EventWrittenEventArgs arguments)
        {
            if (arguments.EventSource.Name == CreationRateOverflowEventName)
            {
                _clock.Value++;
            }
        }
    }

    /// <summary>
    /// Event listener to timestamp (lifetime) overflow
    /// </summary>
    internal sealed class LifetimeOverflowListener : EventListener {

        public readonly List<EventWrittenEventArgs> Events = new();

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == LifetimeOverflowEventName)
            {
                EnableEvents(eventSource, EventLevel.Warning); // enable listening to creation rate overflow events with leve == warning
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs arguments)
        {
            if (arguments.EventSource.Name == LifetimeOverflowEventName)
            {
                Events.Add(arguments);
            }
        }
    }
    #endregion

    #region Profiles
    internal static class IdentityProfiles
    {
        #region Prefix
        /// <summary>
        /// FOR == Forestry prefix
        /// </summary>
        public const string ForestryPrefix = "FOR";
        #endregion

        #region Suffix
        /// <summary>
        /// Suffix length
        /// </summary>
        public const byte SuffixLength = 8;
        #endregion

        #region Lifetimes
        /// <summary>
        /// 8.5 years in seconds (8.5 * 365 * 24 * 60 * 60)
        /// </summary>
        /// <remarks>2 to the power of 28 == 268435456 covers 8 years in seconds with 28 bits</remarks>
        public const int Lifetime_8_Years = 268056000;

        /// <summary>
        /// 17 years in seconds (17 * 365 * 24 * 60 * 60)
        /// </summary>
        /// <remarks>2 to the power of 29 == 536870912 covers 17 years in seconds with 29 bits</remarks>
        public const int Lifetime_17_Years = 536112466;

        /// <summary>
        /// 17 years in seconds (34 * 365 * 24 * 60 * 60)
        /// </summary>
        /// <remarks>2 to the power of 30 == 1073741824 covers 34 years in seconds with 30 bits</remarks>
        public const int Lifetime_34_Years = 1072224000;
        #endregion

        #region Creation Rate
        /// <summary>
        /// Creation rate == 64 before delaying to next second
        /// </summary>
        /// <remarks>2 to the power of 6 == 64 covering 64 per second</remarks>
        public const int CreationRate_64 = 64;

        /// <summary>
        /// Creation rate == 32 before delaying to next second 
        /// </summary>
        /// <remarks>2 to the power of 5 == 32 covering 32 per second</remarks>
        public const int CreationRate_32 = 32;

        /// <summary>
        /// Creation rate == 16 before delaying to next second
        /// </summary>
        /// <remarks>2 to the power of 4 == 16 covering 16 per second</remarks>
        public const int CreationRate_16 = 16;
        #endregion

        #region Nodes
        /// <summary>
        /// Nodes in a distributed system
        /// </summary>
        /// <remarks>2 to the power of 6 == 64 covers 64 nodes
        public const int Nodes = 64;  // Max 65 nodes when 0 is a valid node id
        #endregion

        #region Profiles
        /// <summary>
        /// Sample profile has a policy of a maximum 8 characters using the Crockford alphabet (5 bits per character):
        ///  - 17 year lifetime == 29 bits
        ///  - 32 creations per second == 5 bits
        ///  - 64 nodes == 6 bits
        /// 
        /// total == 40 bits / 5 bits per character == 8 characters
        /// </summary>
        public static readonly Profile SampleProfile = Profile.Create(ForestryPrefix, SuffixLength, Lifetime_17_Years, CreationRate_32, Nodes);
        #endregion
    }
    #endregion
}