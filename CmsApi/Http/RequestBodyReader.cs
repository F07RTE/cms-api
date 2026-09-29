using CmsApi.Core.Exceptions;

namespace CmsApi.Http;

public static class RequestBodyReader
{
    private const int ChunkBytes = 81920;

    /// <summary>Reads the raw body, stopping as soon as it passes <paramref name="maxBytes"/>.</summary>
    /// <exception cref="BatchTooLargeException">The body is larger than <paramref name="maxBytes"/>.</exception>
    public static async Task<ReadOnlyMemory<byte>> ReadBodyAsync(
        this HttpRequest request,
        int maxBytes,
        CancellationToken cancellationToken
    )
    {
        if (request.ContentLength is { } declaredBytes && declaredBytes > maxBytes)
        {
            throw new BatchTooLargeException(declaredBytes);
        }

        // Chunked bodies have no Content-Length, so the limit is also enforced while reading.
        using var buffer = new MemoryStream();
        var chunk = new byte[ChunkBytes];
        int read;
        while ((read = await request.Body.ReadAsync(chunk, cancellationToken)) > 0)
        {
            var receivedBytes = buffer.Length + read;
            if (receivedBytes > maxBytes)
            {
                throw new BatchTooLargeException(receivedBytes);
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer.GetBuffer().AsMemory(0, (int)buffer.Length);
    }
}
