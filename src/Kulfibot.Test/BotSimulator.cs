namespace Kulfibot.Test
{
    using System.Collections.Immutable;
    using NUnit.Framework;

    internal class BotSimulator
    {
        public BotSimulator()
        {
            Messages = new MessageLogRecord(this);
        }

        public SimulatorMessageSource MessageSource { get; } = new();

        public SimulatorMessageHandler MessageHandler { get; } = new();

        public MessageLogRecord Messages { get; }

        public BotConfiguration AsBotConfiguration() => new(
            MessageSources: new[] { MessageSource },
            MessageHandlers: new[] { MessageHandler }
        );

        public void AssertRan()
        {
            Assert.That(MessageSource.WasStarted);
            Assert.That(MessageSource.IsRunning, Is.Not.True);
        }

        //the things we do for an ideal interface
        internal class MessageLogRecord
        {
            private readonly BotSimulator simulator;

            public MessageLogRecord(BotSimulator simulator)
            {
                this.simulator = simulator;
            }

            public ImmutableList<Message> SentToBot => simulator.MessageHandler.Messages;

#pragma warning disable CA1822 //rather purposely not supposed to be static. will fill it out later
            public ImmutableList<Message> ReceivedFromBot => ImmutableList<Message>.Empty;
#pragma warning restore CA1822
        }
    }
}
