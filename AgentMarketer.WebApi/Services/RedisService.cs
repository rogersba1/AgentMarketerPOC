using StackExchange.Redis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using AgentMarketer.Shared.Contracts;

namespace AgentMarketer.WebApi.Services;

/// <summary>
/// Redis service implementation with JSON serialization
/// </summary>
public class RedisService : IRedisService
{
    private readonly IDatabase _database;
    private readonly ISubscriber _subscriber;
    private readonly IServer _server;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisService(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
        _subscriber = redis.GetSubscriber();
        _server = redis.GetServer(redis.GetEndPoints().First());
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var value = await _database.StringGetAsync(key);
        
        if (!value.HasValue)
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value, _jsonOptions);
        await _database.StringSetAsync(key, json, expiry);
    }

    public async Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _database.KeyDeleteAsync(key);
    }

    public async Task<List<T>> GetByPatternAsync<T>(string pattern, CancellationToken cancellationToken = default)
    {
        var keys = _server.Keys(pattern: pattern);
        var results = new List<T>();

        foreach (var key in keys)
        {
            var value = await GetAsync<T>(key!, cancellationToken);
            if (value != null)
            {
                results.Add(value);
            }
        }

        return results;
    }

    public async Task<List<string>> GetKeysAsync(string pattern, CancellationToken cancellationToken = default)
    {
        var keys = _server.Keys(pattern: pattern);
        return await Task.FromResult(keys.Select(k => k.ToString()).ToList());
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _database.KeyExistsAsync(key);
    }

    public async Task<bool> ExpireAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        return await _database.KeyExpireAsync(key, expiry);
    }

    public async Task<long> PublishAsync<T>(string channel, T message, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(message, _jsonOptions);
        return await _subscriber.PublishAsync(RedisChannel.Literal(channel), json);
    }

    public async IAsyncEnumerable<T> SubscribeAsync<T>(string pattern, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channelMessageQueue = await _subscriber.SubscribeAsync(RedisChannel.Pattern(pattern));
        
        while (!cancellationToken.IsCancellationRequested)
        {
            ChannelMessage item;
            
            try
            {
                item = await channelMessageQueue.ReadAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (InvalidOperationException)
            {
                // Channel was completed/closed
                break;
            }
            
            if (item.Message.HasValue)
            {
                var result = TryDeserializeMessage<T>(item.Message!);
                if (result != null)
                {
                    yield return result;
                }
            }
        }
    }

    private T? TryDeserializeMessage<T>(RedisValue message)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(message!, _jsonOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    public async Task<long> IncrementAsync(string key, long value = 1, CancellationToken cancellationToken = default)
    {
        return await _database.StringIncrementAsync(key, value);
    }

    public async Task<long> ListAddAsync<T>(string key, IEnumerable<T> values, CancellationToken cancellationToken = default)
    {
        var serializedValues = values.Select(v => (RedisValue)JsonSerializer.Serialize(v, _jsonOptions)).ToArray();
        return await _database.ListRightPushAsync(key, serializedValues);
    }

    public async Task<List<T>> ListGetAllAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var values = await _database.ListRangeAsync(key);
        var results = new List<T>();

        foreach (var value in values)
        {
            try
            {
                var item = JsonSerializer.Deserialize<T>(value!, _jsonOptions);
                if (item != null)
                {
                    results.Add(item);
                }
            }
            catch (JsonException)
            {
                // Skip invalid items
                continue;
            }
        }

        return results;
    }

    public async Task<long> ListRemoveAsync(string key, long count = 1, CancellationToken cancellationToken = default)
    {
        var removed = 0L;
        for (var i = 0; i < count; i++)
        {
            var value = await _database.ListLeftPopAsync(key);
            if (value.HasValue)
                removed++;
            else
                break;
        }
        return removed;
    }
}
