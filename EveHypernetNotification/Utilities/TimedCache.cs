namespace EveHypernetNotification.Utilities;

public class TimedCache<TK, TV> where TK : notnull
{
    private readonly Dictionary<TK, (DateTime, TV)> _cache = new();

    public TV? Get(TK key)
    {
        if (!_cache.TryGetValue(key, out var value))
            return default;

        return value.Item1 > DateTime.UtcNow ? value.Item2 : default;
    }

    public void Set(TK key, TV value, TimeSpan timeSpan)
    {
        _cache[key] = (DateTime.UtcNow + timeSpan, value);
    }

    public void Set(TK key, TV value, DateTime expireDate)
    {
        _cache[key] = (expireDate, value);
    }

    public void Remove(TK key)
    {
        _cache.Remove(key);
    }

    public void Clear()
    {
        _cache.Clear();
    }

    public bool ContainsKey(TK key)
    {
        return TryGetValue(key, out _);
    }

    public bool TryGetValue(TK key, out TV? value)
    {
        if (!_cache.TryGetValue(key, out var cacheValue))
        {
            value = default;
            return false;
        }

        if (cacheValue.Item1 > DateTime.UtcNow)
        {
            value = cacheValue.Item2;
            return true;
        }

        value = default;
        return false;
    }
}