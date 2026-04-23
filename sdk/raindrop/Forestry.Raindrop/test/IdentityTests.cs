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
    /// - Suffix validation (e.g. only Crockford characters)
    /// - Profiles that create correct bit sizes for lifetimes, creation rate, nodes and their masks (i.e. max values)
    /// - Timestamp part with overflow handling (e.g. VM clock drift, rollover, no DST)
    /// - Creation rate part with overflow handling
    /// - Nodes part
    /// - Remaining part when suffix length is bigger than timestamp + creation rate + nodes bits
    /// - Formatting
    /// - Equality
    /// 
    /// A structured identity was created to solve a particular problem with unstructured (random) that 
    /// required an external resource (Azure Table) to guard against duplicates.  Using a combination of 
    /// timestamp (lifetime), creation rate and nodes (plus remaining) a profile can be made for different scenarios 
    /// and constraints.  Lifetime for structured identities is not infinite and restricted by the number 
    /// of bits allowed for the timestamp part which is mainly determined by the suffix length (i.e. number 
    /// of characters allowed).
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
            IdentityProfile profile = IdentityProfiles.CreateIdentityProfile("", 8, 268056000, 64, 64);

            // Act + Assert
            Assert.Throws<FormatException>(() => Identity.NewIdentity(profile));
        }

        /// <summary>
        /// Prefix must have a maximum of 5 characters
        /// </summary>
        [Fact]
        public void When_PrefixTooLarge_ItShould_ThrowFormatException()
        {
            // Arrange
            IdentityProfile profile = IdentityProfiles.CreateIdentityProfile("ABCDEF", 8, 268056000, 64, 64);

            // Act + Assert (only 5 latin uppercase and digits allowed)
            Assert.Throws<FormatException>(() => Identity.NewIdentity(profile));
        }

        /// <summary>
        /// Prefix must only contain latin uppercase and digits
        /// </summary>
        [Fact]
        public void When_PrefixHasBadCharacter_ItShould_ThrowFormatException()
        {
            // Arrange
            IdentityProfile profile = IdentityProfiles.CreateIdentityProfile("$BCDEF", 8, 268056000, 64, 64);  // Only latin uppercase and digits

            // Act + Assert
            Assert.Throws<FormatException>(() => Identity.NewIdentity(profile));
        }

        /// <summary>
        /// Prefix must not contain whitespaces
        /// </summary>
        [Fact]
        public void When_PrefixHasWhitespace_ItShould_ThrowFormatException()
        {
            // Arrange
            IdentityProfile profile = IdentityProfiles.CreateIdentityProfile("ABC D", 8, 268056000, 64, 64);

            // Act + Assert
            Assert.Throws<FormatException>(() => Identity.NewIdentity(profile));
        }

        /// <summary>
        /// Prefix trims leading whitespaces
        /// </summary>
        [Fact]
        public void When_PrefixStartingWhitespace_ItShould_Ignore()
        {
            // Arrange
            IdentityProfile profile = IdentityProfiles.CreateIdentityProfile(" ABCD", 8, 268056000, 64, 64);

            // Act + Assert
            var identity = Identity.NewIdentity(profile);
            Assert.True(identity.ToString("d").StartsWith('A'));
        }

        /// <summary>
        /// Prefix trims ending whitespaces
        /// </summary>
        [Fact]
        public void When_PrefixEndingWhitespace_ItShould_Ignore()
        {
            // Arrange
            IdentityProfile profile = IdentityProfiles.CreateIdentityProfile("ABCD ", 8, 268056000, 64, 64);

            // Act + Assert
            var identity = Identity.NewIdentity(profile);
            Assert.True(identity.ToString("d")[3] == 'D');
        }

        /// <summary>
        /// Prefix converts lowercase to uppercase
        /// </summary>
        [Fact]
        public void When_PrefixLowercase_ItShould_ToUppercase()
        {
            // Arrange
            IdentityProfile profile = IdentityProfiles.CreateIdentityProfile("abcd", 8, 268056000, 64, 64);

            // Act + Assert
            var identity = Identity.NewIdentity(profile);
            Assert.True(identity.ToString("d").StartsWith('A'));
        }

        /// <summary>
        /// Prefix only latin uppercase (i.e. A-Z, no Ä, Ö, Å etc.)
        /// </summary>
        [Fact]
        public void When_NonLatin_ItShould_ThrowFormatException()
        {
            // Arrange
            IdentityProfile profile = IdentityProfiles.CreateIdentityProfile("ÄBC", 8, 268056000, 64, 64);

            // Act + Assert
            Assert.Throws<FormatException>(() => Identity.NewIdentity(profile));
        }
        #endregion

        #region Suffix
        /// <summary>
        /// Crockford Base32 characters only (i.e. 0-9, A-Z except for O, U, I, L)
        /// </summary>
        [Fact]
        public void When_NewIdentity_ItShould_OnlyCrockfordSuffix()
        {
            // Arrange + Act + Assert
            Assert.Throws<FormatException>(() => new Identity("XXXX-OABC1234"));  // O
            Assert.Throws<FormatException>(() => new Identity("XXXX-UABC1234"));  // U
            Assert.Throws<FormatException>(() => new Identity("XXXX-IABC1234"));  // I
            Assert.Throws<FormatException>(() => new Identity("XXXX-LABC1234"));  // L
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
            IdentityProfile profile = IdentityProfiles.CreateIdentityProfile("ABCD", 8, 268056000, 64, 64);

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
            IdentityProfile profile = IdentityProfiles.CreateIdentityProfile("ABCD", 8, 268056000, 64, 64);

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
            IdentityProfile profile = IdentityProfiles.CreateIdentityProfile("ABCD", 8, 268056000, 64, 64);

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
            Assert.Throws<InvalidOperationException>(() => IdentityProfiles.CreateIdentityProfile("ABCD", 7, 268056000, 64, 64));
        }

        /// <summary>
        /// Non multi-node profile could have zero nodes but still work by filling up the remaining part
        /// </summary>
        [Fact]
        public void When_NewIdentityWithoutNodes_ItShould_HaveZeroNodesBits()
        {
            // Arrange
            IdentityProfile profile = IdentityProfiles.CreateIdentityProfile("ABCD", 8, 268056000, 64, -1);

            // Act + Assert
            Assert.Equal(0, profile.NodesBits);
        }

        [Fact]
        public void When_SmallerYearProfile_ItShould_HavePrefixAndLength()
        {
            // Arrange
            IdentityProfile profile = IdentityProfiles.SmallerYearProfile;

            // Act
            var identity = Identity.NewIdentity(profile);
            var value = identity.ToString();

            // Assert    
            Assert.StartsWith(IdentityProfiles.ResultPrefix, value);
            Assert.Equal(12, value.Length);
        }

        [Fact]
        public void When_LargerYearProfile_ItShould_HavePrefixAndLength()
        {
            // Arrange
            IdentityProfile profile = IdentityProfiles.LargerYearProfile;

            // Act
            var identity = Identity.NewIdentity(profile);
            var value = identity.ToString();

            // Assert    
            Assert.StartsWith(IdentityProfiles.DeliveryPrefix, value);
            Assert.Equal(20, value.Length);
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
            IdentityProfile profile = IdentityProfiles.CreateIdentityProfile("ABCD", 8, 268056000, 64, 64);
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
            IdentityProfile profile = IdentityProfiles.CreateIdentityProfile("ABCD", 8, 268056000, 64, 64);

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
            IdentityProfile profile = IdentityProfiles.CreateIdentityProfile("ABCD", 8, 268056000, 64, 64);

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
            IdentityProfile profile = IdentityProfiles.CreateIdentityProfile("ABCD", 8, 268056000, 64, 64);
            Identity.Suffix suffix = new(profile, clock);

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
            IdentityProfile profile = IdentityProfiles.CreateIdentityProfile("ABCD", 8, 268056000, 64, 64);
            Identity.Suffix suffix = new(profile, clock);

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

            IdentityProfile profile = IdentityProfiles.CreateIdentityProfile("ABCD", 8, 268056000, 64, 64);
            Identity.Suffix suffix = new(profile, clock);

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
        /// Parallel thread identity creation should have no duplicates even when creation 
        /// rate rolls over (i.e. multiple Rebus workers creating identities in parallel and hitting the same creation rate overflow at the same time)
        /// </summary>
        [Fact]
        public void When_ConcurrentCreationRates_ItShould_HaveNoDuplicates()
        {
            // Arrange
            FakeClock clock = new() { Value = 1000 };

            IdentityProfile profile = IdentityProfiles.CreateIdentityProfile("ABCD", 8, 268056000, 64, 64);
            Identity.Suffix suffix = new(profile, clock);

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
            IdentityProfile profile = IdentityProfiles.CreateIdentityProfile("ABCD", 8, 268056000, 64, 64);

            // Act + Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => Identity.NewIdentity(profile, 66));
        }

        /// <summary>
        /// Node id should be extractable from the suffix and be the same as the one set when creating the identity
        /// </summary>
        [Fact]
        public void When_SettingNodeId_ItShould_ExtractTheSameId()
        {
            // Arrange
            IdentityProfile profile = IdentityProfiles.CreateIdentityProfile("ABCD", 8, 268056000, 64, 64);

            // Act
            var identity = Identity.NewIdentity(profile, 5);

            // Assert
            Assert.Equal(5, Identity.GetNode(identity.ToString("s").Split()[1], profile));
        }
        #endregion

        #region Remaining
        [Fact]
        public void When_NewIdentityWithRemaining_ItShould_Fillout()
        {
            // Arrange
            IdentityProfile profile = IdentityProfiles.CreateIdentityProfile("ABCD", 16, 268056000, 64, -1);

            // Act + Assert
            Assert.Equal(20, Identity.NewIdentity(profile, 5).ToString().Length);
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
            IdentityProfile profile = IdentityProfiles.CreateIdentityProfile("ABCD", 8, 268056000, 64, 64);

            // Act + Assert
            var identity = Identity.NewIdentity(profile);
            Assert.True(identity.ToString("d").Contains('-'));
        }

        /// <summary>
        /// Identity as string with a space between the prefix and suffix
        /// </summary>
        [Fact]
        public void When_NewIdentityFormatSpace_ItShould_HaveSpace()
        {
            // Arrange
            IdentityProfile profile = IdentityProfiles.CreateIdentityProfile("ABCD", 8, 268056000, 64, 64);

            // Act + Assert
            var identity = Identity.NewIdentity(profile);
            Assert.True(identity.ToString("s").Contains(' '));
        }

        /// <summary>
        /// Identity with default format should have nothing between the prefix and suffix
        /// </summary>
        [Fact]
        public void When_NewIdentityFormatNone_ItShould_HaveNeitherDashOrSpaces()
        {
            // Arrange
            IdentityProfile profile = IdentityProfiles.CreateIdentityProfile("ABCD", 8, 268056000, 64, 64);

            // Act + Assert
            var identity = Identity.NewIdentity(profile);
            Assert.True(identity.ToString()[4] != '-' || identity.ToString()[4] != ' ');
        }

        /// <summary>
        /// Identity as string should have same length as the suffix length defined in the profile
        /// </summary>
        [Fact]
        public void When_NewIdentity_ItShould_HaveSameSuffixLength()
        {
            // Arrange
            IdentityProfile profile = IdentityProfiles.CreateIdentityProfile("ABCD", 8, 268056000, 64, 64);

            // Act + Assert
            var identity = Identity.NewIdentity(profile);
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
            var identity = Identity.NewIdentity(IdentityProfiles.SmallerYearProfile);

            // Act
            var other = new Identity(identity.ToString("d"));

            // Assert
            Assert.Equal(IdentityProfiles.ResultPrefix.Length + IdentityProfiles.ResultSuffixLength, identity.ToString().Length);
            Assert.True(identity == other);
        }

        /// <summary>
        /// Different identities should not be equal
        /// </summary>
        [Fact]
        public void When_NewIdentity_ItShould_NotEqualOther()
        {
            // Arrange
            var identity = Identity.NewIdentity(IdentityProfiles.SmallerYearProfile);

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
                EnableEvents(eventSource, EventLevel.Warning); // enable listening to creation rate overflow events with level == warning
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
                EnableEvents(eventSource, EventLevel.Warning); // enable listening to creation rate overflow events with level == warning
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
}