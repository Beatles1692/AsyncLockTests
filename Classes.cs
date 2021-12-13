using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KeyedSemaphores;

namespace AsyncLockTests
{

    internal sealed class LockDictionary : ConcurrentDictionary<string, SemaphoreSlim>
    {
        private static readonly Lazy<LockDictionary> lazy = new Lazy<LockDictionary>(() => Create());
        internal static LockDictionary Instance
        {
            get { return lazy.Value; }
        }
        private LockDictionary(ConcurrentDictionary<string, SemaphoreSlim> dictionary) : base(dictionary)
        { }
        private static LockDictionary Create()
        {
            return new LockDictionary(new ConcurrentDictionary<string, SemaphoreSlim>());
        }
    }

    public static class Lock<T>
    {
        private static LockDictionary _locks = LockDictionary.Instance;
        private static readonly object _cacheLock = new object();

        public static SemaphoreSlim Get(string key)
        {
            lock (_cacheLock)
            {
                if (!_locks.ContainsKey(GetLockKey(key)))
                    return null;
                return _locks[GetLockKey(key)];
            }
        }
        public static SemaphoreSlim Create(string key)
        {
            lock (_cacheLock)
            {
                if (!_locks.ContainsKey(GetLockKey(key)))
                    _locks.TryAdd(GetLockKey(key),
                        new SemaphoreSlim(1, 1));

                return _locks[GetLockKey(key)];
            }
        }
        public static void Remove(string key)
        {
            lock (_cacheLock)
            {
                SemaphoreSlim removedObject;
                if (_locks.ContainsKey(GetLockKey(key)))
                    _locks.TryRemove(GetLockKey(key), out removedObject);
            }
        }
        private static string GetLockKey(string key)
        {
            return $"{typeof(T).Name}_{key}";
        }
    }

    public sealed class LockTransaction<TEntity> : IDisposable
    {
        private readonly string _key;
        public LockTransaction(string key)
        {
            _key = key;
            var transactionLock = Lock<TEntity>.Create(key);
            transactionLock.Wait();
        }

        public void Dispose()
        {
            var transactionLock = Lock<TEntity>.Get(_key);
            transactionLock.Release();
        }
    }


    public class SampleData
    {
        public string Name { get; set; }
    }
    public class SampleClass
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new ConcurrentDictionary<string, SemaphoreSlim>();

        public SampleClass(bool useLocking)
        {
            this.useLocking = useLocking;
        }
        private readonly Dictionary<string, SampleData> _db = new Dictionary<string, SampleData>();
        private readonly bool useLocking;

        public async Task AddData(SampleData data, bool wait = false)
        {
            var mainAction = new Func<Task>(async () =>
            {
                if (!wait) await Task.Delay(100);
                if (!_db.ContainsKey(data.Name))
                {
                    if (wait) await Task.Delay(1000);
                    _db.Add(data.Name, data);
                }
            });

            var lockingAction = new Func<Task>(async() =>
            {
                using var lt =await  KeyedSemaphore.LockAsync(data.Name);
                await mainAction();
            });
            
            
            if (data?.Name == null) return;
            if (useLocking)
            {
                await lockingAction();
            }
            else
            {
                await mainAction();
            }
        }

    }
}