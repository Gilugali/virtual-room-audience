namespace VirtualRoom.Audience
{
    public readonly struct ReactionEvent
    {
        public readonly AudienceMember Member;
        public readonly ReactionType Reaction;

        public readonly float Intensity;

        public ReactionEvent(AudienceMember member, ReactionType reaction, float intensity)
        {
            Member = member;
            Reaction = reaction;
            Intensity = intensity;
        }

        public override string ToString() => $"#{Member.Id} {Reaction} ({Intensity:0.00})";
    }

    public readonly struct WaveEvent
    {
        public readonly ReactionType Reaction;

        public readonly int Count;

        public readonly float Fraction;

        public WaveEvent(ReactionType reaction, int count, float fraction)
        {
            Reaction = reaction;
            Count = count;
            Fraction = fraction;
        }

        public override string ToString() => $"WAVE: {Reaction} x{Count} ({Fraction:P0} of the room)";
    }
}
