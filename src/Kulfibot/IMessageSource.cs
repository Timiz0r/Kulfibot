namespace Kulfibot
{
    using System.Threading.Tasks;

    public interface IMessageSource
    {
        Task SubscribeAsync(IBotMessageSink sink);
        Task UnsubscribeAsync(IBotMessageSink sink);
    }
}
