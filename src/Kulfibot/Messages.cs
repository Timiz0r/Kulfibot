namespace Kulfibot
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public static class Messages
    {
        public static readonly IEnumerable<Message> None = Enumerable.Empty<Message>();
        public static readonly Task<IEnumerable<Message>> NoneAsync = Task.FromResult(Enumerable.Empty<Message>());
        //would've called it Single if that wasn't already a type name (float)
        public static IEnumerable<Message> One(Message message) => Enumerable.Repeat(message, 1);
        public static Task<IEnumerable<Message>> OneAsync(Message message) => Task.FromResult(One(message));
    }
}
