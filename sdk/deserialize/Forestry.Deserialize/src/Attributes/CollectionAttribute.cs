namespace Forestry.Deserialize.Attributes
{
    /// <summary>
    /// Element collection attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class CollectionAttribute: Attribute
    {

        public CollectionAttribute(
            string name
        )
        {
            Name = name;
        }

        /// <summary>
        /// Element name distinct within an element collection
        /// </summary>
        public string Name { get; }
    }
}