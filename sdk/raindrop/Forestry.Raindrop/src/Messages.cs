namespace Forestry.Raindrop
{
    internal readonly struct Messages
    {
        internal readonly struct Identity
        {
            public const string IsEmpty = "Identity may not be empty i.e. null || blank";

            public const string HasWhitespace = "Identity may not contains whitespaces";

            public const string WrongPrefixLength = $"Identity prefix length must be between 1 and 5 characters";

            public const string InvalidPrefixCharacter = "Identity prefix has invalid character";

            public const string WrongSuffixLength = "Identity suffix length must be between 1 and 21 characters";

            public const string InvalidSuffixCharacter = "Identity suffix has invalid character";

            public const string Unrecognized = "Identity is not recognized";
        }
    }
}