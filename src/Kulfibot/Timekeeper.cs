namespace Kulfibot
{
    using NodaTime;

    public class Timekeeper
    {
        private readonly Duration duration;
        private Instant nextInstant = SystemClock.Instance.GetCurrentInstant();

        public Timekeeper(Duration duration)
        {
            this.duration = duration;
        }

        public bool HitTargetTime(Instant instant)
        {
            if (instant < nextInstant)
            {
                return false;
            }

            nextInstant = instant + duration;
            return true;
        }
    }
}
