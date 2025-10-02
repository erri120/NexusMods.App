using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using CommunityToolkit.HighPerformance.Buffers;
using Microsoft.Extensions.Hosting;

namespace NexusMods.Backend.Tracking;

internal partial class EventTracker : BackgroundService
{
    private static readonly Uri Endpoint = new("https://api-eu.mixpanel.com/track");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendRequest(stoppingToken);
            }
            catch (Exception e)
            {
                Debugger.Break();
            }

            await Task.Delay(delay: TimeSpan.FromSeconds(10), cancellationToken: stoppingToken);
        }
    }

    private async ValueTask SendRequest(CancellationToken cancellationToken)
    {
        using var bufferWriter = PrepareRequest();
        if (bufferWriter is null) return;

        var array = bufferWriter.DangerousGetArray().Array;
        Debug.Assert(array is not null);

        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        request.Content = new ByteArrayContent(array)
        {
            Headers =
            {
                ContentType = new MediaTypeHeaderValue(mediaType: MediaTypeNames.Application.Json),
            },
        };
        
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType: MediaTypeNames.Application.Json, quality: 1.0));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType: MediaTypeNames.Text.Plain, quality: 0.5));

        try
        {
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            Debugger.Break();

            if (response.StatusCode is HttpStatusCode.BadRequest)
            {
                
            }
        }
        catch (Exception e)
        {
            Debugger.Break();
            // TODO:
        }
    }

    private ArrayPoolBufferWriter<byte>? PrepareRequest()
    {
        _insertRingBuffer.CopyTo(_sortedReadingCopy, index: 0);
        Array.Sort(_sortedReadingCopy);

        var span = CreateSpan(_sortedReadingCopy, _highestSeenId);
        if (span.IsEmpty) return null;

        _highestSeenId = span[^1].Id;

        var minRequestSize = 2; // []
        foreach (var preparedEvent in span)
        {
            Debug.Assert(preparedEvent.IsInitialized);
            minRequestSize += preparedEvent.BufferWriter.WrittenCount;
        }

        minRequestSize += span.Length; // each comma

        var bufferWriter = new ArrayPoolBufferWriter<byte>(pool: _arrayPool, initialCapacity: minRequestSize);

        {
            var jsonWriter = GetWriter(bufferWriter);

            jsonWriter.WriteStartArray();

            foreach (var preparedEvent in span)
            {
                Debug.Assert(preparedEvent.IsInitialized);
                jsonWriter.WriteRawValue(preparedEvent.BufferWriter.WrittenSpan);
                preparedEvent.Dispose();
            }

            jsonWriter.WriteEndArray();
            ReturnWriter(jsonWriter);
        }

        return bufferWriter;
    }

    private static ReadOnlySpan<PreparedEvent> CreateSpan(PreparedEvent[] input, ulong highestSeenId)
    {
        Debug.Assert(input.Length == MaxEvents);

        // Only two possibilities:
        // 1) last highest seen ID is in the array -> start span after that element
        // 2) last highest seen ID is not in the array -> entire array has new events

        // Fast paths since IDs are monotonic increasing and we sort ascending
        if (input[0].Id > highestSeenId) return input;
        if (input[^1].Id == highestSeenId) return ReadOnlySpan<PreparedEvent>.Empty;

        var index = Array.BinarySearch(input, new PreparedEvent(Id: highestSeenId, null!, null!));
        Debug.Assert(index > 0 && index < input.Length - 1);

        var startIndex = index + 1;
        return input.AsSpan(start: startIndex);
    }
}
