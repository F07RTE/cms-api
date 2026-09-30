using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace CmsApi.IntegrationTests;

public sealed class RecordedLogs : ILoggerProvider
{
    private readonly ConcurrentQueue<RecordedLog> logs = new();

    public List<RecordedLog> ForBatch(long batchId) =>
        [
            .. logs.Where(log =>
                log.Properties.TryGetValue(RecordedLog.BatchId, out var value)
                && value is long id
                && id == batchId
            ),
        ];

    public void Clear() => logs.Clear();

    public ILogger CreateLogger(string categoryName) => new Recorder(logs);

    public void Dispose() { }

    private sealed class Recorder(ConcurrentQueue<RecordedLog> logs) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            var properties = state as IEnumerable<KeyValuePair<string, object?>> ?? [];
            logs.Enqueue(new RecordedLog(logLevel, properties.ToDictionary()));
        }
    }
}

public sealed record RecordedLog(LogLevel Level, IReadOnlyDictionary<string, object?> Properties)
{
    public const string BatchId = "BatchId";
    public const string ContentEntityId = "ContentEntityId";
    public const string Outcome = "Outcome";
    public const string Reason = "Reason";
}
