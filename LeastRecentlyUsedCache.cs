using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace LRUCache
{
    public class LeastRecentlyUsedCache<TKey, TValue>
    {
        private class ValueContainer
        {
            internal readonly TValue Value;
            internal readonly LinkedListNode<TKey> KeyNode; 

            public ValueContainer(TValue value, LinkedListNode<TKey> keyNode)
            {
                Value = value;
                KeyNode = keyNode;
            }
        }

        private readonly int _capacity;
        private readonly Dictionary<TKey, ValueContainer> _valueStore;
        private readonly LinkedList<TKey> _keysInAccessOrder;

        // The below two enumerables are just to facilitate unit test assertions on the underlying data structures
        public IEnumerable<TKey> KeysInOrder { get { return _keysInAccessOrder; } }
        public IEnumerable<KeyValuePair<TKey, TValue>> KeysAndValues
        {
            get
            {
                return _valueStore.Select(i => new KeyValuePair<TKey, TValue>(i.Key, i.Value.Value));
            }
        }

        public LeastRecentlyUsedCache(int capacity)
        {
            _capacity = capacity;
            _valueStore = new Dictionary<TKey, ValueContainer>();
            _keysInAccessOrder = new LinkedList<TKey>();
        }

        private void UpdateAccessList(LinkedListNode<TKey> node)
        {
            _keysInAccessOrder.Remove(node);
            _keysInAccessOrder.AddFirst(node);
        }

        private void UpdateValueForKey(TKey key, TValue value)
        {
            var item = _valueStore[key];
            UpdateAccessList(item.KeyNode);
            _valueStore[key] = new ValueContainer(value, item.KeyNode);
        }

        private void AddValueForKey(TKey key, TValue value)
        {
            var node = _keysInAccessOrder.AddFirst(key);
            _valueStore.Add(key, new ValueContainer(value, node));
        }

        private void Evict(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var last = _keysInAccessOrder.Last;
                _keysInAccessOrder.Remove(last);
                _valueStore.Remove(last.Value);
            }
        }

        public void Set(TKey key, TValue value)
        {
            if (_valueStore.ContainsKey(key))
            {
                UpdateValueForKey(key, value);
                return;
            }

            AddValueForKey(key, value);

            var excess = _keysInAccessOrder.Count - _capacity;
            if (excess > 0)
            {
                Evict(excess);
            }
        }

        public TValue Get(TKey key)
        {
            if (!_valueStore.ContainsKey(key))
                throw new KeyNotFoundException();

            var item = _valueStore[key];
            UpdateAccessList(item.KeyNode);

            return item.Value;
        }

        public void Remove(TKey key)
        {
            var item = _valueStore[key];
            _valueStore.Remove(key);
            _keysInAccessOrder.Remove(item.KeyNode);
        }
    }

    public class Tests
    {
        [Fact]
        public void Test()
        {
            var lru = new LeastRecentlyUsedCache<int, string>(3);

            lru.Set(1, "a");
            lru.Set(2, "b");

            lru.KeysInOrder.Should().ContainInOrder(2, 1);

            lru.Get(1);

            lru.KeysInOrder.Should().ContainInOrder(1, 2);

            lru.Set(3, "c");
            lru.Set(4, "d");

            lru.KeysInOrder.Should().ContainInOrder(4, 3, 1);

            Assert.Throws<KeyNotFoundException>(() => lru.Get(2));
            
            lru.Set(2, "bb");

            lru.KeysInOrder.Should().ContainInOrder(2, 4, 3);
            lru.KeysAndValues.Should().Contain(new List<KeyValuePair<int, string>>
            {
                new KeyValuePair<int, string>(3, "c"),
                new KeyValuePair<int, string>(2, "bb"),
                new KeyValuePair<int, string>(4, "d")
            });

            lru.Get(4);
            lru.Set(1, "aaa");
            lru.Set(2, "bbb");
            lru.Set(3, "ccc");

            lru.KeysInOrder.Should().ContainInOrder(3, 2, 1);
            lru.KeysAndValues.Should().Contain(new List<KeyValuePair<int, string>>
            {
                new KeyValuePair<int, string>(1, "aaa"),
                new KeyValuePair<int, string>(3, "ccc"),
                new KeyValuePair<int, string>(2, "bbb")
            });

            lru.Remove(2);

            lru.KeysInOrder.Should().ContainInOrder(3, 1);
            lru.KeysAndValues.Should().Contain(new List<KeyValuePair<int, string>>
            {
                new KeyValuePair<int, string>(3, "ccc"),
                new KeyValuePair<int, string>(1, "aaa")
            });
        }
    }
}