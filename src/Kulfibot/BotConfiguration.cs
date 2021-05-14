namespace Kulfibot
{
    using System.Collections.Immutable;

    public sealed record BotConfiguration(
        ImmutableList<IMessageTransport> MessageTransports,
        ImmutableList<IMessageHandler> MessageHandlers
    );
}
