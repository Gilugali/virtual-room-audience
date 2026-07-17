using System;

namespace VirtualRoom.Audience
{
    [Serializable]
    public class RoomConfig
    {
        public int Size = 24;

        public int Seed = 0;

        public RoomMoodPreset StartingMood = RoomMoodPreset.Polite;

        public bool AllowNegativeReactions = true;

        public float BaseReactionRate = 0.05f;

        public float MemberCooldown = 1.6f;

        public float ContagionGain = 2.0f;

        public float ContagionDecay = 2.0f;

        public float ContagionRadius = 4.0f;

        public float MaxSocialPressure = 5.0f;

        public float MoodResponsiveness = 0.8f;

        public float AttentionDecay = 0.06f;

        public float WarmthInertia = 0.12f;

        public float WaveThreshold = 0.35f;

        public float WaveWindow = 1.2f;

        public RoomConfig Clone() => (RoomConfig)MemberwiseClone();
    }
}
