namespace VirtualRoom.Audience
{
    internal sealed class ReactionPicker
    {
        private readonly RoomConfig _config;
        private readonly RoomRandom _rng;
        private readonly float[] _weights = new float[Reactions.All.Length];

        public ReactionPicker(RoomConfig config, RoomRandom rng)
        {
            _config = config;
            _rng = rng;
        }

        public ReactionType Pick(in Mood mood, in Personality p)
        {
            float eng = mood.Engagement;
            float warm = mood.Warmth;
            float bored = 1f - eng;
            float cold = 1f - warm;

            for (int i = 0; i < Reactions.All.Length; i++)
            {
                ReactionType r = Reactions.All[i];
                float w;

                switch (r)
                {
                    case ReactionType.Applause:
                        w = eng * warm * 1.0f;
                        break;
                    case ReactionType.Cheer:
                        w = eng * eng * warm * p.Enthusiasm * 0.9f;
                        break;
                    case ReactionType.Laugh:
                        w = eng * warm * 0.25f;
                        break;
                    case ReactionType.Nod:
                        w = eng * 1.1f;
                        break;
                    case ReactionType.Wow:
                        w = eng * warm * 0.35f;
                        break;
                    case ReactionType.Love:
                        w = eng * warm * warm * p.Expressiveness * 0.4f;
                        break;
                    case ReactionType.ThumbsUp:
                        w = eng * warm * 0.5f;
                        break;
                    case ReactionType.Question:
                        w = eng * p.Skepticism * 0.55f;
                        break;
                    case ReactionType.Confused:
                        w = eng * cold * cold * p.Skepticism * 1.4f;
                        break;
                    case ReactionType.Yawn:
                        w = bored * bored * 1.6f;
                        break;
                    case ReactionType.Distracted:
                        w = bored * bored * (1f - p.AttentionSpan) * 2.4f;
                        break;
                    default:
                        w = 0f;
                        break;
                }

                if (!_config.AllowNegativeReactions && Reactions.IsNegative(r)) w = 0f;

                _weights[i] = w;
            }

            float total = 0f;
            for (int i = 0; i < _weights.Length; i++) total += _weights[i];

            if (total <= 0.0001f) return ReactionType.None;

            float roll = _rng.Next() * total;
            for (int i = 0; i < _weights.Length; i++)
            {
                roll -= _weights[i];
                if (roll <= 0f) return Reactions.All[i];
            }

            return Reactions.All[Reactions.All.Length - 1];
        }

        public bool AcceptsInvitation(ReactionType invited, in Mood mood, in Personality p)
        {
            if (invited == ReactionType.None) return false;
            if (!_config.AllowNegativeReactions && Reactions.IsNegative(invited)) return false;

            float willingness;
            if (Reactions.IsPositive(invited))
            {
                willingness = 0.25f + mood.Warmth * 0.55f + p.Enthusiasm * 0.3f - p.Skepticism * 0.25f;
            }
            else if (Reactions.IsNegative(invited))
            {
                willingness = 0.5f + (1f - mood.Engagement) * 0.5f;
            }
            else
            {
                willingness = 0.4f + p.Skepticism * 0.4f;
            }

            return _rng.Chance(M.Clamp01(willingness));
        }
    }
}
