using System.Reflection;

namespace Forestry.Deserialize
{
    public interface IIncludeFieldPolicy
    {
        public bool TryEnforce(FieldInfo field);
    }
}