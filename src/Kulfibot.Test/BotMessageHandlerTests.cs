namespace Kulfibot.Test
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public sealed class BotMessageHandlerTests
    {
        [Test]
        public async Task Bot_RunsMultiplePassiveHandlers_WhenMultiplePassiveHandlers()
        {
            Message passiveResponseA = new();
            Message passiveResponseB = new();
            PassiveHandler passiveHandlerA = new(_ => true, _ => passiveResponseA);
            PassiveHandler passiveHandlerB = new(_ => true, _ => passiveResponseB);
            BotConfiguration config = new(
                ImmutableList<IMessageTransport>.Empty,
                ImmutableList.Create<IMessageHandler>(passiveHandlerA, passiveHandlerB));
            BotSimulator simulator = new(config);

            await using (await simulator.RunBotAsync())
            {
                await simulator.Messages.SendToBotAsync(new());

                Assert.That(simulator.Messages.ReceivedFromBot, Has.Exactly(2).Items);
                Assert.That(simulator.Messages.ReceivedFromBot, Has.Member(passiveResponseA));
                Assert.That(simulator.Messages.ReceivedFromBot, Has.Member(passiveResponseB));
            }
        }

        [Test]
        public async Task Bot_RunsNoCommandHandlers_WhenMultipleCommandHandlers()
        {
            CommandHandler commandHandlerA = new(_ => true, _ => new Message());
            CommandHandler commandHandlerB = new(_ => true, _ => new Message());
            BotConfiguration config = new(
                ImmutableList<IMessageTransport>.Empty,
                ImmutableList.Create<IMessageHandler>(commandHandlerA, commandHandlerB));
            BotSimulator simulator = new(config);

            await using (await simulator.RunBotAsync())
            {
                await simulator.Messages.SendToBotAsync(new());

                Assert.That(simulator.Messages.ReceivedFromBot, Is.Empty);
            }
        }

        [Test]
        public async Task Bot_RunsPassiveHandlers_WhenMultipleCommandHandlers()
        {
            Message passiveResponseA = new();
            Message passiveResponseB = new();
            CommandHandler commandHandlerA = new(_ => true, _ => new Message());
            CommandHandler commandHandlerB = new(_ => true, _ => new Message());
            PassiveHandler passiveHandlerA = new(_ => true, _ => passiveResponseA);
            PassiveHandler passiveHandlerB = new(_ => true, _ => passiveResponseB);
            BotConfiguration config = new(
                ImmutableList<IMessageTransport>.Empty,
                ImmutableList.Create<IMessageHandler>(commandHandlerA, commandHandlerB, passiveHandlerA, passiveHandlerB));
            BotSimulator simulator = new(config);

            await using (await simulator.RunBotAsync())
            {
                await simulator.Messages.SendToBotAsync(new());

                Assert.That(simulator.Messages.ReceivedFromBot, Has.Exactly(2).Items);
                Assert.That(simulator.Messages.ReceivedFromBot, Has.Member(passiveResponseA));
                Assert.That(simulator.Messages.ReceivedFromBot, Has.Member(passiveResponseB));
            }
        }

        [Test]
        public async Task Bot_GivesMessagesToHandlers_WhenBothPassiveAndCommand()
        {
            //the simulator has a passive handler already, but this makes the test look more correct
            Message passiveResponse = new();
            PassiveHandler passiveHandler = new(_ => true, _ => passiveResponse);
            Message commandResponse = new();
            CommandHandler commandHandler = new(_ => true, _ => commandResponse);
            BotConfiguration config = new(
                ImmutableList<IMessageTransport>.Empty,
                ImmutableList.Create<IMessageHandler>(passiveHandler, commandHandler));
            BotSimulator simulator = new(config);

            await using (await simulator.RunBotAsync())
            {
                await simulator.Messages.SendToBotAsync(new());

                Assert.That(simulator.Messages.ReceivedFromBot, Has.Exactly(2).Items);
                Assert.That(simulator.Messages.ReceivedFromBot, Has.Member(passiveResponse));
                Assert.That(simulator.Messages.ReceivedFromBot, Has.Member(commandResponse));
            }
        }

        //TODO: if making a delegate-based handler that can replace these, do that
        private class DelegateBasedHandler : IMessageHandler
        {
            private readonly MessageIntent intent;
            private readonly Func<Message, bool> intentPredicate;
            private readonly Func<Message, IEnumerable<Message>> handler;

            public DelegateBasedHandler(
                MessageIntent intent,
                Func<Message, bool> intentPredicate,
                Func<Message, IEnumerable<Message>> handler)
            {
                this.intent = intent;
                this.intentPredicate = intentPredicate;
                this.handler = handler;
            }

            public DelegateBasedHandler(
                MessageIntent intent,
                Func<Message, bool> intentPredicate,
                Func<Message, Message> handler) : this(
                    intent,
                    intentPredicate,
                    new Func<Message, IEnumerable<Message>>(m => new[] { handler(m) }))
            {
            }

            public MessageIntent DeclareIntent(Message message) =>
                intentPredicate(message) ? intent : MessageIntent.Ignore;

            public Task<IEnumerable<Message>> HandleAsync(Message message) =>
                intentPredicate(message) ? Task.FromResult(handler(message)) : Messages.NoneAsync;
        }

        private sealed class PassiveHandler : DelegateBasedHandler
        {
            public PassiveHandler(
                Func<Message, bool> intentPredicate,
                Func<Message, IEnumerable<Message>> handler) : base(MessageIntent.Passive, intentPredicate, handler)
            {
            }

            public PassiveHandler(
                Func<Message, bool> intentPredicate,
                Func<Message, Message> handler) : base(MessageIntent.Passive, intentPredicate, handler)
            {
            }
        }

        private sealed class CommandHandler : DelegateBasedHandler
        {
            public CommandHandler(
                Func<Message, bool> intentPredicate,
                Func<Message, IEnumerable<Message>> handler) : base(MessageIntent.Command, intentPredicate, handler)
            {
            }

            public CommandHandler(
                Func<Message, bool> intentPredicate,
                Func<Message, Message> handler) : base(MessageIntent.Command, intentPredicate, handler)
            {
            }
        }
    }
}
