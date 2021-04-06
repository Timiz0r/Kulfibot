namespace Kulfibot
{
    using System.Threading.Tasks;

    public interface IBotMessageSink
    {
        Task MessageReceivedAsync(Message message);
        //TODO: other lifetime events of the source, like disconnection and whatnot
    }
}
