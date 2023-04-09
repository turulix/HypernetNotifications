using System.Collections.Concurrent;
using Timer = System.Timers.Timer;

namespace EveHypernetNotification.Utilities;

public class BatchingManager<T> : IDisposable
{
    private readonly int _batchSize;
    private readonly ConcurrentQueue<T> _queue = new();
    private readonly Action<List<T>> _callback;
    private readonly ReaderWriterLockSlim _lock = new();

    public BatchingManager(int batchSize, Action<List<T>> callback)
    {
        _batchSize = batchSize;
        _callback = callback;
    }

    public void Add(T item)
    {
        _lock.EnterWriteLock();
        try
        {
            _queue.Enqueue(item);
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        Execute();
    }

    public void AddRange(IEnumerable<T> items)
    {
        _lock.EnterWriteLock();
        try
        {
            foreach (var item in items)
            {
                _queue.Enqueue(item);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        Execute();
    }

    private void Execute(bool isFlush = false)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_queue.Count <= _batchSize && !isFlush)
                return;

            var items = new List<T>();
            while (_queue.TryDequeue(out var item) && (items.Count < _batchSize || isFlush))
            {
                items.Add(item);
            }

            _callback(items);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Flush()
    {
        Execute(true);
    }

    public void Dispose()
    {
        Flush();
    }
}