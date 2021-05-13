namespace Kulfibot
{
    using System.Collections.Generic;

    public record BotConfiguration(
        IReadOnlyList<IMessageTransport> MessageTransports,
        IReadOnlyList<IMessageHandler> MessageHandlers
    );
}
