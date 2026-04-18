namespace Forestry.Deserialize
{
    internal static partial class TypeExtensions
    {
        /// <summary>
        /// Get type hierarchy from derived types to the base type 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Type[] GetSortedTypeHierarchy(this Type type)
        {
            if (!type.IsInterface)
            {
                var results = new List<Type>();

                for (Type? current = type; current != null; current = current.BaseType)
                {
                    results.Add(current);
                }

                return [.. results];
            }

            // TODO: Interfaces?
            return [];
        }

        /// <summary>
        /// Types like void, pointers, och references 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool CanDeserialize(this Type type) {
            return type == typeof(void) || type.IsPointer || type.IsByRef;
        }
    }
}