using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Forestry.Deserialize
{
    internal static partial class ReflextionExtensions
    {
        /// <summary>
        /// Get singlar attribute
        /// </summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <param name="memberInfo"></param>
        /// <param name="inherited"></param>
        /// <returns></returns>
        public static TAttribute? GetSinglarAttribute<TAttribute>(
            this MemberInfo memberInfo,
            bool inherited
        ) where TAttribute : Attribute {
            object[] attributes = memberInfo.GetCustomAttributes(typeof(TAttribute), inherited);

            if (attributes.Length == 0)
            {
                return null;
            }

            if (attributes.Length == 1)
            {
                return (TAttribute)attributes[0];
            }

            Throwing.WhenNotSingularAttribute(typeof(TAttribute), memberInfo);
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="parameterTypes"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        /// <see cref="https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/7.0/reflection-invoke-exceptions"/>
        public static object? ThrowingInstantiate(
            this Type type,
            Type[] parameterTypes,
            object?[] parameters
        ) {
            ConstructorInfo constructorInfo = type.GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, 
                null, 
                parameterTypes, 
                null
            )!;

            return constructorInfo.Invoke(
                BindingFlags.DoNotWrapExceptions, 
                null, 
                parameters, 
                null
            );
        }

        /// <summary>
        /// Try get generic base type e.g. generic List<T> from base type List<>
        /// </summary>
        /// <param name="type"></param>
        /// <param name="baseType"></param>
        /// <returns></returns>
        public static bool TryGetGenericBaseType(
            this Type type, 
            Type? baseType,
            [NotNullWhen(true)] out Type? genericBaseType
        ) {
            genericBaseType = null;

            if (baseType is null)
            {
                return false;
            }

            Debug.Assert(baseType.IsGenericType);
            Debug.Assert(!baseType.IsInterface);
            Debug.Assert(baseType == baseType.GetGenericTypeDefinition());

            genericBaseType = type;

            while (genericBaseType != null && genericBaseType != typeof(object))
            {
                if (genericBaseType.IsGenericType)
                {
                    Type genericTypeToCheck = genericBaseType.GetGenericTypeDefinition();
                    if (genericTypeToCheck == baseType)
                    {
                        return true;
                    }
                }

                genericBaseType = genericBaseType.BaseType;
            }

            return false;
        }

        /// <summary>
        /// Try get object constructor
        /// </summary>
        /// <param name="type"></param>
        /// <param name="constructorInfo"></param>
        /// <returns></returns>
        public static bool TryGetObjectConstructor(
            this Type type,
            out ConstructorInfo? constructorInfo
        ) {
            constructorInfo = null;
            ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

            if (constructors.Length == 1)
            {
                constructorInfo = constructors[0];
                return true;
            }

            foreach (ConstructorInfo constructor in constructors)
            {
                if (constructor.GetParameters().Length == 0)
                {
                    constructorInfo = constructor;
                    return true;
                }
            }

            return false;
        }
    }
}