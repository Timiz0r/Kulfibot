namespace Kulfibot.Test
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Threading.Tasks;

    internal class SimulatorMessageTransport : IMessageTransport
    {
        private IBotMessageSink? bot;
        public ImmutableList<Message> MessagesSent = ImmutableList<Message>.Empty;

        public bool WasStarted { get; private set; }

        public bool IsRunning { get; private set; }


        public Task SendToBotAsync(Message message) => bot is not null ?
            bot.MessageReceivedAsync(message) :
            throw new InvalidOperationException("Attempting to send a message when bot has not subscribed yet.");

        Task IMessageTransport.SubscribeAsync(IBotMessageSink sink)
        {
            if (bot is not null) throw new InvalidOperationException("Was not expecting another subscriber.");

            //type check for intended type (we're testing Bot, after all), but don't depend on it specifically
            bot = sink as Bot ?? throw new ArgumentOutOfRangeException(
                nameof(sink), $"'{typeof(Bot)}' expected; got '{sink.GetType()}'.");
            IsRunning = true;
            WasStarted = true;

            return Task.CompletedTask;
        }

        Task IMessageTransport.UnsubscribeAsync(IBotMessageSink sink)
        {
            if (bot is null) throw new InvalidOperationException(
                "Somehow unsubscribing when never subscribed in the first place.");

            if (!object.ReferenceEquals(sink, bot)) throw new ArgumentNullException(
                nameof(sink), "Got a different bot than when subscribing.");

            bot = null;
            IsRunning = false;

            return Task.CompletedTask;
        }

        Task IMessageTransport.SendMessagesAsync(IEnumerable<Message> messages)
        {
            _ = ImmutableInterlocked.Update(ref MessagesSent, (messages, newMessage) => messages.AddRange(newMessage), messages);
            return Task.CompletedTask;
        }
    }
}
