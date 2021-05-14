namespace Kulfibot.Test
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal sealed class BotSimulator
    {
        private readonly SimulatorMessageTransport messageTransport = new();
        private readonly SimulatorMessageHandler messageHandler = new();
        private readonly BotConfiguration basisConfiguration;

        public BotSimulator() : this(new(
            ImmutableList<IMessageTransport>.Empty,
            ImmutableList<IMessageHandler>.Empty
        ))
        {
        }

        public BotSimulator(BotConfiguration basisConfiguration)
        {
            this.Messages = new MessageRecord(this);
            this.basisConfiguration = basisConfiguration;
        }

        public MessageRecord Messages { get; }

        public BotConfiguration AsBotConfiguration() => basisConfiguration with
        {
            MessageTransports = basisConfiguration.MessageTransports.Add(messageTransport),
            MessageHandlers = basisConfiguration.MessageHandlers.Add(messageHandler)
        };

        public Task<IAsyncDisposable> RunBotAsync()
        {
            BotConfiguration configuration = AsBotConfiguration();

            Bot bot = new(configuration);
            return bot.RunAsync();
        }

        //the things we do for an ideal interface
        internal sealed class MessageRecord
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

        private sealed class RunTracker : IAsyncDisposable
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
