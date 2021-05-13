namespace Kulfibot.Test
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class BotTests
    {
        [Test]
        public async Task Bot_GivesMessagesToHandlers_WhenSentFromSource()
        {
            BotSimulator simulator = new();

            Message message = new();
            await using (await simulator.RunBotAsync().ConfigureAwait(false))
            {
                await simulator.Messages.SendToBotAsync(message).ConfigureAwait(false);
            }

            Assert.That(simulator.Messages.SentToBot, Has.Exactly(1).Items);
            Assert.That(simulator.Messages.SentToBot, Has.Member(message));
        }

        [Test]
        public async Task Bot_Throws_WhenMultipleExclusiveHandlers()
        {
            ExclusiveHandler a = new(_ => true, _ => Messages.None);
            ExclusiveHandler b = new(_ => true, _ => Messages.None);
            BotConfiguration config = new(
                Array.Empty<IMessageTransport>(),
                new[] { a, b });
            BotSimulator simulator = new(config);

            await using (await simulator.RunBotAsync().ConfigureAwait(false))
            {
                //TODO: dont really want to propagate exceptions to the message sources, so will come up with something later
                //probably some sort of IErrorHandler, or IMessageHandlers get errors they're related to,
                //  or IMessageHandlers can get specifically informed of conflicts
                Assert.That(
                    async () => await simulator.Messages.SendToBotAsync(new()).ConfigureAwait(false),
                    Throws.Exception.Message.Contains("Multiple handlers want exclusive handling of the message"));
            }
        }

        [Test]
        public async Task Bot_SendsMessages_WhenMessageHandlersRespond()
        {
            Message response = new();
            ExclusiveHandler respondingHandler = new(_ => true, _ => response);
            BotConfiguration config = new(
                Array.Empty<IMessageTransport>(),
                new[] { respondingHandler });
            BotSimulator simulator = new(config);

            await using (await simulator.RunBotAsync().ConfigureAwait(false))
            {
                await simulator.Messages.SendToBotAsync(new()).ConfigureAwait(false);
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
            _ = await bot.RunAsync().ConfigureAwait(false);

            Assert.That(async () => _ = await bot.RunAsync().ConfigureAwait(false), Throws.Exception);
        }

        //TODO: if making a delegate-based handler that can replace these, do that
        private class ExclusiveHandler : IMessageHandler
        {
            private readonly Func<Message, bool> intentPredicate;
            private readonly Func<Message, IEnumerable<Message>> handler;

            public ExclusiveHandler(
                Func<Message, bool> intentPredicate,
                Func<Message, IEnumerable<Message>> handler)
            {
                this.intentPredicate = intentPredicate;
                this.handler = handler;
            }

            public ExclusiveHandler(
                Func<Message, bool> intentPredicate,
                Func<Message, Message> handler)
            {
                this.intentPredicate = intentPredicate;
                this.handler = new(m => new[] { handler(m) });
            }

            public MessageIntent DeclareIntent(Message message) =>
                intentPredicate(message) ? MessageIntent.Exclusive : MessageIntent.Ignore;

            public Task<IEnumerable<Message>> HandleAsync(Message message) =>
                intentPredicate(message) ? Task.FromResult(handler(message)) : Messages.NoneAsync;
        }
    }
}
