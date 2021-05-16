namespace Kulfibot
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;

    public sealed class Bot : IBotMessageSink
    {
        private int hasStarted; //int for Interlocked
        private readonly BotConfiguration botConfiguration;
        //using interface here because not 100% sure of desired block at time of writing
        private readonly ITargetBlock<Message> receivedMessages;
        private readonly Task messageHandlingCompletion;

        public Bot(BotConfiguration botConfiguration)
        {
            this.botConfiguration = botConfiguration;

            //or could buffer them first
            TransformManyBlock<Message, (Message, IMessageHandler)> messageHandlerSplitter =
                new(m => GetMessageHandlersForMessage(m));
            ActionBlock<(Message, IMessageHandler)> messageHandlerRunner =
                new(pair => HandleMessage(pair.Item1, pair.Item2));
            //or could buffer them first, batch them first, or actionblock them
            //instead, messageHandlerRunner is responsible for sending

            //not unlinking, so dont need idisposable
            _ = messageHandlerSplitter.LinkTo(
                messageHandlerRunner,
                new DataflowLinkOptions() { PropagateCompletion = true });

            receivedMessages = messageHandlerSplitter;
            messageHandlingCompletion = messageHandlerRunner.Completion;
        }

        public async Task<IAsyncDisposable> RunAsync()
        {
            if (receivedMessages.Completion.IsCompleted)
            {
                throw new InvalidOperationException("The bot cannot be started again after having been stopped.");
            }

            if (System.Threading.Interlocked.CompareExchange(ref hasStarted, 1, 0) == 1)
            {
                throw new InvalidOperationException("The bot is already starting or has started.");
            }

            await Task.WhenAll(
                botConfiguration.MessageTransports.Select(source => source.StartAsync(this)));

            return new RunTracker(this);
        }

        Task IBotMessageSink.MessageReceivedAsync(Message message) =>
            receivedMessages.SendAsync(message);

        public IEnumerable<(Message, IMessageHandler)> GetMessageHandlersForMessage(Message message)
        {
            //TODO: up for refactoring
            Dictionary<MessageIntent, IMessageHandler[]> handlersByIntent =
                botConfiguration.MessageHandlers
                    .Select(handler => (handler, intent: handler.DeclareIntent(message)))
                    .GroupBy(item => item.intent, item => item.handler)
                    .ToDictionary(group => group.Key, group => group.ToArray());

            IMessageHandler[] commandHandlers = handlersByIntent.TryGetValue(
                MessageIntent.Command,
                out IMessageHandler[]? handlers)
                    ? handlers
                    : Array.Empty<IMessageHandler>();

            if (commandHandlers.Length >= 2)
            {
                //TODO: log
            }

            List<(Message, IMessageHandler)> results = new();
            if (commandHandlers.Length == 1)
            {
                results.Add((message, commandHandlers[0]));
            }

            if (handlersByIntent.TryGetValue(
                MessageIntent.Passive, out IMessageHandler[]? passiveHandlers))
            {
                results.AddRange(passiveHandlers.Select(handler => (message, handler)));
            }

            return results;
        }

        private async Task HandleMessage(Message message, IMessageHandler handler)
        {
            IEnumerable<Message> messages = Messages.None;
            try
            {
                messages = await handler.HandleAsync(message);
            }
            //rather purposely will catch everything, since failed message handling is nothing to crash over
            //different IMessageHandlers should be considered completely non-conflicting, from a corruption
            //point-of-view. not dissimilar to, for instance, asp.net.
#pragma warning disable CA1031
            catch (System.Exception)
#pragma warning restore CA1031
            {
                //TODO: log
                return;
            }

            await Task.WhenAll(
                this.botConfiguration.MessageTransports.Select(t => t.SendMessagesAsync(messages))
            );
        }

        private class RunTracker : IAsyncDisposable
        {
            private readonly Bot bot;

            public RunTracker(Bot bot)
            {
                this.bot = bot;
            }

            //start is run by the one that instantiated this instance, since we certainly cant call it in the ctor
            public async ValueTask DisposeAsync()
            {
                await Task.WhenAll(
                    this.bot.botConfiguration.MessageTransports.Select(source => source.StoppingAsync()));

                this.bot.receivedMessages.Complete();
                await this.bot.messageHandlingCompletion;

                await Task.WhenAll(
                    this.bot.botConfiguration.MessageTransports.Select(source => source.StopAsync()));
            }
        }
    }
}
