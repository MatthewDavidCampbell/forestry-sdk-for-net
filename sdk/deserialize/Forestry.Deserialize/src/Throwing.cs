using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Forestry.Deserialize
{
    internal static partial class Throwing
    {

        public static void ArguementIsNull([NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string? name = null)
        {
            if (argument is null)
            {
                throw new ArgumentNullException(name);
            }
        }

        [DoesNotReturn]
        public static void WhenTypeDefinitionIsInitialized()
        {
            throw new InvalidOperationException(Formatting.TypeDefinitionIsInitialized);
        }

        [DoesNotReturn]
        public static void WhenDeserializeOptionsIsReadOnly()
        {
            throw new InvalidOperationException(Formatting.DeserializeOptionsIsReadOnly);
        }

        [DoesNotReturn]
        public static void WhenWrongDeclaringTypeDefintion(PropertyDefinition propertyDefinition)
        {
            Debug.Assert(propertyDefinition.DeclaringTypeDefinition is not null, "Exception only expected when declaring type definition is null");
            throw new InvalidOperationException(Formatting.Format(Formatting.WrongDeclaringTypeDefintion, propertyDefinition.DeclaringTypeDefinition.Type.FullName, propertyDefinition.Name));
        }

        [DoesNotReturn]
        public static void WhenConfigurePropertiesWrongDeclaringTypeDefintion(TypeDefinitionKind kind)
        {
            throw new InvalidOperationException(Formatting.Format(Formatting.@ConfigurePropertiesWrongDeclaringTypeDefintion, kind));
        }

        [DoesNotReturn]
        public static void WhenNotSingularAttribute(Type attributeType, MemberInfo member)
        {
            string location = member is Type type ? type.ToString() : $"{member.DeclaringType}.{member.Name}";
            throw new InvalidOperationException(Formatting.Format(Formatting.WhenNotSingularAttribute, attributeType, location));
        }
    }
}