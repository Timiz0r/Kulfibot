namespace Kulfibot
{
    using System.Threading.Tasks;

    public interface IMessageSource
    {
        Task SubscribeAsync(IMessageSink sink);
        Task UnsubscribeAsync(IMessageSink sink);
    }
}
