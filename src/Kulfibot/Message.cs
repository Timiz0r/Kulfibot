namespace Kulfibot
{
    using System;

    public record Message(Guid Id)
    {
        public Message() : this(Guid.NewGuid())
        {
        }
    }
}
