namespace Kulfibot
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public static class Messages
    {
        public static readonly IEnumerable<Message> None = Enumerable.Empty<Message>();
        public static readonly Task<IEnumerable<Message>> NoneAsync = Task.FromResult(Enumerable.Empty<Message>());
    }
}
