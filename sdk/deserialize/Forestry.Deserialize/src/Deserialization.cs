using System.Diagnostics;

namespace Forestry.Deserialize
{
    /// <summary>
    /// 
    /// </summary>
    public partial static class Deserialization
    {
        /// <summary>
        /// Rather than multiple calls to typeof just cache 
        /// </summary>
        /// <returns></returns>
        internal static readonly Type _objectType = typeof(object);

        /// <summary>
        /// Get <see cref="TypeDefinition"/> favoring prevalent types
        /// </summary>
        /// <param name="type"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static TypeDefinition GetTypeDefinition(
            Type type, 
            DeserializeOptions options
        ) {
            Debug.Assert(type is not null);
            options.SetReadOnly();

            return type == _objectType
                ? options.ObjectTypeDefintion
                : options.GetTypeDefinition(type);
        }
    }
}