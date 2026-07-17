namespace VirtualRoom.Audience
{
    public sealed class AudienceMember
    {
        public int Id { get; }

        public SeatPosition Seat { get; internal set; }

        public Personality Personality { get; }

        public float Attention { get; internal set; }

        public ReactionType CurrentReaction { get; internal set; }

        public float ReactionIntensity { get; internal set; }

        public float ReactionTimeLeft { get; internal set; }

        public bool IsReacting => CurrentReaction != ReactionType.None && ReactionTimeLeft > 0f;

        internal float SocialPressure;

        internal float Cooldown;

        internal float WanderPhase;

        internal AudienceMember(int id, Personality personality, SeatPosition seat, float attention, float wanderPhase)
        {
            Id = id;
            Personality = personality;
            Seat = seat;
            Attention = attention;
            WanderPhase = wanderPhase;
            CurrentReaction = ReactionType.None;
        }
    }
}
