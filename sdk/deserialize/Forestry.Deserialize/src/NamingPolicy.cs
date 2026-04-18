namespace Forestry.Deserialize
{
    /// <summary>
    /// Naming policy
    /// </summary>
    public abstract class NamingPolicy
    {
        public abstract bool TryEnforce(string name);

        public abstract string Process(string name);

        public static NamingPolicy Default { get; } = new DefaultNamingPolicy();

        private sealed class DefaultNamingPolicy : NamingPolicy
        {
            public override bool TryEnforce(string name)
            {
                if (name is null || name == string.Empty)
                {
                    return false;
                }

                return true;
            }

            public override string Process(string name)
            {
                return name; 
            }
        }
    }
}