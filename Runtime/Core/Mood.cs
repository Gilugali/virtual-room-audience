namespace VirtualRoom.Audience
{
    public enum RoomMood
    {
        Cold,

        Restless,

        Drifting,

        Polite,

        Attentive,

        Warm,

        Electric,
    }

    public readonly struct Mood
    {
        public readonly float Engagement;
        public readonly float Warmth;

        public Mood(float engagement, float warmth)
        {
            Engagement = engagement;
            Warmth = warmth;
        }

        public RoomMood Label
        {
            get
            {
                bool engaged = Engagement >= 0.55f;
                bool awake = Engagement >= 0.3f;

                if (Engagement >= 0.8f && Warmth >= 0.75f) return RoomMood.Electric;
                if (engaged && Warmth >= 0.6f) return RoomMood.Warm;
                if (engaged) return RoomMood.Attentive;
                if (awake && Warmth >= 0.45f) return RoomMood.Polite;
                if (awake) return RoomMood.Restless;
                if (Warmth >= 0.45f) return RoomMood.Drifting;
                return RoomMood.Cold;
            }
        }

        public override string ToString() =>
            $"{Label} (engagement {Engagement:0.00}, warmth {Warmth:0.00})";
    }

    public enum RoomMoodPreset
    {
        Cold,
        Polite,
        Friendly,
        Hyped,
    }

    internal static class MoodPresets
    {
        public static Mood Resolve(RoomMoodPreset preset)
        {
            switch (preset)
            {
                case RoomMoodPreset.Cold: return new Mood(0.20f, 0.20f);
                case RoomMoodPreset.Friendly: return new Mood(0.60f, 0.70f);
                case RoomMoodPreset.Hyped: return new Mood(0.85f, 0.90f);
                default: return new Mood(0.45f, 0.50f);
            }
        }
    }
}
