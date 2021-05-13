namespace Kulfibot
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;

    public class Bot : IBotMessageSink
    {
        private readonly BotConfiguration botConfiguration;

        public Bot(BotConfiguration botConfiguration)
        {
            this.botConfiguration = botConfiguration;
        }

        public Task StartAsync() =>
            Task.WhenAll(botConfiguration.MessageTransports.Select(source => source.SubscribeAsync(this)));

        public Task StopAsync() =>
            Task.WhenAll(botConfiguration.MessageTransports.Select(source => source.UnsubscribeAsync(this)));

        //TODO: will probably use tpl dataflow to basically move this for sure off the thread of the source
        //additionally, we don't want exceptions to propagate to the sender, either.
        public async Task MessageReceivedAsync(Message message)
        {
            //TODO: up for refactoring
            Dictionary<MessageIntent, IMessageHandler[]> handlersByIntent =
                botConfiguration.MessageHandlers
                    .Select(handler => (handler, intent: handler.DeclareIntent(message)))
                    .GroupBy(item => item.intent, item => item.handler)
                    .ToDictionary(group => group.Key, group => group.ToArray());

            IMessageHandler[] exclusiveHandlers = handlersByIntent.TryGetValue(
                MessageIntent.Exclusive,
                out IMessageHandler[]? handlers) ?
                    handlers! :
                    Array.Empty<IMessageHandler>();

            IEnumerable<Task<IEnumerable<Message>>> responseTasks = exclusiveHandlers.Length switch
            {
                > 1 =>
                    throw new InvalidOperationException(
                        $"Multiple handlers want exclusive handling of the message: " +
                        string.Join(", ", exclusiveHandlers.Select(handler => handler.GetType().Name))),
                1 => exclusiveHandlers[0..1].Select(handler => handler.HandleAsync(message)),
                _ => handlersByIntent.TryGetValue(
                    MessageIntent.Passive, out IMessageHandler[]? passiveHandlers) ?
                        passiveHandlers!.Select(handler => handler.HandleAsync(message)) :
                        new[] { Messages.NoneAsync }
            };

            IEnumerable<Message> messages =
                (await Task.WhenAll(responseTasks).ConfigureAwait(false)).SelectMany(m => m);

            await Task.WhenAll(
                botConfiguration.MessageTransports.Select(mt => mt.SendMessagesAsync(messages))).ConfigureAwait(false);
        }
    }
}
