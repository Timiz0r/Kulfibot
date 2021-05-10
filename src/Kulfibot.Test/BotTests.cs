namespace Kulfibot.Test
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class BotTests
    {
        [Test]
        public async Task Bot_GivesMessagesToHandlers_WhenSentFromSource()
        {
            BotSimulator simulator = new();
            Bot bot = new(simulator.AsBotConfiguration());

            Message message = new();
            await bot.StartAsync().ConfigureAwait(false);
            await simulator.Messages.SendAsync(message).ConfigureAwait(false);
            await bot.StopAsync().ConfigureAwait(false);

            simulator.AssertRan();
            Assert.That(simulator.Messages.SentToBot, Has.Exactly(1).Items);
            Assert.That(simulator.Messages.SentToBot, Has.Member(message));
        }

        [Test]
        public void Bot_Throws_WhenMultipleExclusiveHandlers()
        {
            BotSimulator simulator = new();
            ExclusiveHandler a = new(_ => true, _ => Task.CompletedTask);
            ExclusiveHandler b = new(_ => true, _ => Task.CompletedTask);

            BotConfiguration config = new(
                Array.Empty<IMessageSource>(),
                new IMessageHandler[] { a, b });
            Bot bot = new(simulator.AsBotConfiguration(config));

            //TODO: dont really want to propagate exceptions to the message sources, so will come up with something later
            //probably some sort of IErrorHandler, or IMessageHandlers get errors they're related to,
            //  or IMessageHandlers can get specifically informed of conflicts
            Assert.That(async () => await simulator.Messages.SendAsync(new()).ConfigureAwait(false), Throws.Exception);
        }

        //TODO: if making a delegate-based handler that can replace these, do that
        private class ExclusiveHandler : IMessageHandler
        {
            private readonly Func<Message, bool> intentPredicate;
            private readonly Func<Message, Task> handler;

            public ExclusiveHandler(Func<Message, bool> intentPredicate, Func<Message, Task> handler)
            {
                this.intentPredicate = intentPredicate;
                this.handler = handler;
            }

            public MessageIntent DeclareIntent(Message message) => intentPredicate(message) ? MessageIntent.Exclusive : MessageIntent.Ignore;
            public Task HandleAsync(Message message) => handler(message);
        }
    }
}
