using Microsoft.AspNetCore.Http;

namespace HttpContextMoqSessionTest;

public class InMemorySession : ISession
{

    private readonly IDictionary<string, byte[]?> _dictionary = new Dictionary<string, byte[]?>();

    public InMemorySession()
    {

    }

    /// <inheritdoc />
    public Task LoadAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CommitAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public bool TryGetValue(string key, out byte[]? value)
    {
        return _dictionary.TryGetValue(key, out value);
    }

    /// <inheritdoc />
    public void Set(string key, byte[] value)
    {
        _dictionary.Add(key, value);
    }

    /// <inheritdoc />
    public void Remove(string key)
    {
        _dictionary.Remove(key);
    }

    /// <inheritdoc />
    public void Clear()
    {
        _dictionary.Clear();
    }

    /// <inheritdoc />
    public bool IsAvailable => true;

    /// <inheritdoc />
    public string Id => nameof(InMemorySession);

    /// <inheritdoc />
    public IEnumerable<string> Keys => _dictionary.Keys;
}