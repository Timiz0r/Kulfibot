namespace Kulfibot.Discord
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using Kulfibot.Discord.Messages;

    public class DiscordResponseMessageSerializer : JsonConverter<DiscordResponseMessage>
    {
        public override bool CanConvert(Type typeToConvert) =>
            typeof(DiscordResponseMessage).IsAssignableFrom(typeToConvert);

        public override DiscordResponseMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            throw new NotImplementedException();
        public override void Write(Utf8JsonWriter writer, DiscordResponseMessage value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteNumber("op", value.Opcode);

            writer.WritePropertyName("d");
            value.SerializeData(writer);

            writer.WriteEndObject();
        }
    }
}
