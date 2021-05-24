namespace Kulfibot.Discord.Messages
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public abstract record DiscordResponseMessage(
        //ignoring for easier serialization of data
        [property: JsonIgnore] int Opcode) : Message
    {

        //object; otherwise, derived class's members not seen
        //or could make this a generic record, but probably not much point
        public virtual void SerializeData(Utf8JsonWriter jsonWriter) =>
            JsonSerializer.Serialize(jsonWriter, this, GetType());

        //it looks the same as above, but they have different purposes
        public void Serialize(Utf8JsonWriter jsonWriter) =>
            JsonSerializer.Serialize(jsonWriter, this, DiscordJsonSerialization.Options);
    }
}
