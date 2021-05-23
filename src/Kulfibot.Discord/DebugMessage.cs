namespace Kulfibot.Discord
{
    using static Kulfibot.Discord.DiscordMessageTransport;

    public record DebugMessage(RawPayload RawPayload) : Message;
}
