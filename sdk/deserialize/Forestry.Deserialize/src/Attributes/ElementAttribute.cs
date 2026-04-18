namespace Forestry.Deserialize.Attributes
{
    /// <summary>
    /// Element attribute on simple types or collections of simples types 
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class ElementAttribute: Attribute
    {
        public ElementAttribute(string name, string elementCollection)
        {
            Name = name;
            ElementCollection = elementCollection;
        }

        public string Name { get; }  
        
        public string ElementCollection { get; }
    }
}