namespace Kulfibot.Test
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class BotSimulator
    {
        private readonly SimulatorMessageTransport messageTransport = new();
        private readonly SimulatorMessageHandler messageHandler = new();

        public BotSimulator()
        {
            Messages = new MessageRecord(this);
        }

        public MessageRecord Messages { get; }

        public BotConfiguration AsBotConfiguration() => AsBotConfiguration(
            new BotConfiguration(
                Array.Empty<IMessageTransport>(),
                Array.Empty<IMessageHandler>()
            ));

        public BotConfiguration AsBotConfiguration(BotConfiguration basis) => new(
            MessageTransports: basis.MessageTransports.Concat(new[] { messageTransport }).ToArray(),
            MessageHandlers: basis.MessageHandlers.Concat(new[] { messageHandler }).ToArray()
        );

        public Task<IAsyncDisposable> RunBotAsync()
        {
            BotConfiguration configuration = AsBotConfiguration();

            Bot bot = new(configuration);
            return bot.RunAsync();
        }

        public Task<IAsyncDisposable> RunBotAsync(BotConfiguration basis)
        {
            BotConfiguration configuration = AsBotConfiguration(basis);

            //this much code duplication is fine
            Bot bot = new(configuration);
            return bot.RunAsync();
        }

        //the things we do for an ideal interface
        internal class MessageRecord
        {
            private readonly BotSimulator simulator;

            public MessageRecord(BotSimulator simulator)
            {
                this.simulator = simulator;
            }

            public ImmutableList<Message> SentToBot => simulator.messageHandler.MessagesReceived;

            public ImmutableList<Message> ReceivedFromBot => simulator.messageTransport.MessagesSent;

            public Task SendToBotAsync(Message message) => this.simulator.messageTransport.SendToBotAsync(message);
        }

        private class RunTracker : IAsyncDisposable
        {
            private readonly IAsyncDisposable botRunTracker;
            private readonly BotSimulator simulator;

            public RunTracker(
                IAsyncDisposable botRunTracker,
                BotSimulator simulator
            )
            {
                this.botRunTracker = botRunTracker;
                this.simulator = simulator;
            }
            public async ValueTask DisposeAsync()
            {
                await botRunTracker.DisposeAsync().ConfigureAwait(false);

                Assert.That(this.simulator.messageTransport.WasStarted);
                Assert.That(this.simulator.messageTransport.IsRunning, Is.Not.True);
            }
        }
    }
}
