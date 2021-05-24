namespace Kulfibot.Discord.Messages
{
    using System.Text.Json;

    public record IdentifyMessage(
        string Token,
        int Intents,
        string OS,
        string Browser,
        string Device) : DiscordResponseMessage(Opcode: 2)
    {
        public override void SerializeData(Utf8JsonWriter jsonWriter)
        {
            jsonWriter.WriteStartObject();

            jsonWriter.WriteString("token", this.Token);
            jsonWriter.WriteNumber("intents", this.Intents);

            jsonWriter.WriteStartObject("properties");
            jsonWriter.WriteString("$os", this.OS);
            jsonWriter.WriteString("$browser", this.Browser);
            jsonWriter.WriteString("$device", this.Device);
            jsonWriter.WriteEndObject();

            jsonWriter.WriteEndObject();
        }
    }
}
