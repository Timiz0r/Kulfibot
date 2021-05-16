namespace Kulfibot.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    //TODO: if making a delegate-based handler that can replace these, do that
    internal class BasicHandler : IMessageHandler
    {
        private readonly MessageIntent intent;
        private readonly Func<Message, bool> intentPredicate;
        private readonly Func<Message, Task<IEnumerable<Message>>> handler;

        public BasicHandler(
            MessageIntent intent,
            Func<Message, bool> intentPredicate,
            Func<Message, Task<IEnumerable<Message>>> handler)
        {
            this.intent = intent;
            this.intentPredicate = intentPredicate;
            this.handler = handler;
        }

        public BasicHandler(
            MessageIntent intent,
            Func<Message, bool> intentPredicate,
            Func<Message, Task<Message>> handler) : this(
                intent,
                intentPredicate,
                async m =>
                {
                    Message result = await handler(m);
                    return new[] { m };
                })
        {
        }

        public BasicHandler(
            MessageIntent intent,
            Func<Message, bool> intentPredicate,
            Func<Message, IEnumerable<Message>> handler) : this(
                intent,
                intentPredicate,
                m => Task.FromResult(handler(m)))
        {
        }

        public BasicHandler(
            MessageIntent intent,
            Func<Message, bool> intentPredicate,
            Func<Message, Message> handler) : this(
                intent,
                intentPredicate,
                new Func<Message, IEnumerable<Message>>(m => new[] { handler(m) }))
        {
        }

        public MessageIntent DeclareIntent(Message message) =>
            intentPredicate(message) ? intent : MessageIntent.Ignore;

        public Task<IEnumerable<Message>> HandleAsync(Message message) =>
            intentPredicate(message) ? handler(message) : Messages.NoneAsync;

        public static IMessageHandler CreatePassive(
            Func<Message, bool> intentPredicate,
            Func<Message, Message> handler) =>
            new BasicHandler(MessageIntent.Passive, intentPredicate, handler);
        public static IMessageHandler CreatePassive(
            Func<Message, bool> intentPredicate,
            Func<Message, IEnumerable<Message>> handler) =>
            new BasicHandler(MessageIntent.Passive, intentPredicate, handler);
        public static IMessageHandler CreatePassive(
            Func<Message, bool> intentPredicate,
            Func<Message, Task<Message>> handler) =>
            new BasicHandler(MessageIntent.Passive, intentPredicate, handler);
        public static IMessageHandler CreatePassive(
            Func<Message, bool> intentPredicate,
            Func<Message, Task<IEnumerable<Message>>> handler) =>
            new BasicHandler(MessageIntent.Passive, intentPredicate, handler);

        public static IMessageHandler CreateCommand(
            Func<Message, bool> intentPredicate,
            Func<Message, Message> handler) =>
            new BasicHandler(MessageIntent.Command, intentPredicate, handler);
        public static IMessageHandler CreateCommand(
            Func<Message, bool> intentPredicate,
            Func<Message, IEnumerable<Message>> handler) =>
            new BasicHandler(MessageIntent.Command, intentPredicate, handler);
        public static IMessageHandler CreateCommand(
            Func<Message, bool> intentPredicate,
            Func<Message, Task<Message>> handler) =>
            new BasicHandler(MessageIntent.Command, intentPredicate, handler);
        public static IMessageHandler CreateCommand(
            Func<Message, bool> intentPredicate,
            Func<Message, Task<IEnumerable<Message>>> handler) =>
            new BasicHandler(MessageIntent.Command, intentPredicate, handler);
    }
}
