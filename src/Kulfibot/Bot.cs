namespace Kulfibot
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;

    public class Bot : IMessageSink
    {
        private readonly BotConfiguration botConfiguration;

        public Bot(BotConfiguration botConfiguration)
        {
            this.botConfiguration = botConfiguration;
        }

        public Task StartAsync() =>
            Task.WhenAll(botConfiguration.MessageSources.Select(source => source.SubscribeAsync(this)));

        public Task StopAsync() =>
            Task.WhenAll(botConfiguration.MessageSources.Select(source => source.UnsubscribeAsync(this)));

        //TODO: will probably use tpl dataflow to basically move this for sure off the thread of the source
        //additionally, we don't want exceptions to propagate to the sender, either.
        public Task MessageReceivedAsync(Message message)
        {
            //TODO: up for refactoring
            Dictionary<MessageIntent, ImmutableArray<IMessageHandler>> handlersByIntent =
                botConfiguration.MessageHandlers
                    .Select(handler => (handler, intent: handler.DeclareIntent(message)))
                    .GroupBy(item => item.intent, item => item.handler)
                    .ToDictionary(group => group.Key, group => group.ToImmutableArray());

            ImmutableArray<IMessageHandler> exclusiveHandlers = handlersByIntent.TryGetValue(
                MessageIntent.Exclusive,
                out ImmutableArray<IMessageHandler> handlers) ?
                    handlers :
                    ImmutableArray<IMessageHandler>.Empty;

            return exclusiveHandlers.Length switch
            {
                > 1 =>
                    throw new InvalidOperationException(
                        $"Multiple handlers want exclusive handling of the message: " +
                        string.Join(", ", exclusiveHandlers.Select(handler => handler.GetType().Name))),
                1 => exclusiveHandlers[0].HandleAsync(message),
                _ => handlersByIntent.TryGetValue(
                    MessageIntent.Passive, out ImmutableArray<IMessageHandler> passiveHandlers) ?
                        Task.WhenAll(passiveHandlers.Select(handler => handler.HandleAsync(message))) :
                        Task.CompletedTask
            };
        }
    }
}
