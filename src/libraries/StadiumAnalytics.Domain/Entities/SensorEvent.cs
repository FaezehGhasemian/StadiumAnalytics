using StadiumAnalytics.Domain.Enums;

namespace StadiumAnalytics.Domain.Entities
{
    public class SensorEvent
    {
        public long Id { get; private set; }
        public string Gate { get; private set; }
        public DateTime TimestampUtc { get; private set; }
        public int NumberOfPeople { get; private set; }
        public MovementType Type { get; private set; }

        private SensorEvent()
        {
            Gate = string.Empty;
        }

        public SensorEvent(string gate, DateTime timestampUtc, int numberOfPeople, MovementType type)
        {
            if (string.IsNullOrWhiteSpace(gate))
                throw new ArgumentException("Gate must be provided.", nameof(gate));

            if (timestampUtc == default)
                throw new ArgumentException("Timestamp must be provided.", nameof(timestampUtc));

            if (timestampUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("Timestamp must be in UTC.", nameof(timestampUtc));

            if (numberOfPeople < 0)
                throw new ArgumentOutOfRangeException(nameof(numberOfPeople), "Number of people cannot be negative.");

            Gate = gate.Trim();
            TimestampUtc = timestampUtc;
            NumberOfPeople = numberOfPeople;
            Type = type;
        }
    }
}
