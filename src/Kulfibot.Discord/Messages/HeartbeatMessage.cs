namespace Kulfibot.Discord.Messages
{
    public record HeartbeatMessage() : DiscordResponseMessage(Opcode: 1);
}
