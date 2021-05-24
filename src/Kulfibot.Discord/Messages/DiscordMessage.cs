namespace Kulfibot.Discord.Messages
{
    using System;
    using System.Buffers;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public record DiscordMessage(
        int Opcode,
        JsonElement? Data = null,
        int? Sequence = null,
        string? Name = null) : Message
    {
        public T ConvertData<T>()
        {
            //throw because caller should know if there's data or not
            if (this.Data is null)
            {
                throw new InvalidOperationException("Payload has no data.");
            }

            //bytes vs string? go bytes
            ArrayBufferWriter<byte> buffer = new();
            using (Utf8JsonWriter writer = new(buffer))
            {
                this.Data.Value.WriteTo(writer);
            }

            T result = JsonSerializer.Deserialize<T>(buffer.WrittenSpan, DiscordJsonSerialization.Options)!;
            return result;
        }

        public static DiscordMessage FromPayload(RawPayload payload) => new(
            Opcode: payload.Opcode,
            Data: payload.Data,
            Sequence: payload.Sequence,
            Name: payload.Name);
    }
}
