namespace Kulfibot.Discord
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.IO.Pipelines;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Http.Json;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;
    using Kulfibot.Discord.Messages;

    public sealed class DiscordMessageTransport : IMessageTransport
    {
        private static readonly Uri Endpoint = new("https://discord.com/api/v9");
        private readonly HttpClient httpClient = new();
        private readonly WebSocketPipe webSocketPipe = new();
        private IBotMessageSink bot = new NullBotMessageSink();
        private Task processingLoop = Task.CompletedTask;
        private bool stopping;
        private readonly ActionBlock<DiscordMessage> processMessages;
        private readonly ActionBlock<IEnumerable<DiscordResponseMessage>> sendMessages;

        public DiscordMessageTransport(
            DiscordSecrets discordSecrets
        )
        {
            this.httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bot", discordSecrets.BotToken);

            this.processMessages = new(m => ProcessMessageAsync(m));
            //could hypothetically
            this.sendMessages = new(ms => SendMessagesToWebsocketAsync(ms));
        }

        public Task SendMessagesAsync(IEnumerable<Message> messages) =>
            this.sendMessages.SendAsync(messages.OfType<DiscordResponseMessage>());

        public async Task StartAsync(IBotMessageSink sink)
        {
            this.bot = sink;
            this.stopping = false;

            HttpResponseMessage gatewayBotResponseMessage = await this.httpClient.GetAsync(GetUri("gateway/bot"));
            _ = gatewayBotResponseMessage.EnsureSuccessStatusCode();
            GatewayBotResponse? gatewayBotResponse =
                await gatewayBotResponseMessage.Content.ReadFromJsonAsync<GatewayBotResponse>();

            //have no idea how this can be null yet
            await this.webSocketPipe.StartAsync(new Uri(gatewayBotResponse!.Url + "?v=9&encoding=json"), CancellationToken.None);

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

                ReadResult readResult = await this.webSocketPipe.Input.ReadAsync();
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

                List<RawPayload> payloads = Parse(buffer, out SequencePosition consumedTo);
                this.webSocketPipe.Input.AdvanceTo(consumedTo);

                foreach (RawPayload payload in payloads)
                {
                    //this admittedly shouldnt take long, and there likely arent many payloads
                    //still, not the best to await here.
                    //TODO: put them in all at once
                    _ = await this.processMessages.SendAsync(DiscordMessage.FromPayload(payload));
                }
            }

            static List<RawPayload> Parse(ReadOnlySequence<byte> buffer, out SequencePosition consumedTo)
            {
                List<RawPayload> payloads = new();
                consumedTo = buffer.Start;

                while (true)
                {
                    if (buffer.IsEmpty)
                    {
                        return payloads;
                    }

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

                    consumedTo = buffer.GetPosition(jsonReader.BytesConsumed);
                    buffer = buffer.Slice(jsonReader.BytesConsumed, buffer.End);
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

        private Task SendMessagesToWebsocketAsync(IEnumerable<DiscordResponseMessage> messages)
        {
            using Utf8JsonWriter jsonWriter = new(this.webSocketPipe.Output);
            foreach (DiscordResponseMessage message in messages)
            {
                message.Serialize(jsonWriter);
            }

            return this.webSocketPipe.Output.FlushAsync().AsTask();
        }

        //TODO: could consider UnknownMessage as a special case
        private Task ProcessMessageAsync(DiscordMessage discordMessage)
        {
            Task rawMessageTask = this.bot.MessageReceivedAsync(discordMessage);

            Message? result = discordMessage switch
            {
                { Opcode: 10 } => discordMessage.ConvertData<HelloMessage>(),
                _ => null
            };

            return result is null
                ? rawMessageTask
                : Task.WhenAll(
                    rawMessageTask,
                    this.bot.MessageReceivedAsync(result)
                );
        }

        private static Uri GetUri(string path) => new(Endpoint, path);

        private record GatewayBotResponse(string Url);
    }
}
