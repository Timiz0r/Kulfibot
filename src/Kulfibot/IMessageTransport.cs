namespace Kulfibot
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IMessageTransport
    {
        Task SubscribeAsync(IBotMessageSink sink);
        Task UnsubscribeAsync(IBotMessageSink sink);
        Task SendMessagesAsync(IEnumerable<Message> message);
    }
}
