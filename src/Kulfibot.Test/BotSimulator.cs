namespace Kulfibot.Test
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal class BotSimulator
    {
        private readonly SimulatorMessageSource messageSource = new();
        private readonly SimulatorMessageHandler messageHandler = new();

        public BotSimulator()
        {
            Messages = new MessageRecord(this);
        }

        public MessageRecord Messages { get; }

        public BotConfiguration AsBotConfiguration(BotConfiguration basis) => new(
            MessageSources: basis.MessageSources.Concat(new[] { messageSource }).ToArray(),
            MessageHandlers: basis.MessageHandlers.Concat(new[] { messageHandler }).ToArray()
        );

        public BotConfiguration AsBotConfiguration() => new(
            MessageSources: new[] { messageSource },
            MessageHandlers: new[] { messageHandler }
        );

        public void AssertRan()
        {
            Assert.That(messageSource.WasStarted);
            Assert.That(messageSource.IsRunning, Is.Not.True);
        }

        //the things we do for an ideal interface
        internal class MessageRecord
        {
            private readonly BotSimulator simulator;

            public MessageRecord(BotSimulator simulator)
            {
                this.simulator = simulator;
            }

            public ImmutableList<Message> SentToBot => simulator.messageHandler.Messages;

#pragma warning disable CA1822 //rather purposely not supposed to be static. will fill it out later
            public ImmutableList<Message> ReceivedFromBot => ImmutableList<Message>.Empty;
#pragma warning restore CA1822

            public Task SendAsync(Message message) => this.simulator.messageSource.SendAsync(message);
        }
    }
}
