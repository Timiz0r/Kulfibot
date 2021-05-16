namespace Kulfibot.Test
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Threading.Tasks;

    internal sealed class SimulatorMessageTransport : IMessageTransport
    {
        private IBotMessageSink? bot;
        public ImmutableList<Message> MessagesSent = ImmutableList<Message>.Empty;

        public bool WasStarted { get; private set; }

        public bool IsRunning { get; private set; }

        public bool IsStopping { get; private set; }


        public Task SendToBotAsync(Message message) =>
            this.bot is null
                ? throw new InvalidOperationException(
                    "Attempting to send a message when bot has not started the transport yet.")
                : this.IsStopping
                    ? Task.CompletedTask
                    : this.bot.MessageReceivedAsync(message);

        Task IMessageTransport.StartAsync(IBotMessageSink sink)
        {
            if (this.bot is not null) throw new InvalidOperationException("Was not expecting another start.");

            //type check for intended type (we're testing Bot, after all), but don't depend on it specifically
            this.bot = sink as Bot ?? throw new ArgumentOutOfRangeException(
                nameof(sink), $"'{typeof(Bot)}' expected; got '{sink.GetType()}'.");
            this.IsRunning = true;
            this.WasStarted = true;

            return Task.CompletedTask;
        }

        public Task StoppingAsync()
        {
            this.IsStopping = true;

            return Task.CompletedTask;
        }

        Task IMessageTransport.StopAsync()
        {
            if (bot is null) throw new InvalidOperationException(
                "Somehow stopping when never started in the first place.");

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
