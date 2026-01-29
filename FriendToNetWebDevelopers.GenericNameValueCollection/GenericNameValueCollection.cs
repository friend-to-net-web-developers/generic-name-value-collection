using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;

namespace FriendToNetWebDevelopers.GenericNameValueCollection
{
    /// <summary>
    /// Represents a generic collection of key-value pairs where each key is associated with a collection of values.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the elements in the collections associated with each key.
    /// </typeparam>
    /// <remarks>
    /// The <see cref="GenericNameValueCollection{T}"/> class provides functionality for storing and managing key-value
    /// pairs where keys are strings and values are collections of type <typeparamref name="T"/>.
    /// The class supports various operations such as adding, removing, retrieving, and enumerating over its elements.
    /// </remarks>
    public class GenericNameValueCollection<T> :
        ICollection<KeyValuePair<string?, ICollection<T>>>,
        IEnumerable<KeyValuePair<string?, ICollection<T>>>,
        IDictionary<string?, ICollection<T>>,
        IReadOnlyCollection<KeyValuePair<string?, ICollection<T>>>,
        IReadOnlyDictionary<string?, ICollection<T>>,
        IDeserializationCallback,
        ISerializable
    {
        public bool IsReadOnly { get; }
        private readonly List<string?> _keys = new List<string?>();
        private readonly List<ICollection<T>> _values = new List<ICollection<T>>();
        private readonly StringComparer _comparer = StringComparer.OrdinalIgnoreCase;

        /// <summary>
        /// Represents a collection of key-value pairs where each key is associated with a collection of values
        /// of a specified type. Provides functionality for managing such collections, supporting optional case-insensitive
        /// key comparison, and enabling access through various collection interfaces.
        /// </summary>
        /// <typeparam name="T">The type of elements stored within the collection associated with each key.</typeparam>
        public GenericNameValueCollection()
        {
            IsReadOnly = false;
        }

        /// <summary>
        /// Represents a generic collection of key-value pairs, where each key is associated
        /// with a collection of values of a specified type. Provides support for case-insensitive
        /// key comparison and various collection interfaces for managing and accessing elements.
        /// </summary>
        /// <typeparam name="T">The type of elements stored within the collection associated with each key.</typeparam>
        public GenericNameValueCollection(int capacity, StringComparer? comparer = null)
        {
            IsReadOnly = false;
            if(comparer != null) _comparer = comparer;
            _keys = new List<string?>(capacity);
            _values = new List<ICollection<T>>(capacity);
        }

        /// <summary>
        /// Represents a collection of key-value pairs where each key is associated with a collection of values of a specified type.
        /// Allows case-insensitive key comparisons by default or uses a user-provided <c>StringComparer</c>.
        /// Supports various collection interfaces for reading, adding, and managing key-value pairs.
        /// </summary>
        /// <typeparam name="T">The type of elements stored in the collection of values associated with each key.</typeparam>
        public GenericNameValueCollection(StringComparer comparer)
        {
            IsReadOnly = false;
            _comparer = comparer;
        }

        /// <summary>
        /// Represents a collection of key-value pairs, where each key is associated with a collection of values of a specified type.
        /// This collection supports case-insensitive key comparisons if a string comparer is specified or defaults to <c>StringComparer.OrdinalIgnoreCase</c>.
        /// </summary>
        /// <typeparam name="T">The type of elements stored in the collection of values associated with each key.</typeparam>
        public GenericNameValueCollection(ICollection<KeyValuePair<string?, ICollection<T>>> collection,
            StringComparer? comparer = null, bool isReadOnly = false)
        {
            IsReadOnly = false;
            if(comparer != null) _comparer = comparer;
            foreach (var item in collection)
            {
                Set(item);
            }
            IsReadOnly = isReadOnly;
        }

        /// <summary>
        /// Represents a collection of key-value pairs where each key is associated with a collection of values.
        /// The keys are case-insensitive if a string comparer is specified or set to <c>StringComparer.OrdinalIgnoreCase</c> by default.
        /// </summary>
        /// <typeparam name="T">The type of values associated with each key.</typeparam>
        public GenericNameValueCollection(GenericNameValueCollection<T> collection, StringComparer? comparer = null,
            bool isReadOnly = false)
        {
            IsReadOnly = false;
            if(comparer != null) _comparer = comparer;
            foreach (var item in collection)
            {
                Set(item);
            }
            IsReadOnly = isReadOnly;
        }

        private int? _indexFromKey(string? key)
        {
            foreach (var i in Enumerable.Range(0, _keys.Count))
            {
                if (key == null && _keys[i] == null || _comparer.Equals(_keys[i], key)) return i;
            }
            return null;
        }

        /// <summary>
        /// Gets the number of keys currently stored in the <see cref="GenericNameValueCollection{T}"/>.
        /// </summary>
        /// <remarks>
        /// This property represents the total count of key-value pairs in the collection.
        /// Each key can be associated with a collection of values. If the collection is empty, the value of this property will be zero.
        /// </remarks>
        /// <value>
        /// An integer representing the number of keys in the collection.
        /// </value>
        public int Length => _keys.Count;

        /// <summary>
        /// Gets the total number of keys in the <see cref="GenericNameValueCollection{T}"/>.
        /// </summary>
        /// <remarks>
        /// This property provides the count of unique keys stored in the collection.
        /// Each key is associated with one or more values. If the collection is empty, this property returns zero.
        /// </remarks>
        /// <value>
        /// An integer representing the total count of keys in the collection.
        /// </value>
        public int Count => _keys.Count;

        public IEnumerator<KeyValuePair<string?, ICollection<T>>> GetEnumerator()
        {
            return _keys
                .Zip(_values, (k, v) 
                    => new KeyValuePair<string?, ICollection<T>>(k, new ReadOnlyCollection<T>(v.ToArray())))
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Set(KeyValuePair<string?, ICollection<T>> item)
        {
            Set(item.Key, item.Value.IsReadOnly ? new List<T>(item.Value) : item.Value);
        }
        
        public void Set(string? key, ICollection<T> value)
        {
            if (IsReadOnly) throw new InvalidOperationException("Collection is read-only");
            var index = _indexFromKey(key);
            if (index.HasValue)
            {
                _values[index.Value] = new List<T>(value);
            }
            else
            {
                _keys.Add(key);
                _values.Add(new List<T>(value));
            }
        }
        
        public void Add(GenericNameValueCollection<T> collection)
        {
            if(collection == null) throw new ArgumentNullException(nameof(collection));
            foreach (var item in collection) Add(item);
        }
        
        public void Add(KeyValuePair<string?, ICollection<T>> item)
        {
            foreach (var value in item.Value) Add(item.Key, value);
        }

        public void Add(string? key, ICollection<T> value)
        {
            if (IsReadOnly) throw new InvalidOperationException("Collection is read-only");
        }

        public void Add(string? key, T value)
        {
            if (IsReadOnly) throw new InvalidOperationException("Collection is read-only");
            var index = _indexFromKey(key);
            if (index.HasValue)
            {
                _values[index.Value].Add(value);
            }
            else
            {
                _keys.Add(key);
                _values.Add(new List<T> { value });
            }
        }

        /// <summary>
        /// Retrieves a collection of values associated with the specified key.
        /// </summary>
        /// <param name="key">The key for which to retrieve the associated values, or <c>null</c>.</param>
        /// <returns>
        /// A collection of values associated with the specified key, or <c>null</c> if the key does not exist or is <c>null</c>.
        /// </returns>
        public ICollection<T>? Get(string? key)
        {
            var index = _indexFromKey(key);
            if (index == null) return null;
            return new List<T>(_values[index.Value]);
        }

        /// <summary>
        /// Retrieves the collection of values at the specified index in the internal list of collections.
        /// </summary>
        /// <param name="index">The zero-based index of the collection to retrieve.</param>
        /// <returns>
        /// A copy of the collection of values located at the specified index, or <c>null</c> if the index is out of range.
        /// </returns>
        public ICollection<T>? Get(int index)
        {
            if (index < 0 || index >= _values.Count) return null;
            return new List<T>(_values[index]);
        }

        public void Clear()
        {
            if (IsReadOnly) throw new InvalidOperationException("Collection is read-only");
            _keys.Clear();
            _values.Clear();
        }

        public bool Contains(KeyValuePair<string?, ICollection<T>> item)
        {
            return _keys.Contains(item.Key);
        }

        public void CopyTo(KeyValuePair<string?, ICollection<T>>[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0 || arrayIndex > array.Length) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            var index = 0;
            foreach (var key in _keys)
            {
                if (index < arrayIndex) continue;
                var list = new List<T>(_values[index]);
                array[index++] = new KeyValuePair<string?, ICollection<T>>(key, list);
            }
        }

        public bool Remove(KeyValuePair<string?, ICollection<T>> item) => Remove(item.Key);

        int ICollection<KeyValuePair<string?, ICollection<T>>>.Count => _keys.Count;

        bool IDictionary<string?, ICollection<T>>.ContainsKey(string? key)
        {
            return _keys.Contains(key);
        }

        bool IReadOnlyDictionary<string?, ICollection<T>>.TryGetValue(string? key, out ICollection<T> value)
        {
            var index = _indexFromKey(key);
            if (index == null)
            {
                value = new List<T>();
                return false;
            }
            value = new List<T>(_values[index.Value]);
            return true;
        }

        public bool Remove(string? key)
        {
            if (IsReadOnly) throw new InvalidOperationException("Collection is read-only");
            var index = _indexFromKey(key);
            if (index == null) return false;
            _keys.RemoveAt(index.Value);
            _values.RemoveAt(index.Value);
            return true;
        }

        /// <summary>
        /// Retrieves the key at the specified index within the collection.
        /// </summary>
        /// <param name="index">The zero-based index of the key to retrieve.</param>
        /// <returns>The key at the specified index, or <c>null</c> if the key is not found.</returns>
        public string? GetKey(int index) => _keys[index];

        /// <summary>
        /// Gets an array containing all the keys in the <see cref="GenericNameValueCollection{T}"/>.
        /// </summary>
        /// <remarks>
        /// This property returns a string array containing the keys currently stored in the collection.
        /// If the collection is empty, an empty array will be returned. The order of the keys in the array
        /// corresponds to the order in which they were added to the collection.
        /// </remarks>
        /// <value>
        /// An array of strings representing the keys in the collection.
        /// </value>
        public string?[] AllKeys => _keys.ToArray();

        /// <summary>
        /// Determines whether the collection contains any keys.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the collection contains one or more keys; otherwise, <c>false</c>.
        /// </returns>
        public bool HasKeys() => _keys.Count > 0;

        bool IReadOnlyDictionary<string?, ICollection<T>>.ContainsKey(string? key)
        {
            return _keys.Contains(key);
        }

        bool IDictionary<string?, ICollection<T>>.TryGetValue(string? key, out ICollection<T> value)
        {
            var index = _indexFromKey(key);
            if (index == null)
            {
                value = new List<T>();
                return false;
            }
            value = _values[index.Value];
            return true;
        }

        public ICollection<T> this[string? key]
        {
            get
            {
                var index = _indexFromKey(key);
                return index == null ? null! : new List<T>(_values[index.Value]);
            }
            set => Set(key, value);
        }

        /// <summary>
        /// Represents a generic name-value collection that stores multiple values for each key.
        /// </summary>
        /// <typeparam name="T">The type of the values stored in the collection.</typeparam>
        public ICollection<T> this[int index] =>
            _values.Count < index ? throw new IndexOutOfRangeException() : new List<T>(_values[index]);

        IEnumerable<string?> IReadOnlyDictionary<string?, ICollection<T>>.Keys => _keys;

        IEnumerable<ICollection<T>> IReadOnlyDictionary<string?, ICollection<T>>.Values => _values;

        ICollection<string?> IDictionary<string?, ICollection<T>>.Keys => _keys;

        ICollection<ICollection<T>> IDictionary<string?, ICollection<T>>.Values => _values;

        int IReadOnlyCollection<KeyValuePair<string?, ICollection<T>>>.Count => _keys.Count;

        public virtual void OnDeserialization(object sender)
        {
            //Nothing more to do here
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue("_keys", _keys);
            info.AddValue("_values", _values);
            info.AddValue("_comparer", _comparer);
            info.AddValue("IsReadOnly", IsReadOnly);
        }
    }
}