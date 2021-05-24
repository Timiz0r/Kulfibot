namespace Kulfibot.Discord
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public record RawPayload(
        [property: JsonPropertyName("op")] int Opcode,
        [property: JsonPropertyName("d")] JsonElement? Data,
        [property: JsonPropertyName("s")] int? Sequence,
        [property: JsonPropertyName("t")] string? Name
    );
}
