namespace Kulfibot
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IMessageHandler
    {
        MessageIntent DeclareIntent(Message message);
        Task<IEnumerable<Message>> HandleAsync(Message message);
    }
}
