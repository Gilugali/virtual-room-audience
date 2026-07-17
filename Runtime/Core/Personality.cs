namespace VirtualRoom.Audience
{
    public readonly struct Personality
    {
        public readonly float Enthusiasm;

        public readonly float AttentionSpan;

        public readonly float Expressiveness;

        public readonly float Skepticism;

        public readonly float Suggestibility;

        public Personality(float enthusiasm, float attentionSpan, float expressiveness, float skepticism, float suggestibility)
        {
            Enthusiasm = enthusiasm;
            AttentionSpan = attentionSpan;
            Expressiveness = expressiveness;
            Skepticism = skepticism;
            Suggestibility = suggestibility;
        }

        internal static Personality Roll(RoomRandom rng)
        {
            return new Personality(
                enthusiasm: rng.Bell(),
                attentionSpan: rng.Bell(),
                expressiveness: rng.Bell(),
                skepticism: rng.Bell(0.4f),
                suggestibility: rng.Bell(0.6f));
        }
    }
}
