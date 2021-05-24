namespace Kulfibot.ConsoleApp
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Kulfibot.Discord;
    using Kulfibot.Discord.Messages;

    internal sealed class Program
    {
#pragma warning disable IDE0060
        public static async Task Main(string[] args)
#pragma warning restore IDE0060
        {
            DiscordSecrets discordSecrets = await DiscordSecrets.FromFileAsync("secrets.json");
            DiscordMessageTransport discordMessageTransport = new(discordSecrets);
            DebugMessageConsoleWriter debugHandler = new();
            ConnectionMaintainerHandler connectionMaintainerHandler = new(discordSecrets);
            ClockMessageTransport clockMessageTransport = new();
            BotConfiguration configuration = new(
                ImmutableList.Create<IMessageTransport>(discordMessageTransport, clockMessageTransport),
                ImmutableList.Create<IMessageHandler>(debugHandler, connectionMaintainerHandler)
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
                message is DiscordMessage ? MessageIntent.Passive : MessageIntent.Ignore;

            public Task<IEnumerable<Message>> HandleAsync(Message message)
            {
                //wont throw anyway
                DiscordMessage discordMessage = message as DiscordMessage ?? throw new InvalidOperationException();

                Console.WriteLine(
                    $"[{DateTimeOffset.Now:o}] Seq={discordMessage.Sequence} Op={discordMessage.Opcode} Name={discordMessage.Name} Data='{discordMessage.Data}'");

                return Messages.NoneAsync;
            }
        }
    }
}
