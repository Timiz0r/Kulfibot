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

        public BotConfiguration AsBotConfiguration(BotConfiguration basis) => new(
            MessageTransports: basis.MessageTransports.Concat(new[] { messageTransport }).ToArray(),
            MessageHandlers: basis.MessageHandlers.Concat(new[] { messageHandler }).ToArray()
        );

        public BotConfiguration AsBotConfiguration() => new(
            MessageTransports: new[] { messageTransport },
            MessageHandlers: new[] { messageHandler }
        );

        public void AssertRan()
        {
            Assert.That(messageTransport.WasStarted);
            Assert.That(messageTransport.IsRunning, Is.Not.True);
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
    }
}
