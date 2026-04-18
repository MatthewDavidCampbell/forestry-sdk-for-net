using System.Reflection;

namespace Forestry.Deserialize
{
    public interface IIgnorePropertyPolicy
    {
        public bool TryEnforce(PropertyInfo property);
    }
}