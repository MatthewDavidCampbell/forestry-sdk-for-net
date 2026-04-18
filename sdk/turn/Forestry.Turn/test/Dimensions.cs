#nullable disable

namespace Forestry.Turn.Tests
{
    /// <summary>
    /// Answer || question dimensions
    /// </summary>
    internal class Dimensions
    {
        private readonly Dictionary<string, object> _dimensions = new(StringComparer.OrdinalIgnoreCase);

        public Dimensions() { }

        /// <summary>
        /// Add value as collection
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Add(string name, string value) {
            if (_dimensions.TryGetValue(name, out object collection))
            {
                if (collection is List<string> values)
                {
                    values.Add(value);
                }
                else
                {
                    _dimensions[name] = new List<string> { collection as string, value };
                }
            }
            else
            {
                _dimensions[name] = value;
            }
        }

        /// <summary>
        /// Try get value
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGet(string name, out string value)
        {
            if (_dimensions.TryGetValue(name, out object collection))
            {
                if (collection is List<string> values)
                {
                    value = string.Join(Dimension.DefaultDelimeter, values);
                }
                else
                {
                    value = collection as string;
                }

                return true;
            }

            value = null;
            return false;
        }

        /// <summary>
        /// Try get values
        /// </summary>
        /// <param name="name"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public bool TryGet(string name, out IEnumerable<string> values)
        {
            if (_dimensions.TryGetValue(name, out object value))
            {
                if (value is List<string> collection)
                {
                    values = collection;
                } else
                {
                    values = [value as string];
                }
            }

            values = null;
            return false;
        }

        /// <summary>
        /// Contains
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Contains(string name) => _dimensions.ContainsKey(name);

        /// <summary>
        /// Set
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Set(string name, string value) => _dimensions[name] = value;

        /// <summary>
        /// Remove
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Remove(string name) => _dimensions.Remove(name);

        /// <summary>
        /// Enumerate
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Dimension> Enumerate() => _dimensions.Select(
            dimension => new Dimension(
                dimension.Key, 
                (dimension.Value is List<string> values) ? 
                    string.Join(Dimension.DefaultDelimeter, values) : 
                    dimension.Value as string
            )
        );
    }
}
