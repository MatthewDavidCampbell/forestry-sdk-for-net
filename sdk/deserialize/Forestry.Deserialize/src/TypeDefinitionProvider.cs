using System.Diagnostics;
using System.Reflection;
using Forestry.Deserialize.Attributes;

namespace Forestry.Deserialize
{
    /// <summary>
    /// Default <see cref="TypeDefinition"/> provider
    /// </summary>
    public partial class TypeDefinitionProvider : ITypeDefinitionProvider
    {
        private const BindingFlags _memberBindingFlags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.DeclaredOnly;
            
        public TypeDefinition? GetTypeDefinition(Type type, DeserializeOptions options)
        {
            Throwing.ArguementIsNull(type);
            Throwing.ArguementIsNull(options);

            type.CanDeserialize();
            Deserializer deserializer = GetDeserializer(type, options);
            TypeDefinition typeDefinition = GetTypeDefintion(type, deserializer, options);

            return typeDefinition;
        }

        internal static TypeDefinition GetTypeDefintion(
            Type type, 
            Deserializer deserializer, 
            DeserializeOptions options
        ) {
            TypeDefinition typeDefinition = TypeDefinition.GetTypeDefinition(type, deserializer, options);

            if (typeDefinition is { Kind: TypeDefinitionKind.Object })
            {
                if (type.GetSinglarAttribute<CollectionAttribute>(inherited: false) is { } elementCollectionAttribute)
                {
                    typeDefinition.ElementCollection = elementCollectionAttribute.Name; 
                }

                GetPropertyDefinitions(typeDefinition);
            }

            deserializer.AfterTypeDefinitionInitialization(typeDefinition, options);
            return typeDefinition;
        }

        /// <summary>
        /// Get property definitions
        /// </summary>
        /// <param name="typeDefinition"></param>
        private static void GetPropertyDefinitions(
            TypeDefinition typeDefinition
        ) {
            Debug.Assert(!typeDefinition.IsInitialized);
            Debug.Assert(typeDefinition.Kind is TypeDefinitionKind.Object);

            foreach (Type derivedType in typeDefinition.Type.GetSortedTypeHierarchy())
            {
                if (derivedType == TypeDefinition.SystemObjectType || derivedType == TypeDefinition.SystemValueType)
                {
                    break;
                }
                
                GetPropertyDefinitions(typeDefinition, derivedType);
            }
        }

        /// <summary>
        /// Get property definitions from members for each derivedType type
        /// </summary>
        /// <param name="typeDefinition"></param>
        /// <param name="derivedType"></param>
        private static void GetPropertyDefinitions(
            TypeDefinition typeDefinition,
            Type derivedType
        ) {
            Debug.Assert(!typeDefinition.IsInitialized);
            Debug.Assert(derivedType.IsAssignableFrom(typeDefinition.Type));

            foreach (PropertyInfo property in derivedType.GetProperties(_memberBindingFlags))
            {
                if (typeDefinition.Options.IgnorePropertyPolicy.TryEnforce(property))
                {
                    continue;
                }

                GetPropertyDefinition(typeDefinition, property.PropertyType, property);
            }

            foreach (FieldInfo field in derivedType.GetFields(_memberBindingFlags))
            {
                if (typeDefinition.Options.IncludeFieldPolicy.TryEnforce(field))
                {
                    GetPropertyDefinition(typeDefinition, field.FieldType, field);
                }
            }
        }

        /// <summary>
        /// Get property definition from memberInfo (field || property)
        /// </summary>
        /// <param name="typeDefinition"></param>
        /// <param name="memberType"></param>
        /// <param name="memberInfo"></param>
        private static void GetPropertyDefinition(
            TypeDefinition typeDefinition,
            Type memberType,
            MemberInfo memberInfo
        ) {
            PropertyDefinition propertyDefinition = typeDefinition.CreatePropertyDefinition(memberType, memberDeclaringType: memberInfo.DeclaringType);

            SetPropertyName(propertyDefinition, memberInfo);

            typeDefinition.Properties.Add(propertyDefinition);
        }

        private static void SetPropertyName(PropertyDefinition propertyDefinition, MemberInfo memberInfo)
        {
            string? name;

            if (propertyDefinition.Options.ElementNamingPolicy is not null)
            {
                name = propertyDefinition.Options.ElementNamingPolicy.Process(memberInfo.Name);
            } else
            {
                name = memberInfo.Name;
            }

            if (name is null)
            {
                throw new InvalidOperationException("");
            }

            propertyDefinition.Name = name;
        }

        /// <summary>
        /// Get deserializer by order starting with user defined then from services
        /// </summary>
        /// <param name="type"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        internal static Deserializer GetDeserializer(Type type, DeserializeOptions options)
        {
            Deserializer? deserializer = options.GetUserDefinedDeserializer(type);

            // TODO: Deserializer from attributes

            deserializer ??= GetServiceDeserializer(type, options);


            // TODO: Assert derivedType type from interfaces etc.
            return deserializer;
        }

        /// <summary>
        /// Get service defined deserializers by order starting with simples targeting values then 
        /// trying factories either targeting enumerable or objects
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static Deserializer GetServiceDeserializer(Type type, DeserializeOptions options)
        {
            _simpleDeserializers ??= options.DeserializerProvider.GetSimpleDeserializers();
            _deserializerFactories ??= options.DeserializerProvider.GetFactoryDeserializers();

            if (_simpleDeserializers.TryGetValue(type, out Deserializer? deserializer))
            {
                return deserializer;
            }

            foreach (DeserializerFactory factory in _deserializerFactories)
            {
                if (factory.CanDeserialize(type))
                {
                    deserializer = factory.CreateDeserializer(type, options);
                    break;
                }
            }

            Debug.Assert(deserializer is not null);
            return deserializer;
        }

        private static Dictionary<Type, Deserializer>? _simpleDeserializers;

        private static DeserializerFactory[]? _deserializerFactories;
    }
}