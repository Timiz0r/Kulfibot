namespace Kulfibot.Test
{
    using System.Collections.Immutable;
    using System.Threading.Tasks;

    internal class SimulatorMessageHandler : IMessageHandler
    {
        public ImmutableList<Message> Messages = ImmutableList<Message>.Empty;

        public MessageIntent DeclareIntent(Message message) => MessageIntent.Passive;
        public Task HandleAsync(Message message)
        {
            _ = ImmutableInterlocked.Update(ref Messages, (messages, newMessage) => messages.Add(newMessage), message);
            return Task.CompletedTask;
        }
    }
}
