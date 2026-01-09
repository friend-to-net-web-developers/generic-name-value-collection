using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;

namespace FriendToNetWebDevelopers.GenericNameValueCollection
{
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
        
        public GenericNameValueCollection()
        {
            IsReadOnly = false;
        }

        public GenericNameValueCollection(int capacity, StringComparer? comparer = null)
        {
            IsReadOnly = false;
            if(comparer != null) _comparer = comparer;
            _keys = new List<string?>(capacity);
            _values = new List<ICollection<T>>(capacity);
        }
        
        public GenericNameValueCollection(StringComparer comparer)
        {
            IsReadOnly = false;
            _comparer = comparer;
        }

        public GenericNameValueCollection(ICollection<KeyValuePair<string?, ICollection<T>>> collection, StringComparer? comparer = null, bool isReadOnly = false)
        {
            IsReadOnly = false;
            if(comparer != null) _comparer = comparer;
            foreach (var item in collection)
            {
                Set(item);
            }
            IsReadOnly = isReadOnly;
        }
        
        public GenericNameValueCollection(GenericNameValueCollection<T> collection, StringComparer? comparer = null, bool isReadOnly = false)
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
        
        public int Length => _keys.Count;
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

        public ICollection<T>? Get(string? key)
        {
            var index = _indexFromKey(key);
            if (index == null) return null;
            return new List<T>(_values[index.Value]);
        }

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

        public string? GetKey(int index) => _keys[index];
        public string?[] AllKeys => _keys.ToArray();
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