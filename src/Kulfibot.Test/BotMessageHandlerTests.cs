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
            IMessageHandler passiveHandlerA = BasicHandler.CreatePassive(_ => true, _ => passiveResponseA);
            IMessageHandler passiveHandlerB = BasicHandler.CreatePassive(_ => true, _ => passiveResponseB);
            BotConfiguration config = new(
                ImmutableList<IMessageTransport>.Empty,
                ImmutableList.Create(passiveHandlerA, passiveHandlerB));
            BotSimulator simulator = new(config);

            await using (await simulator.RunBotAsync())
            {
                await simulator.Messages.SendToBotAsync(new());
            }

            Assert.That(simulator.Messages.ReceivedFromBot, Has.Exactly(2).Items);
            Assert.That(simulator.Messages.ReceivedFromBot, Has.Member(passiveResponseA));
            Assert.That(simulator.Messages.ReceivedFromBot, Has.Member(passiveResponseB));
        }

        [Test]
        public async Task Bot_RunsNoCommandHandlers_WhenMultipleCommandHandlers()
        {
            IMessageHandler commandHandlerA = BasicHandler.CreateCommand(_ => true, _ => new Message());
            IMessageHandler commandHandlerB = BasicHandler.CreateCommand(_ => true, _ => new Message());
            BotConfiguration config = new(
                ImmutableList<IMessageTransport>.Empty,
                ImmutableList.Create(commandHandlerA, commandHandlerB));
            BotSimulator simulator = new(config);

            await using (await simulator.RunBotAsync())
            {
                await simulator.Messages.SendToBotAsync(new());
            }

            Assert.That(simulator.Messages.ReceivedFromBot, Is.Empty);
        }

        [Test]
        public async Task Bot_RunsPassiveHandlers_WhenMultipleCommandHandlers()
        {
            Message passiveResponseA = new();
            Message passiveResponseB = new();
            IMessageHandler commandHandlerA = BasicHandler.CreateCommand(_ => true, _ => new Message());
            IMessageHandler commandHandlerB = BasicHandler.CreateCommand(_ => true, _ => new Message());
            IMessageHandler passiveHandlerA = BasicHandler.CreatePassive(_ => true, _ => passiveResponseA);
            IMessageHandler passiveHandlerB = BasicHandler.CreatePassive(_ => true, _ => passiveResponseB);
            BotConfiguration config = new(
                ImmutableList<IMessageTransport>.Empty,
                ImmutableList.Create(commandHandlerA, commandHandlerB, passiveHandlerA, passiveHandlerB));
            BotSimulator simulator = new(config);

            await using (await simulator.RunBotAsync())
            {
                await simulator.Messages.SendToBotAsync(new());
            }

            Assert.That(simulator.Messages.ReceivedFromBot, Has.Exactly(2).Items);
            Assert.That(simulator.Messages.ReceivedFromBot, Has.Member(passiveResponseA));
            Assert.That(simulator.Messages.ReceivedFromBot, Has.Member(passiveResponseB));
        }

        [Test]
        public async Task Bot_GivesMessagesToHandlers_WhenBothPassiveAndCommand()
        {
            //the simulator has a passive handler already, but this makes the test look more correct
            Message passiveResponse = new();
            IMessageHandler passiveHandler = BasicHandler.CreatePassive(_ => true, _ => passiveResponse);
            Message commandResponse = new();
            IMessageHandler commandHandler = BasicHandler.CreateCommand(_ => true, _ => commandResponse);
            BotConfiguration config = new(
                ImmutableList<IMessageTransport>.Empty,
                ImmutableList.Create(passiveHandler, commandHandler));
            BotSimulator simulator = new(config);

            await using (await simulator.RunBotAsync())
            {
                await simulator.Messages.SendToBotAsync(new());
            }

            Assert.That(simulator.Messages.ReceivedFromBot, Has.Exactly(2).Items);
            Assert.That(simulator.Messages.ReceivedFromBot, Has.Member(passiveResponse));
            Assert.That(simulator.Messages.ReceivedFromBot, Has.Member(commandResponse));
        }

        [Test]
        public async Task Bot_DoesNothingInParticular_WhenHandlerThrows()
        {
            //the simulator has a passive handler already, but this makes the test look more correct
            IMessageHandler handler = BasicHandler.CreatePassive(
                _ => true,
                new Func<Message, Message>(_ => throw new InvalidOperationException()));
            BotConfiguration config = new(
                ImmutableList<IMessageTransport>.Empty,
                ImmutableList.Create(handler));
            BotSimulator simulator = new(config);

            await using (await simulator.RunBotAsync())
            {
                await simulator.Messages.SendToBotAsync(new());
            }
        }

        [Test]
        public async Task Bot_SendsResponses_WhenSomeHandlersFailButOthersDont()
        {
            Message commandResponse = new();
            Message passiveResponse = new();
            IMessageHandler commandHandlerSucceed = BasicHandler.CreateCommand(_ => true, _ => commandResponse);
            IMessageHandler passiveHandlerSucceed = BasicHandler.CreatePassive(_ => true, _ => passiveResponse);
            IMessageHandler passiveHandlerFail = BasicHandler.CreatePassive(
                _ => true,
                new Func<Message, Message>(_ => throw new InvalidOperationException()));
            BotConfiguration config = new(
                ImmutableList<IMessageTransport>.Empty,
                ImmutableList.Create(
                    commandHandlerSucceed,
                    passiveHandlerSucceed,
                    passiveHandlerFail));
            BotSimulator simulator = new(config);

            await using (await simulator.RunBotAsync())
            {
                await simulator.Messages.SendToBotAsync(new());
            }

            Assert.That(simulator.Messages.ReceivedFromBot, Has.Exactly(2).Items);
            Assert.That(simulator.Messages.ReceivedFromBot, Has.Member(commandResponse));
            Assert.That(simulator.Messages.ReceivedFromBot, Has.Member(passiveResponse));
        }
    }
}
