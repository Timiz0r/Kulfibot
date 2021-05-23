using System.Threading.Tasks;

namespace Kulfibot
{
    public class NullBotMessageSink : IBotMessageSink
    {
        public Task MessageReceivedAsync(Message message) => Task.CompletedTask;
    }
}
