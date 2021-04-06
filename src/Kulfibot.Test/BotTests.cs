namespace Kulfibot.Test
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class Tests
    {
        [Test]
        public async Task Bot_ReceivesMessages_WhenSentFromSource()
        {
            BotSimulator simulator = new();
            Bot bot = new(simulator.AsBotConfiguration());

            Message message = new();
            await bot.StartAsync().ConfigureAwait(false);
            await simulator.MessageSource.SendAsync(message).ConfigureAwait(false);
            await bot.StopAsync().ConfigureAwait(false);

            simulator.AssertRan();
            Assert.That(simulator.Messages.SentToBot, Has.Exactly(1).Items);
            Assert.That(simulator.Messages.SentToBot, Has.Member(message));
        }
    }
}
