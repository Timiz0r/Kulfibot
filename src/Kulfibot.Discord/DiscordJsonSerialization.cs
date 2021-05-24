namespace Kulfibot.Discord
{
    using System.Text.Json;

    public static class DiscordJsonSerialization
    {
        //dont mutate pls
        public static readonly JsonSerializerOptions Options = GetJsonSerializerOptions();

        private static JsonSerializerOptions GetJsonSerializerOptions()
        {
            JsonSerializerOptions serializationOptions = new();
            serializationOptions.Converters.Add(new DiscordResponseMessageSerializer());
            serializationOptions.Converters.Add(new DiscordDurationJsonConverter());

            return serializationOptions;
        }
    }
}
