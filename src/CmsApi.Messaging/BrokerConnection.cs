using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace CmsApi.Messaging;

// One connection per process, opened on first use; channels are opened per use from it.
public sealed class BrokerConnection(IOptions<MessagingOptions> options) : IAsyncDisposable
{
    private const string ClientName = "cms-api";

    private readonly SemaphoreSlim gate = new(1, 1);
    private IConnection? connection;

    public async Task<IConnection> ConnectAsync(CancellationToken cancellationToken)
    {
        if (connection is not null)
        {
            return connection;
        }

        await gate.WaitAsync(cancellationToken);
        try
        {
            return connection ??= await OpenAsync(cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (connection is not null)
        {
            await connection.DisposeAsync();
        }

        gate.Dispose();
    }

    private Task<IConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            Uri = new Uri(options.Value.ConnectionString),
            ClientProvidedName = ClientName,
        };
        return factory.CreateConnectionAsync(cancellationToken);
    }
}
