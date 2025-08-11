using System;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Extensions.ValueTask;
using Soenneker.Utils.AtomicResources.Abstract;

namespace Soenneker.Utils.AtomicResources;

/// <summary>
/// Thread-safe holder for a single resource that can be lazily created,
/// atomically reset (swap), and asynchronously torn down.
/// </summary>
public sealed class AtomicResource<T> : IAtomicResource<T> where T : class
{
    private readonly Func<T> _factory;
    private readonly Func<T, ValueTask> _teardown;
    private T? _value;
    private volatile bool _disposed;

    public AtomicResource(Func<T> factory, Func<T, ValueTask> teardown)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _teardown = teardown ?? throw new ArgumentNullException(nameof(teardown));
    }

    public bool IsDisposed => _disposed;

    public T? GetOrCreate()
    {
        if (_disposed)
            return null;

        T? existing = Volatile.Read(ref _value);

        if (existing is not null)
            return existing;

        T created = _factory();
        T? raced = Interlocked.CompareExchange(ref _value, created, null);

        if (raced is null)
        {
            // We published 'created'; check for a dispose race.
            if (_disposed)
            {
                Interlocked.Exchange(ref _value, null);
                _ = _teardown(created); // best-effort cleanup (cannot await here)
                return null;
            }

            return created;
        }

        // Lost the race; tear down our extra
        _ = _teardown(created);
        return raced;
    }


    public T? TryGet() => Volatile.Read(ref _value);

    public async ValueTask Reset()
    {
        if (_disposed)
            return;

        T fresh = _factory();
        T? old = Interlocked.Exchange(ref _value, fresh);

        if (old is null) 
            return;

        try
        {
            await _teardown(old).NoSync();
        }
        catch
        {
            /* ignore */
        }
    }


    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        T? old = Interlocked.Exchange(ref _value, null);

        if (old is null)
            return;

        try
        {
            await _teardown(old).NoSync();
        }
        catch
        {
            /* ignore */
        }
    }
}