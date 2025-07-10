namespace AgentMarketer.Shared.Contracts;

/// <summary>
/// Service for Redis operations with JSON serialization
/// </summary>
public interface IRedisService
{
    /// <summary>
    /// Get a value from Redis and deserialize it
    /// </summary>
    /// <typeparam name="T">Type to deserialize to</typeparam>
    /// <param name="key">Redis key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deserialized value or default if not found</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Set a value in Redis with JSON serialization
    /// </summary>
    /// <typeparam name="T">Type to serialize</typeparam>
    /// <param name="key">Redis key</param>
    /// <param name="value">Value to store</param>
    /// <param name="expiry">Optional expiration time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a key from Redis
    /// </summary>
    /// <param name="key">Redis key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if key was deleted, false if key didn't exist</returns>
    Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all keys matching a pattern and return their values
    /// </summary>
    /// <typeparam name="T">Type to deserialize to</typeparam>
    /// <param name="pattern">Redis key pattern (supports wildcards)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of deserialized values</returns>
    Task<List<T>> GetByPatternAsync<T>(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all keys matching a pattern
    /// </summary>
    /// <param name="pattern">Redis key pattern (supports wildcards)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching keys</returns>
    Task<List<string>> GetKeysAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a key exists in Redis
    /// </summary>
    /// <param name="key">Redis key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if key exists, false otherwise</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Set expiration time for a key
    /// </summary>
    /// <param name="key">Redis key</param>
    /// <param name="expiry">Expiration time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if expiration was set, false if key doesn't exist</returns>
    Task<bool> ExpireAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publish a message to a Redis channel
    /// </summary>
    /// <typeparam name="T">Type to serialize</typeparam>
    /// <param name="channel">Channel name</param>
    /// <param name="message">Message to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of subscribers that received the message</returns>
    Task<long> PublishAsync<T>(string channel, T message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribe to Redis channels and receive messages
    /// </summary>
    /// <typeparam name="T">Type to deserialize messages to</typeparam>
    /// <param name="pattern">Channel pattern (supports wildcards)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of messages</returns>
    IAsyncEnumerable<T> SubscribeAsync<T>(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increment a numeric value in Redis
    /// </summary>
    /// <param name="key">Redis key</param>
    /// <param name="value">Value to increment by (default 1)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New value after increment</returns>
    Task<long> IncrementAsync(string key, long value = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add items to a Redis list
    /// </summary>
    /// <typeparam name="T">Type to serialize</typeparam>
    /// <param name="key">Redis key</param>
    /// <param name="values">Values to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Length of list after addition</returns>
    Task<long> ListAddAsync<T>(string key, IEnumerable<T> values, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all items from a Redis list
    /// </summary>
    /// <typeparam name="T">Type to deserialize to</typeparam>
    /// <param name="key">Redis key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of deserialized items</returns>
    Task<List<T>> ListGetAllAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove items from a Redis list
    /// </summary>
    /// <param name="key">Redis key</param>
    /// <param name="count">Number of items to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of items removed</returns>
    Task<long> ListRemoveAsync(string key, long count = 1, CancellationToken cancellationToken = default);
}
