using System;

namespace VirtualRoom.Audience
{
    public enum ReactionType
    {
        None = 0,

        Applause,
        Cheer,
        Laugh,
        Nod,
        Wow,
        Love,
        ThumbsUp,

        Question,

        Confused,
        Yawn,
        Distracted,
    }

    public enum Valence
    {
        Negative = -1,
        Neutral = 0,
        Positive = 1,
    }

    public static class Reactions
    {
        public static readonly ReactionType[] All =
        {
            ReactionType.Applause,
            ReactionType.Cheer,
            ReactionType.Laugh,
            ReactionType.Nod,
            ReactionType.Wow,
            ReactionType.Love,
            ReactionType.ThumbsUp,
            ReactionType.Question,
            ReactionType.Confused,
            ReactionType.Yawn,
            ReactionType.Distracted,
        };

        public static Valence ValenceOf(ReactionType reaction)
        {
            switch (reaction)
            {
                case ReactionType.Applause:
                case ReactionType.Cheer:
                case ReactionType.Laugh:
                case ReactionType.Nod:
                case ReactionType.Wow:
                case ReactionType.Love:
                case ReactionType.ThumbsUp:
                    return Valence.Positive;

                case ReactionType.Confused:
                case ReactionType.Yawn:
                case ReactionType.Distracted:
                    return Valence.Negative;

                default:
                    return Valence.Neutral;
            }
        }

        public static bool IsPositive(ReactionType r) => ValenceOf(r) == Valence.Positive;
        public static bool IsNegative(ReactionType r) => ValenceOf(r) == Valence.Negative;

        public static float DurationOf(ReactionType reaction)
        {
            switch (reaction)
            {
                case ReactionType.Applause: return 2.4f;
                case ReactionType.Cheer: return 1.8f;
                case ReactionType.Laugh: return 1.6f;
                case ReactionType.Nod: return 1.0f;
                case ReactionType.Wow: return 1.2f;
                case ReactionType.Love: return 1.8f;
                case ReactionType.ThumbsUp: return 1.4f;
                case ReactionType.Question: return 3.0f;
                case ReactionType.Confused: return 1.6f;
                case ReactionType.Yawn: return 2.0f;
                case ReactionType.Distracted: return 3.5f;
                default: return 1.0f;
            }
        }

        public static Rgb ColorOf(ReactionType reaction)
        {
            switch (reaction)
            {
                case ReactionType.Applause: return new Rgb(1.00f, 0.84f, 0.25f);
                case ReactionType.Cheer: return new Rgb(1.00f, 0.55f, 0.15f);
                case ReactionType.Laugh: return new Rgb(0.55f, 0.85f, 0.35f);
                case ReactionType.Nod: return new Rgb(0.60f, 0.75f, 0.90f);
                case ReactionType.Wow: return new Rgb(0.70f, 0.50f, 0.95f);
                case ReactionType.Love: return new Rgb(0.95f, 0.35f, 0.55f);
                case ReactionType.ThumbsUp: return new Rgb(0.35f, 0.80f, 0.70f);
                case ReactionType.Question: return new Rgb(0.95f, 0.95f, 0.95f);
                case ReactionType.Confused: return new Rgb(0.80f, 0.65f, 0.30f);
                case ReactionType.Yawn: return new Rgb(0.55f, 0.55f, 0.60f);
                case ReactionType.Distracted: return new Rgb(0.40f, 0.40f, 0.45f);
                default: return new Rgb(1f, 1f, 1f);
            }
        }

        public static string GlyphOf(ReactionType reaction)
        {
            switch (reaction)
            {
                case ReactionType.Applause: return "clap!";
                case ReactionType.Cheer: return "woo!";
                case ReactionType.Laugh: return "ha!";
                case ReactionType.Nod: return "mm-hm";
                case ReactionType.Wow: return "whoa";
                case ReactionType.Love: return "<3";
                case ReactionType.ThumbsUp: return "+1";
                case ReactionType.Question: return "?";
                case ReactionType.Confused: return "??";
                case ReactionType.Yawn: return "yawn";
                case ReactionType.Distracted: return "...";
                default: return "";
            }
        }
    }
}
