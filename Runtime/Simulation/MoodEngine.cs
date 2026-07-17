namespace VirtualRoom.Audience
{
    internal sealed class MoodEngine
    {
        private readonly RoomConfig _config;

        private float _engagement;
        private float _warmth;

        private float _reactionSentiment;

        public Mood Current => new Mood(_engagement, _warmth);

        public MoodEngine(RoomConfig config)
        {
            _config = config;
            Reset();

        }

        public void Reset()
        {
            Mood start = MoodPresets.Resolve(_config.StartingMood);
            _engagement = start.Engagement;
            _warmth = start.Warmth;
            _reactionSentiment = 0f;
        }

        public void Tick(float dt, float speakerEnergy)
        {
            float target = M.Clamp01(0.15f + speakerEnergy * 0.85f);
            _engagement = M.Damp(_engagement, target, _config.MoodResponsiveness, dt);

            _engagement -= _config.AttentionDecay * dt * (1f - speakerEnergy);
            _engagement = M.Clamp01(_engagement);

            float warmthTarget = M.Clamp01(0.5f + _reactionSentiment * 0.5f);
            _warmth = M.Damp(_warmth, warmthTarget, _config.WarmthInertia, dt);

            _reactionSentiment = M.Damp(_reactionSentiment, 0f, 0.15f, dt);
        }

        public void ApplyCue(in CueProfile cue)
        {
            _engagement = M.Clamp01(_engagement + cue.EngagementDelta);
            _warmth = M.Clamp01(_warmth + cue.WarmthDelta);
        }

        public void ObserveReaction(ReactionType reaction, float intensity, int roomSize)
        {
            if (roomSize <= 0) return;

            float valence = (int)Reactions.ValenceOf(reaction);
            if (valence == 0f) return;

            _reactionSentiment = M.Clamp(
                _reactionSentiment + valence * intensity * (2.5f / roomSize),
                -1f, 1f);
        }

        public void Nudge(float engagementDelta, float warmthDelta)
        {
            _engagement = M.Clamp01(_engagement + engagementDelta);
            _warmth = M.Clamp01(_warmth + warmthDelta);
        }

        public void Set(float engagement, float warmth)
        {
            _engagement = M.Clamp01(engagement);
            _warmth = M.Clamp01(warmth);
            _reactionSentiment  = M.Clamp01(warmth);
        }
    }
}
