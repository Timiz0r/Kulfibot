namespace Kulfibot
{
    using System.Threading.Tasks;

    public interface IMessageHandler
    {
        MessageIntent DeclareIntent(Message message);
        Task HandleAsync(Message message);
    }
}
