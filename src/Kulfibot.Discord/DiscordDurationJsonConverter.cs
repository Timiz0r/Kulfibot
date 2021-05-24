namespace Kulfibot.Discord
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using NodaTime;

    //so will assume integer milliseconds
    internal class DiscordDurationJsonConverter : JsonConverter<Duration>
    {
        public override Duration Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            Duration.FromMilliseconds(reader.GetInt64());

        public override void Write(Utf8JsonWriter writer, Duration value, JsonSerializerOptions options) =>
            writer.WriteNumberValue((int)value.TotalMilliseconds);
    }
}
