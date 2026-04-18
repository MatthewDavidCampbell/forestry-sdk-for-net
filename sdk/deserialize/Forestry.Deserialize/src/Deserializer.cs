namespace Forestry.Deserialize
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class Deserializer
    {        
        protected Deserializer()
        {
            DeserializerKind = GetDeserializerKind();
        }

        #region Shape
        /// <summary>
        /// Type expected when deserializing
        /// </summary>
        /// <value></value>
        public abstract Type? Type { get; }

        /// <summary>
        /// Enumerable deserializer
        /// </summary>
        internal virtual Type? ElementType => null;

        /// <summary>
        /// Dictionary deserializer
        /// </summary>
        internal virtual Type? KeyType => null;

        /// <summary>
        /// Reader constraints
        /// </summary>
        /// <value></value>
        public DeserializerKind DeserializerKind
        {
            get => _deserializerKind;
            init
            {
                _deserializerKind = value;
            }
        }

        private DeserializerKind _deserializerKind;

        /// <summary>
        /// Get reader constraints
        /// </summary>
        /// <returns></returns>
        protected internal abstract DeserializerKind GetDeserializerKind();
        #endregion

        #region Reading
    
        /// <summary>
        /// Deserializer type assertion
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public abstract bool CanDeserialize(Type type);

        /// <summary>
        /// Can this deserializer read <see cref="Value"/> || <see cref="IEnumerable{Value}"/>
        /// </summary>
        /// <value></value>
        public virtual bool CanReadValues { get => false; }
        #endregion

        #region Configuration
        /// <summary>
        /// Initialize <see cref="TypeDefinition"/> with reflection
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public abstract TypeDefinition InitializeTypeDefinition(DeserializeOptions options);

        /// <summary>
        /// Deserializer overrides after <see cref="TypeDefinition"/> initialization
        /// </summary>
        internal virtual void AfterTypeDefinitionInitialization(TypeDefinition typeDefinition, DeserializeOptions options) { }
        #endregion Configuration
    }
}