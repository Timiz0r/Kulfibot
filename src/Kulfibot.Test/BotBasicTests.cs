namespace Kulfibot.Test
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public sealed class BotBasicTests
    {
        [Test]
        public async Task Bot_GivesMessagesToHandlers_WhenSentFromSource()
        {
            BotSimulator simulator = new();

            Message message = new();
            await using (await simulator.RunBotAsync())
            {
                await simulator.Messages.SendToBotAsync(message);
            }

            Assert.That(simulator.Messages.SentToBot, Has.Exactly(1).Items);
            Assert.That(simulator.Messages.SentToBot, Has.Member(message));
        }

        [Test]
        public async Task Bot_SendsMessages_WhenMessageHandlersRespond()
        {
            Message response = new();
            CommandHandler respondingHandler = new(_ => true, _ => response);
            BotConfiguration config = new(
                ImmutableList<IMessageTransport>.Empty,
                ImmutableList.Create<IMessageHandler>(respondingHandler));
            BotSimulator simulator = new(config);

            await using (await simulator.RunBotAsync())
            {
                await simulator.Messages.SendToBotAsync(new());
            }

            Assert.That(simulator.Messages.ReceivedFromBot, Has.Exactly(1).Items);
            Assert.That(simulator.Messages.ReceivedFromBot, Has.Member(response));
        }

        [Test]
        public async Task Bot_Throws_WhenRunMultipleTimesBeforeStopping()
        {
            BotSimulator simulator = new();
            Bot bot = new(simulator.AsBotConfiguration());

            //didn't dispose, so not stopped
            _ = await bot.RunAsync();

            Assert.That(async () => _ = await bot.RunAsync(), Throws.Exception);
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
