namespace Kulfibot
{
    using System.Collections.Immutable;

    //TODO: this likely changes once we get an ioc container
    //aka getting instances exactly when needed. perhaps handlers are short-lived.
    public sealed record BotConfiguration(
        ImmutableList<IMessageTransport> MessageTransports,
        ImmutableList<IMessageHandler> MessageHandlers
    );
}
