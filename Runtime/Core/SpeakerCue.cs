namespace VirtualRoom.Audience
{
    public enum SpeakerCue
    {
        Greeting,
        Joke,
        KeyPoint,
        Question,

        Pause,

        Stumble,

        Silence,

        StrongFinish,
    }

    internal readonly struct CueProfile
    {
        public readonly float EngagementDelta;
        public readonly float WarmthDelta;
        public readonly ReactionType Invites;
        public readonly float InviteStrength;
        public readonly float InviteWindow;

        private CueProfile(float engagement, float warmth, ReactionType invites, float strength, float window)
        {
            EngagementDelta = engagement;
            WarmthDelta = warmth;
            Invites = invites;
            InviteStrength = strength;
            InviteWindow = window;
        }

        public static CueProfile For(SpeakerCue cue)
        {
            switch (cue)
            {
                case SpeakerCue.Greeting:
                    return new CueProfile(0.10f, 0.05f, ReactionType.Applause, 0.55f, 1.5f);
                case SpeakerCue.Joke:
                    return new CueProfile(0.08f, 0.12f, ReactionType.Laugh, 0.95f, 2.0f);
                case SpeakerCue.KeyPoint:
                    return new CueProfile(0.12f, 0.03f, ReactionType.Nod, 0.65f, 2.0f);
                case SpeakerCue.Question:
                    return new CueProfile(0.10f, 0.00f, ReactionType.Question, 0.50f, 2.5f);
                case SpeakerCue.Pause:
                    return new CueProfile(0.06f, 0.00f, ReactionType.Nod, 0.25f, 1.5f);
                case SpeakerCue.Stumble:
                    return new CueProfile(-0.10f, -0.05f, ReactionType.Confused, 0.45f, 2.0f);
                case SpeakerCue.Silence:
                    return new CueProfile(-0.16f, -0.03f, ReactionType.Distracted, 0.55f, 3.0f);
                case SpeakerCue.StrongFinish:
                    return new CueProfile(0.25f, 0.20f, ReactionType.Applause, 1.10f, 3.5f);
                default:
                    return new CueProfile(0f, 0f, ReactionType.None, 0f, 0f);
              }

        }

    }
}
