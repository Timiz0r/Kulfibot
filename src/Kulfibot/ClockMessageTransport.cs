namespace Kulfibot
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NodaTime;
    using NodaTime.Extensions;

    public class ClockMessageTransport : IMessageTransport
    {
        private Task processingLoop = Task.CompletedTask;
        private IBotMessageSink bot = new NullBotMessageSink();
        private bool stopping;

        public Task SendMessagesAsync(IEnumerable<Message> message) => Task.CompletedTask;
        public Task StartAsync(IBotMessageSink sink)
        {
            this.bot = sink;
            this.processingLoop = ProcessingLoop();
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            this.stopping = true;
            return this.processingLoop;
        }

        public Task StoppingAsync()
        {
            this.stopping = true;
            return Task.CompletedTask;
        }

        private async Task ProcessingLoop()
        {
            ZonedDateTime next = SystemClock.Instance.GetCurrentInstant()
                .InUtc()
                .PlusSeconds(1)
                .LocalDateTime
                .With(TimeAdjusters.TruncateToSecond)
                .InUtc();

            while (!this.stopping)
            {
                ZonedDateTime now = SystemClock.Instance.GetCurrentInstant().InUtc();

                Duration delayDuration = next - now;
                //could be behind for some reason
                if (delayDuration > Duration.Zero)
                {
                    await Task.Delay(delayDuration.ToTimeSpan());
                }

                ClockMessage message = new(next.ToInstant());
                await this.bot.MessageReceivedAsync(message);

                //if we somehow get behind, we'll try to catch up
                //versus always basing next based on the current instant, which would skip instants if behind.
                next = next.PlusSeconds(1);
            }
        }
    }
}
