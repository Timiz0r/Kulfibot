namespace Kulfibot.ConsoleApp
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Threading;
    using System.Threading.Tasks;
    using Kulfibot.Discord;
    using static Kulfibot.Discord.DiscordMessageTransport;

    internal sealed class Program
    {
#pragma warning disable IDE0060
        public static async Task Main(string[] args)
#pragma warning restore IDE0060
        {
            DiscordSecrets discordSecrets = await DiscordSecrets.FromFileAsync("secrets.json");
            DiscordMessageTransport transport = new(discordSecrets);
            DebugMessageConsoleWriter handler = new();
            BotConfiguration configuration = new(
                ImmutableList.Create<IMessageTransport>(transport),
                ImmutableList.Create<IMessageHandler>(handler)
            );
            Bot bot = new(configuration);
            await using (await bot.RunAsync())
            {
                TaskCompletionSource tcs = new();
                Console.CancelKeyPress += (s, e) =>
                {
                    e.Cancel = true;
                    tcs.SetResult();
                };
                await tcs.Task;
            }
            System.Diagnostics.Debugger.Break();
        }

        private class DebugMessageConsoleWriter : IMessageHandler
        {
            public MessageIntent DeclareIntent(Message message) =>
                message is DebugMessage ? MessageIntent.Passive : MessageIntent.Ignore;

            public Task<IEnumerable<Message>> HandleAsync(Message message)
            {
                //wont throw anyway
                DebugMessage debugMessage = message as DebugMessage ?? throw new InvalidOperationException();
                RawPayload payload = debugMessage.RawPayload;

                Console.WriteLine($"[{DateTimeOffset.Now:o}] Seq={payload.Sequence} Op={payload.Opcode} Name={payload.Name} Data='{payload.Data}'");

                return Messages.NoneAsync;
            }
        }
    }
}
