namespace Kulfibot.Test
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Threading;
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
            IMessageHandler respondingHandler = BasicHandler.CreateCommand(_ => true, _ => response);
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

            Assert.That(async () => _ = await bot.RunAsync(), Throws.InvalidOperationException);
        }

        [Test]
        public async Task Bot_WaitsForMessageHandling_WhenStoppedBeforeItIsDone()
        {
            IMessageHandler respondingHandler = BasicHandler.CreateCommand(
                _ => true,
                async _ =>
                {
                    await Task.Delay(1000);
                    return new Message();
                });
            BotConfiguration config = new(
                ImmutableList<IMessageTransport>.Empty,
                ImmutableList.Create(respondingHandler));
            BotSimulator simulator = new(config);

            await using (await simulator.RunBotAsync())
            {
                await simulator.Messages.SendToBotAsync(new());
            }

            Assert.That(simulator.Messages.ReceivedFromBot, Has.Exactly(1).Items);
        }

        [Test]
        public async Task Bot_Throws_WhenStartingASecondTime()
        {
            BotSimulator simulator = new();
            Bot bot = new(simulator.AsBotConfiguration());

            await using (IAsyncDisposable tracker = await bot.RunAsync())
            {
            }

            Assert.That(async () => _ = await bot.RunAsync(), Throws.InvalidOperationException);
        }
    }
}
