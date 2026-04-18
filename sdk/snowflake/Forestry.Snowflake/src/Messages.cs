namespace Forestry.Snowflake
{
    internal readonly struct Messages
    {
        internal readonly struct Identity
        {
            public const string IdentityEmpty = "Identity may not be empty i.e. null || blank";

            public const string IdentityWhitespace = "Identity may not contains whitespaces";

            public const string IdentityWrongPrefixLength = $"Identity prefix length must be between 1 and 5 characters";

            public const string IdentityInvalidPrefixCharacter = "Identity prefix has invalid character";

            public const string IdentityWrongSuffixLength = "Identity suffix length must be between 1 and 21 characters";

            public const string IdentityInvalidSuffixCharacter = "Identity suffix has invalid character";

            public const string IdentityUnrecognized = "Identity is not recognized";
        }
    }
}