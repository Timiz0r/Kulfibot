namespace Kulfibot.Test
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Threading.Tasks;

    internal sealed class SimulatorMessageHandler : IMessageHandler
    {
        public ImmutableList<Message> MessagesReceived = ImmutableList<Message>.Empty;

        public MessageIntent DeclareIntent(Message message) => MessageIntent.Passive;

        public Task<IEnumerable<Message>> HandleAsync(Message message)
        {
            _ = ImmutableInterlocked.Update(ref MessagesReceived, (messages, newMessage) => messages.Add(newMessage), message);
            return Messages.NoneAsync;
        }
    }
}
