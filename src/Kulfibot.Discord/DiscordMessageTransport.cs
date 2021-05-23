namespace Kulfibot.Discord
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.IO.Pipelines;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Http.Json;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class DiscordMessageTransport : IMessageTransport
    {
        private static readonly Uri Endpoint = new("https://discord.com/api/v9");
        private readonly HttpClient httpClient = new();
        private readonly WebSocketPipe webSocketPipe = new();
        private readonly DiscordConfiguration discordConfiguration;
        private IBotMessageSink bot = new NullBotMessageSink();
        private Task processingLoop = Task.CompletedTask;
        private bool stopping;

        public DiscordMessageTransport(
            DiscordConfiguration discordConfiguration
        )
        {
            this.discordConfiguration = discordConfiguration;

            this.httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bot", discordConfiguration.BotToken);
        }

        public Task SendMessagesAsync(IEnumerable<Message> message) => throw new System.NotImplementedException();
        public async Task StartAsync(IBotMessageSink sink)
        {
            this.bot = sink;
            this.stopping = false;

            HttpResponseMessage gatewayBotResponseMessage = await this.httpClient.GetAsync(GetUri("/gateway/bot"));
            _ = gatewayBotResponseMessage.EnsureSuccessStatusCode();
            GatewayBotResponse? gatewayBotResponse =
                await gatewayBotResponseMessage.Content.ReadFromJsonAsync<GatewayBotResponse>();

            //have no idea how this can be null yet
            await this.webSocketPipe.StartAsync(new Uri(gatewayBotResponse!.Url), CancellationToken.None);

            processingLoop = ProcessingLoop();
        }

        public async Task StopAsync()
        {
            //TODO: also revisit setting this later
            this.stopping = true;
            await this.webSocketPipe.StopAsync();
            await this.processingLoop;
        }

        public Task StoppingAsync()
        {
            this.stopping = true;
            return Task.CompletedTask;
        }

        private async Task ProcessingLoop()
        {
            while (true)
            {
                //TODO: at time of writing, this method only does reading
                //it'll also do heartbeats, so this positioning perhaps changes.
                if (this.stopping) break;

                ReadResult readResult = await webSocketPipe.Input.ReadAsync();
                ReadOnlySequence<byte> buffer = readResult.Buffer;

                if (readResult.IsCanceled)
                {
                    break;
                }

                if (readResult.IsCompleted && buffer.IsEmpty)
                {
                    break;
                }

                if (buffer.IsEmpty)
                {
                    continue;
                }

                List<RawPayload> payloads = Parse(buffer);
                foreach (RawPayload payload in payloads)
                {
                    //this admittedly shouldnt take long, and there likely arent many payloads
                    //still, not the best to await here.
                    //TODO: instead, use tpl dataflow.
                    await bot.MessageReceivedAsync(new DebugMessage(payload));
                }
            }

            static List<RawPayload> Parse(ReadOnlySequence<byte> buffer)
            {
                List<RawPayload> payloads = new();

                while (true)
                {
                    //avoid JsonSerializer for this because culture invariant JsonException handling may get weird
                    //(depending on localization).
                    //rather, inspecting the message may not work as expected in japan (etc).
                    Utf8JsonReader jsonReader = new(buffer);

                    Utf8JsonReader fullMessageChecker = jsonReader;
                    _ = fullMessageChecker.Read();
                    if (!fullMessageChecker.TrySkip())
                    {
                        return payloads;
                    }

                    RawPayload payload = JsonSerializer.Deserialize<RawPayload>(ref jsonReader)!;
                    payloads.Add(payload);

                    buffer = buffer.Slice(jsonReader.BytesConsumed + 1, buffer.End);
                }

                //things to think about:
                //want to deserialize a JsonElement, so kinda gotta use JsonSerializer
                //if preferring to mostly use Utf8JsonReader directly...
                //  deserialize opcode
                //  then copy the state.
                //  then, move to start of data and TrySkip. if fail, then we have incomplete data.
                //  then continue rest of manual deserialization.
                //  if not throw and finished (complete data), then create a new Utf8JsonReader used copied state,
                //    then deserialize JsonElement.
                //instead, just using JsonSerializer entirely, for now.

                //another limitation is that the .net deserializer can only do one message at a time
                //would rather work around it with some slicing than awaiting
            }
        }

        private static Uri GetUri(string path) => new Uri(Endpoint, path);

        private record GatewayBotResponse(string Url);

        public record RawPayload
        {
            [JsonPropertyName("op")]
            public int Opcode { get; init; }

            [JsonPropertyName("d")]
            public JsonElement? Data { get; init; }

            [JsonPropertyName("s")]
            public int? Sequence { get; init; }

            [JsonPropertyName("t")]
            public string? Name { get; init; }
        }

        public record DebugMessage(RawPayload RawPayload) : Message;
    }
}
