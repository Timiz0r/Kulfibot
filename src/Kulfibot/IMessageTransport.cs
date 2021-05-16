namespace Kulfibot
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IMessageTransport
    {
        Task StartAsync(IBotMessageSink sink);
        //indicates that no new messages should be provided to the bot, but that the bot might still send messages
        Task StoppingAsync();
        Task StopAsync();
        //since some transports may be capable of sending multiple messages at a time, we'll allow that here
        //if a transport can't do that, then it should be fully capable of serializing multiple messages
        Task SendMessagesAsync(IEnumerable<Message> message);
    }
}
