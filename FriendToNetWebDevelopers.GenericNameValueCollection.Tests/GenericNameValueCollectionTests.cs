using System.Collections;
using System.Collections.Specialized;

namespace FriendToNetWebDevelopers.GenericNameValueCollection.Tests;

public class GenericNameValueCollectionTests
{
    [Test]
    public void Constructor_Default_CreatesEmptyCollection()
    {
        var collection = new GenericNameValueCollection<string>();
        Assert.That(collection.Count, Is.EqualTo(0));
    }

    [Test]
    public void Constructor_WithCapacity_CreatesEmptyCollection()
    {
        var collection = new GenericNameValueCollection<int>(10);
        Assert.That(collection.Count, Is.EqualTo(0));
    }

    [Test]
    public void Constructor_WithExistingCollection_CopiesAllEntries()
    {
        var original = new GenericNameValueCollection<string>();
        original.Add("key1", "value1");
        original.Add("key1", "value2");
        original.Add("key2", "value3");

        var copy = new GenericNameValueCollection<string>(original);

        Assert.That(copy.Count, Is.EqualTo(2));
        Assert.That(copy["key1"], Is.EqualTo(new[] { "value1", "value2" }));
        Assert.That(copy["key2"], Is.EqualTo(new[] { "value3" }));
    }

    [Test]
    public void Add_SingleValue_AddsToCollection()
    {
        var collection = new GenericNameValueCollection<string>();
        collection.Add("name", "value");

        Assert.That(collection.Count, Is.EqualTo(1));
        Assert.That(collection["name"], Is.EqualTo(new[] { "value" }));
    }

    [Test]
    public void Add_MultipleValuesToSameKey_StoresAllValues()
    {
        var collection = new GenericNameValueCollection<int>();
        collection.Add("numbers", 1);
        collection.Add("numbers", 2);
        collection.Add("numbers", 3);

        var values = collection["numbers"];
        Assert.That(values, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void Add_NullKey_AcceptsNullKey()
    {
        var collection = new GenericNameValueCollection<string>();
        collection.Add(null, "value1");
        collection.Add(null, "value2");

        var values = collection[null];
        Assert.That(values, Is.EqualTo(new[] { "value1", "value2" }));
    }

    [Test]
    public void Add_Collection_AddsAllEntries()
    {
        var collection1 = new GenericNameValueCollection<string>();
        collection1.Add("key1", "value1");
        collection1.Add("key2", "value2");

        var collection2 = new GenericNameValueCollection<string>();
        collection2.Add("key3", "value3");
        collection2.Add(collection1);

        Assert.That(collection2.Count, Is.EqualTo(3));
        Assert.That(collection2["key1"], Is.EqualTo(new[] { "value1" }));
        Assert.That(collection2["key2"], Is.EqualTo(new[] { "value2" }));
        Assert.That(collection2["key3"], Is.EqualTo(new[] { "value3" }));
    }

    [Test]
    public void Add_NullCollection_ThrowsArgumentNullException()
    {
        var collection = new GenericNameValueCollection<string>();
        Assert.Throws<ArgumentNullException>(() => collection.Add(null!));
    }

    [Test]
    public void Get_ExistingKey_ReturnsValues()
    {
        var collection = new GenericNameValueCollection<string>();
        collection.Add("key", "value1");
        collection.Add("key", "value2");

        var values = collection.Get("key");
        Assert.That(values, Is.EqualTo(new[] { "value1", "value2" }));
    }

    [Test]
    public void Get_NonExistentKey_ReturnsNull()
    {
        var collection = new GenericNameValueCollection<string>();
        var values = collection.Get("nonexistent");
        Assert.That(values, Is.Null);
    }

    [Test]
    public void Get_NullKey_ReturnsNull()
    {
        var collection = new GenericNameValueCollection<string>();
        var values = collection.Get(null);
        Assert.That(values, Is.Null);
    }

    [Test]
    public void Get_ByIndex_ReturnsValues()
    {
        var collection = new GenericNameValueCollection<double>();
        collection.Add("first", 1.1);
        collection.Add("second", 2.2);

        var values = collection.Get(0);
        Assert.That(values, Is.EqualTo(new[] { 1.1 }));
    }

    [Test]
    public void Get_ByInvalidIndex_ReturnsNull()
    {
        var collection = new GenericNameValueCollection<string>();
        collection.Add("key", "value");

        Assert.That(collection.Get(-1), Is.Null);
        Assert.That(collection.Get(10), Is.Null);
    }

    [Test]
    public void Set_NewKey_AddsEntry()
    {
        var collection = new GenericNameValueCollection<string>();
        collection.Set("key", new[] { "value1", "value2" });

        Assert.That(collection.Count, Is.EqualTo(1));
        Assert.That(collection["key"], Is.Not.Null);
    }

    [Test]
    public void Indexer_Get_ReturnsValues()
    {
        var collection = new GenericNameValueCollection<string>();
        collection.Add("key", "value");

        var values = collection["key"];
        Assert.That(values, Is.EqualTo(new[] { "value" }));
    }

    [Test]
    public void Indexer_Set_UpdatesValues()
    {
        var collection = new GenericNameValueCollection<string>();
        collection["key"] = new[] { "value1", "value2" };

        Assert.That(collection.Count, Is.EqualTo(1));
    }

    [Test]
    public void IndexerByInt_Get_ReturnsValues()
    {
        var collection = new GenericNameValueCollection<string>();
        collection.Add("first", "value1");
        collection.Add("second", "value2");

        Assert.That(collection[0], Is.EqualTo(new[] { "value1" }));
        Assert.That(collection[1], Is.EqualTo(new[] { "value2" }));
    }

    [Test]
    public void GetKey_ValidIndex_ReturnsKey()
    {
        var collection = new GenericNameValueCollection<string>();
        collection.Add("mykey", "value");

        var key = collection.GetKey(0);
        Assert.That(key, Is.EqualTo("mykey"));
    }

    [Test]
    public void Remove_ExistingKey_RemovesEntry()
    {
        var collection = new GenericNameValueCollection<string>();
        collection.Add("key1", "value1");
        collection.Add("key2", "value2");

        collection.Remove("key1");

        Assert.That(collection.Count, Is.EqualTo(1));
        Assert.That(collection["key1"], Is.Null);
        Assert.That(collection["key2"], Is.Not.Null);
    }

    [Test]
    public void Remove_NonExistentKey_DoesNothing()
    {
        var collection = new GenericNameValueCollection<string>();
        collection.Add("key", "value");

        collection.Remove("nonexistent");

        Assert.That(collection.Count, Is.EqualTo(1));
    }

    [Test]
    public void Clear_RemovesAllEntries()
    {
        var collection = new GenericNameValueCollection<string>();
        collection.Add("key1", "value1");
        collection.Add("key2", "value2");

        collection.Clear();

        Assert.That(collection.Count, Is.EqualTo(0));
    }

    [Test]
    public void AllKeys_ReturnsAllKeys()
    {
        var collection = new GenericNameValueCollection<string>();
        collection.Add("key1", "value1");
        collection.Add("key2", "value2");
        collection.Add("key3", "value3");

        var keys = collection.AllKeys;

        Assert.That(keys.Length, Is.EqualTo(3));
        Assert.That(keys, Does.Contain("key1"));
        Assert.That(keys, Does.Contain("key2"));
        Assert.That(keys, Does.Contain("key3"));
    }

    [Test]
    public void AllKeys_EmptyCollection_ReturnsEmptyArray()
    {
        var collection = new GenericNameValueCollection<string>();
        var keys = collection.AllKeys;

        Assert.That(keys, Is.Not.Null);
        Assert.That(keys.Length, Is.EqualTo(0));
    }

    [Test]
    public void HasKeys_EmptyCollection_ReturnsFalse()
    {
        var collection = new GenericNameValueCollection<string>();
        Assert.That(collection.HasKeys(), Is.False);
    }

    [Test]
    public void HasKeys_WithEntries_ReturnsTrue()
    {
        var collection = new GenericNameValueCollection<string>();
        collection.Add("key", "value");

        Assert.That(collection.HasKeys(), Is.True);
    }
    

    [Test]
    public void CopyTo_NullArray_ThrowsArgumentNullException()
    {
        var collection = new GenericNameValueCollection<string>();
        collection.Add("key", "value");

        Assert.Throws<ArgumentNullException>(() => collection.CopyTo(null!, 0));
    }


    [Test]
    public void Constructor_WithEqualityComparer_UsesComparer()
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var collection = new GenericNameValueCollection<string>(comparer);
        collection.Add("KEY", "value1");
        collection.Add("key", "value2");

        // Case-insensitive comparer should treat these as the same key
        Assert.That(collection.Count, Is.EqualTo(1));
        var values = collection["KEY"];
        Assert.That(values, Has.Count.EqualTo(2));
    }

    [Test]
    public void DifferentValueTypes_IntCollection()
    {
        var collection = new GenericNameValueCollection<int>();
        collection.Add("integers", 42);
        collection.Add("integers", 100);

        var values = collection["integers"];
        Assert.That(values, Is.EqualTo(new[] { 42, 100 }));
    }

    [Test]
    public void MultipleOperations_ComplexScenario()
    {
        var collection = new GenericNameValueCollection<string>();

        // Add multiple entries
        collection.Add("key1", "a");
        collection.Add("key1", "b");
        collection.Add("key2", "c");
        collection.Add("key3", "d");

        // Remove one
        collection.Remove("key2");

        // Update one
        collection["key3"] = new[] { "e", "f" };

        // Verify state
        Assert.That(collection.Count, Is.EqualTo(2));
        Assert.That(collection["key1"], Is.EqualTo(new[] { "a", "b" }));
        Assert.That(collection["key2"], Is.Null);
    }

    private class TestObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}