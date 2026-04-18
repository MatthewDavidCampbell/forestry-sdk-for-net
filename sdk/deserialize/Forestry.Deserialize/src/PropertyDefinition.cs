using System.Diagnostics;

namespace Forestry.Deserialize
{
    public abstract class PropertyDefinition(
        Type type,
        Type declaringType,
        TypeDefinition? declaringTypeDefinition,
        DeserializeOptions options
    )
    {

        #region Shape
        /// <summary>
        /// Property <see cref="Type"/>
        /// </summary>
        /// <value></value>
        public Type Type { get; } = type;

        /// <summary>
        /// Declaring <see cref="Type"/>
        /// </summary>
        /// <value></value>
        public Type DeclaringType { get; } = declaringType;

        /// <summary>
        /// Declaring <see cref="TypeDefinition"/>
        /// </summary>
        /// <value></value>
        public TypeDefinition? DeclaringTypeDefinition { get; private set; } = declaringTypeDefinition;

        /// <summary>
        /// Options
        /// </summary>
        /// <value></value>
        public virtual DeserializeOptions Options { get; } = options;

        /// <summary>
        /// Name when deserializing
        /// </summary>
        /// <value></value>
        public string Name
        {
            get
            {
                Debug.Assert(_name is not null);
                return _name;
            }
            set
            {
                ThrowingWhenIsInitialized();
                ArgumentNullException.ThrowIfNull(value);

                _name = value;
            }
        }

        private string? _name;

        /// <summary>
        /// Property <see cref="TypeDefinition"/>
        /// </summary>
        /// <value></value>
        internal TypeDefinition TypeDefinition
        {
            get
            {
                Debug.Assert(_typeDefinition?.IsConfiguring == true);

                TypeDefinition value = _typeDefinition;
                value.SetConfiguration();

                return value;
            }
            set
            {
                _typeDefinition = value;
            }
        }

        private TypeDefinition? _typeDefinition;
        #endregion

        #region Configuration
        /// <summary>
        /// Asserts is configured
        /// </summary>
        /// <value></value>
        internal bool IsConfigured { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        internal void Configure()
        {
            Debug.Assert(DeclaringTypeDefinition is not null);
            Debug.Assert(!IsConfigured);

            // TODO: Maybe ignore logic here?

            _typeDefinition ??= Options.GetTypeDefinition(Type);
            _typeDefinition.SetConfiguration();

            if (TypeDefinition.Deserializer.CanReadValues)
            {
                // TODO:
            }
            else if (_typeDefinition.Kind == TypeDefinitionKind.Object && _typeDefinition.Properties.Count == 0)
            {
                DeclaringTypeDefinition.Properties.Remove(this);
            }
            else if (_typeDefinition.ElementTypeDefinition?.Kind == TypeDefinitionKind.Object && _typeDefinition.ElementTypeDefinition.Properties.Count == 0)
            {
                DeclaringTypeDefinition.Properties.Remove(this);
            }

            IsConfigured = true;
        }

        /// <summary>
        /// Throw when declaring type definition is initialized
        /// </summary>
        private protected void ThrowingWhenIsInitialized()
        {
            DeclaringTypeDefinition?.ThrowingWhenIsInitialized();
        }

        /// <summary>
        /// Set declaring type definition only once else throw
        /// </summary>
        /// <param name="parent"></param>
        internal void SetDeclaringTypeDefinition(TypeDefinition parent)
        {
            if (DeclaringTypeDefinition is null)
            {
                DeclaringTypeDefinition = parent;
            }
            else if (DeclaringTypeDefinition != parent)
            {
                Throwing.WhenWrongDeclaringTypeDefintion(this);
            }
        }
        #endregion
    }
}