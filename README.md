# Generic Name-Value Collection

A generic implementation of `NameValueCollection` for .NET Standard 2.1+. This library provides a type-safe, strongly-typed alternative to the classic `NameValueCollection` class, allowing you to store multiple values per key with any type you specify.

## Features

- **Type-safe**: Use any type for values (`string`, `int`, custom objects, etc.)
- **Multiple values per key**: Store multiple values under the same key
- **Case-insensitive keys by default**: Configurable string comparison
- **Nullable key support**: Keys can be null
- **Read-only collections**: Create immutable collections
- **Serialization support**: Implements `ISerializable`
- **Dictionary-like interface**: Implements `IDictionary<string?, ICollection<T>>`, `IReadOnlyDictionary<string?, ICollection<T>>`, and more
- **Ordered**: Maintains the order of keys as they are added
- **Flexible Lookups**: Access values by key (string) or by index (integer)

## Installation

```bash
dotnet add package FriendToNetWebDevelopers.GenericNameValueCollection
```

## Basic Usage

### Creating a Collection

```csharp
using FriendToNetWebDevelopers.GenericNameValueCollection;

// Create an empty collection
var collection = new GenericNameValueCollection<string>();

// Create with initial capacity and optional comparer
var collection2 = new GenericNameValueCollection<int>(capacity: 100);

// Create with custom string comparer
var collection3 = new GenericNameValueCollection<string>(StringComparer.Ordinal);
```

### Adding Values

```csharp
var collection = new GenericNameValueCollection<string>();

// Add single values
collection.Add("name", "John");
collection.Add("name", "Jane"); // Multiple values for same key

// Set replaces all existing values for a key
collection.Set("emails", new List<string> { "john@example.com", "jane@example.com" });

// Use indexer to set values
collection["phone"] = new List<string> { "555-0100", "555-0101" };
```

### Retrieving Values

```csharp
// Get by key
ICollection<string>? names = collection.Get("name");
// Returns: ["John", "Jane"]

// Get by index
ICollection<string>? firstEntryValues = collection.Get(0);

// Use indexer
ICollection<string> phones = collection["phone"];

// Returns null if key doesn't exist
ICollection<string>? missing = collection.Get("nonexistent"); // null
```

### Working with Keys

```csharp
// Get all keys
string?[] keys = collection.AllKeys;

// Get key by index
string? key = collection.GetKey(0);

// Check if collection has any keys
bool hasKeys = collection.HasKeys();

// Get count
int count = collection.Count;
```

### Removing Values

```csharp
// Remove by key
bool removed = collection.Remove("name");

// Clear all entries
collection.Clear();
```

## Advanced Usage

### Using Different Value Types

```csharp
// Integer collection
var numbers = new GenericNameValueCollection<int>();
numbers.Add("primes", 2);
numbers.Add("primes", 3);
numbers.Add("primes", 5);

// Custom object collection
var people = new GenericNameValueCollection<Person>();
people.Add("employees", new Person { Name = "Alice", Age = 30 });
```

### Case-Sensitive Keys

```csharp
// Case-insensitive (default)
var caseInsensitive = new GenericNameValueCollection<string>();
caseInsensitive.Add("KEY", "value1");
caseInsensitive.Add("key", "value2");
// Both added to the same key, Count = 1

// Case-sensitive
var caseSensitive = new GenericNameValueCollection<string>(StringComparer.Ordinal);
caseSensitive.Add("KEY", "value1");
caseSensitive.Add("key", "value2");
// Added as separate keys, Count = 2
```

### Read-Only Collections

```csharp
var original = new GenericNameValueCollection<string>();
original.Add("key", "value");

// Create a read-only copy
var readOnly = new GenericNameValueCollection<string>(original, isReadOnly: true);

// This will throw InvalidOperationException
// readOnly.Add("key2", "value2");
```

### Handling Null Keys

```csharp
var collection = new GenericNameValueCollection<string>();

// Add values with null key
collection.Add(null, "value1");
collection.Add(null, "value2");

// Retrieve values with null key
ICollection<string> values = collection[null];
// Returns: ["value1", "value2"]
```

## Target Frameworks

- .NET Standard 2.1
- .NET 8.0
- .NET 9.0
- .NET 10.0

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Repository

https://github.com/friend-to-net-web-developers/generic-name-value-collection
