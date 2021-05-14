namespace Kulfibot
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public sealed class Bot : IBotMessageSink
    {
        private readonly BotConfiguration botConfiguration;

        public Bot(BotConfiguration botConfiguration)
        {
            this.botConfiguration = botConfiguration;
        }

        public async Task<IAsyncDisposable> RunAsync()
        {
            await Task.WhenAll(
                botConfiguration.MessageTransports.Select(source => source.SubscribeAsync(this))).ConfigureAwait(false);

            return new RunTracker(this);
        }

        //TODO: will probably use tpl dataflow to basically move this for sure off the thread of the source
        //additionally, we don't want exceptions to propagate to the sender, either.
        //though, since we now have response messages, tpl dataflow becomes a bit harder, since we need to wait for the
        //  results of the handlers going thru a pipeline. that or create pipelines ad-hoc, which is kinda pointless.
        //given that, an alternate option is to abandon the "all outgoing messages are responses" design.
        //or maybe tpl dataflow is used to queue up messages, and the logic in MessageReceivedAsync is left as-is/moved.
        async Task IBotMessageSink.MessageReceivedAsync(Message message)
        {
            //TODO: up for refactoring
            Dictionary<MessageIntent, IMessageHandler[]> handlersByIntent =
                botConfiguration.MessageHandlers
                    .Select(handler => (handler, intent: handler.DeclareIntent(message)))
                    .GroupBy(item => item.intent, item => item.handler)
                    .ToDictionary(group => group.Key, group => group.ToArray());

            IMessageHandler[] commandHandlers = handlersByIntent.TryGetValue(
                MessageIntent.Command,
                out IMessageHandler[]? handlers) ?
                    handlers :
                    Array.Empty<IMessageHandler>();

            if (commandHandlers.Length >= 2)
            {
                throw new InvalidOperationException(
                    "Multiple handlers want command handling of the message: " +
                    string.Join(", ", commandHandlers.Select(handler => handler.GetType().Name)));
            }

            List<Task<IEnumerable<Message>>> handlerTasks = new();
            if (commandHandlers.Length == 1)
            {
                handlerTasks.Add(commandHandlers[0].HandleAsync(message));
            }

            if (handlersByIntent.TryGetValue(
                MessageIntent.Passive, out IMessageHandler[]? passiveHandlers))
            {
                handlerTasks.AddRange(passiveHandlers.Select(handler => handler.HandleAsync(message)));
            }

            IEnumerable<Message> messages =
                (await Task.WhenAll(handlerTasks).ConfigureAwait(false)).SelectMany(m => m);

            await Task.WhenAll(
                botConfiguration.MessageTransports.Select(mt => mt.SendMessagesAsync(messages))).ConfigureAwait(false);
        }

        private class RunTracker : IAsyncDisposable
        {
            private readonly Bot bot;

            public RunTracker(Bot bot)
            {
                this.bot = bot;
            }

            //start is run by the one that instantiated this instance, since we certainly cant call it in the ctor
            public ValueTask DisposeAsync() => new(
                Task.WhenAll(
                    this.bot.botConfiguration.MessageTransports.Select(source => source.UnsubscribeAsync(this.bot))));
        }
    }
}
