namespace Kulfibot.Discord.Messages
{
    using System.Text.Json.Serialization;
    using NodaTime;

    public record HelloMessage(
        [property: JsonPropertyName("heartbeat_interval")] Duration HeartbeatInterval
    ) : Message;
}
