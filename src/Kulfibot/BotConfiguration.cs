namespace Kulfibot
{
    using System.Collections.Generic;

    //TODO: use immutable collections
    public sealed record BotConfiguration(
        IReadOnlyList<IMessageTransport> MessageTransports,
        IReadOnlyList<IMessageHandler> MessageHandlers
    );
}
