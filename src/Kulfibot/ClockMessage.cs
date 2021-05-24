namespace Kulfibot
{
    using NodaTime;

    public record ClockMessage(Instant Instant) : Message;
}
