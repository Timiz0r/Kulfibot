namespace Kulfibot
{
    using System.Collections.Generic;

    public record BotConfiguration(
        IReadOnlyList<IMessageSource> MessageSources,
        IReadOnlyList<IMessageHandler> MessageHandlers
    );
}
