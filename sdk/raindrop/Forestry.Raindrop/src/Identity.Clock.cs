namespace Forestry.Raindrop
{
    /// <summary>
    /// Clock interface accessing the current time in seconds or milliseconds
    /// </summary>
    public readonly partial struct Identity
    {
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
}