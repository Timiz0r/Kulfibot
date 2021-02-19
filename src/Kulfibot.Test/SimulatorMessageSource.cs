namespace Kulfibot.Test
{
    using System;
    using System.Threading.Tasks;

    internal class SimulatorMessageSource : IMessageSource
    {
        private Bot? bot;

        public bool WasStarted { get; private set; }
        public bool IsRunning { get; private set; }

        public Task SendAsync(Message message) => bot is null ?
            throw new InvalidOperationException("Attempting to send a message when bot has not subscribed yet.") :
            bot.MessageReceivedAsync(message);

        Task IMessageSource.SubscribeAsync(IMessageSink sink)
        {
            if (bot is not null) throw new InvalidOperationException("Was not expecting another subscriber.");

            bot = sink is Bot newBot ?
                newBot :
                throw new ArgumentOutOfRangeException(
                    nameof(sink), $"'{typeof(Bot)}' expected; got '{sink.GetType()}'.");
            IsRunning = true;
            WasStarted = true;

            return Task.CompletedTask;
        }

        Task IMessageSource.UnsubscribeAsync(IMessageSink sink)
        {
            if (bot is null) throw new InvalidOperationException(
                "Somehow unsubscribing when never subscribed in the first place.");

            if (!object.ReferenceEquals(sink, bot)) throw new ArgumentNullException(
                nameof(sink), $"'{typeof(Bot)}' expected; got '{sink.GetType()}'.");

            bot = null;
            IsRunning = false;

            return Task.CompletedTask;
        }
    }
}
