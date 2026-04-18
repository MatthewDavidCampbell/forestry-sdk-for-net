using System.Diagnostics;

namespace Forestry.Deserialize
{
    public abstract partial class TypeDefinition
    {
        private ObservablePropertyDefinitionList? _properties;

        /// <summary>
        /// Property definitions apply to only object shapes otherwise empty
        /// </summary>
        internal ObservablePropertyDefinitionList Properties
        {
            get
            {
                return _properties ?? CreateProperties();

                ObservablePropertyDefinitionList CreateProperties()
                {
                    ObservablePropertyDefinitionList values = new(this);

                    ObservablePropertyDefinitionList? others = Interlocked.CompareExchange(ref _properties, values, null);
                    return others ?? values;
                }
            }
        }

        /// <summary>
        /// Configure <see cref="Deserialize.PropertyDefinition"/> values when <see cref="TypeDefinitionKind.Object"/>
        /// </summary>
        private void ConfigureProperties()
        {
            Debug.Assert(Kind == TypeDefinitionKind.Object);

            ObservablePropertyDefinitionList properties = Properties;

            for (int index = 0; index < properties.Count; index++)
            {
                PropertyDefinition propertyDefinition = properties[index];
                Debug.Assert(propertyDefinition.DeclaringTypeDefinition == this);

                propertyDefinition.Configure();
                // TODO: has || is element then keep otherwise remove
            }
        }

        /// <summary>
        /// Initialize property definition using a type definition from the options cache 
        /// else from a reflective instantiator
        /// </summary>
        /// <param name="memberType"></param>
        /// <param name="memberDeclaringType"></param>
        /// <returns></returns>
        internal PropertyDefinition CreatePropertyDefinition(
            Type memberType,
            Type? memberDeclaringType
        ) {
            PropertyDefinition propertyDefinition;

            if (Options.TryGetTypeDefinition(memberType, out TypeDefinition? typeDefinition))
            {
                propertyDefinition = typeDefinition.CreatePropertyDefinition(declaringTypeDefinition: this, memberDeclaringType, Options);
            } else
            {
                propertyDefinition = Options.PropertyDefinitionReflectiveInstantiator(memberDeclaringType ?? Type, this, Options);
            }

            return propertyDefinition;
        }

        /// <summary>
        /// Create property definition from declaring derived || base type
        /// </summary>
        /// <param name="declaringTypeDefinition"></param>
        /// <param name="declaringType"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private protected abstract PropertyDefinition CreatePropertyDefinition(
            TypeDefinition declaringTypeDefinition,
            Type? declaringType,
            DeserializeOptions options
        );

        /// <summary>
        /// Observable property definition list asserting that the type definition is 
        /// not set initialized and has an object shape
        /// </summary>
        internal sealed class ObservablePropertyDefinitionList : ObservableList<PropertyDefinition>
        {
            public ObservablePropertyDefinitionList(TypeDefinition target)
            {
                _typeDefinition = target;
            }

            private readonly TypeDefinition _typeDefinition;

            public override bool IsReadOnly => _typeDefinition._properties == this && _typeDefinition.IsInitialized || _typeDefinition.Kind != TypeDefinitionKind.Object;

            /// <summary>
            /// Before an list operation assert the declaring type definition has not been 
            /// set initialized or unexpected shape (kind) 
            /// </summary>
            protected override void Before()
            {
                if (_typeDefinition._properties == this)
                {
                    _typeDefinition.ThrowingWhenIsInitialized();
                }

                if (_typeDefinition.Kind != TypeDefinitionKind.Object)
                {
                    Throwing.WhenConfigurePropertiesWrongDeclaringTypeDefintion(_typeDefinition.Kind);
                }
            }

            /// <summary>
            /// Before an item operation
            /// </summary>
            /// <param name="item"></param>
            protected override void Before(PropertyDefinition item) => item.SetDeclaringTypeDefinition(_typeDefinition);
        }
    }
}