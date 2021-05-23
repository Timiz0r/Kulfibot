namespace Kulfibot
{
    using System.Threading.Tasks;

    public class NullBotMessageSink : IBotMessageSink
    {
        public Task MessageReceivedAsync(Message message) => Task.CompletedTask;
    }
}
