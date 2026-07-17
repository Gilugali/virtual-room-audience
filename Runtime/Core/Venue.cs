namespace VirtualRoom.Audience
{
    public enum Venue
    {
        Theater,

        Classroom,

        ConferenceHall,

        ComedyClub,
    }

    public sealed class VenuePreset
    {
        public Venue Venue { get; }

        public string DisplayName { get; }

        public string Description { get; }

        public RoomConfig Config { get; }

        public SeatingSettings Seating { get; }

        public VenuePreset(Venue venue, string displayName, string description, RoomConfig config, SeatingSettings seating)
        {
            Venue = venue;
            DisplayName = displayName;
            Description = description;
            Config = config;
            Seating = seating;
        }
    }

    public static class Venues
    {
        public static readonly Venue[] All =
        {
            Venue.Theater,
            Venue.Classroom,
            Venue.ConferenceHall,
            Venue.ComedyClub,
        };

        public static VenuePreset Preset(Venue venue)
        {
            switch (venue)
            {
                case Venue.Classroom:
                    return new VenuePreset(
                        venue,
                        "Classroom",
                        "A small, attentive class in straight rows. Questions and nods, muted applause.",
                        new RoomConfig
                        {
                            Size = 28,
                            StartingMood = RoomMoodPreset.Polite,
                            ContagionGain = 1.4f,
                            ContagionRadius = 3.0f,
                            ContagionDecay = 2.3f,
                            MemberCooldown = 1.8f,
                            WaveThreshold = 0.42f,
                        },
                        new SeatingSettings
                        {
                            Style = SeatingStyle.Classroom,
                            FrontRowRadius = 3.4f,
                            RowSpacing = 1.6f,
                            SeatSpacing = 1.3f,
                            AisleWidth = 1.3f,
                            RiserHeight = 0f,
                            Jitter = 0.08f,
                        });

                case Venue.ConferenceHall:
                    return new VenuePreset(
                        venue,
                        "Conference hall",
                        "A medium-large professional crowd. Restrained — nods and thumbs-up, not roars.",
                        new RoomConfig
                        {
                            Size = 48,
                            StartingMood = RoomMoodPreset.Polite,
                            ContagionGain = 1.7f,
                            ContagionRadius = 4.0f,
                            ContagionDecay = 2.1f,
                            WaveThreshold = 0.38f,
                        },
                        new SeatingSettings
                        {
                            Style = SeatingStyle.Theater,
                            ArcDegrees = 110f,
                            FrontRowRadius = 3.8f,
                            RowSpacing = 1.5f,
                            SeatSpacing = 1.15f,
                            RiserHeight = 0.30f,
                        });

                case Venue.ComedyClub:
                    return new VenuePreset(
                        venue,
                        "Comedy club",
                        "A small, dense club. Contagion runs hot: it laughs easy and dies hard.",
                        new RoomConfig
                        {
                            Size = 22,
                            StartingMood = RoomMoodPreset.Polite,
                            ContagionGain = 3.0f,
                            ContagionRadius = 4.5f,
                            ContagionDecay = 2.4f,
                            BaseReactionRate = 0.07f,
                            WaveThreshold = 0.30f,
                        },
                        new SeatingSettings
                        {
                            Style = SeatingStyle.Theater,
                            ArcDegrees = 170f,
                            FrontRowRadius = 2.3f,
                            RowSpacing = 1.1f,
                            SeatSpacing = 0.9f,
                            RiserHeight = 0.20f,
                            Jitter = 0.15f,
                        });

                case Venue.Theater:
                default:
                    return new VenuePreset(
                        Venue.Theater,
                        "Theater",
                        "A big tiered auditorium. Warm, and applause carries across the whole room.",
                        new RoomConfig
                        {
                            Size = 80,
                            StartingMood = RoomMoodPreset.Polite,
                            ContagionGain = 2.3f,
                            ContagionRadius = 5.5f,
                            ContagionDecay = 1.7f,
                            WaveThreshold = 0.30f,
                        },
                        new SeatingSettings
                        {
                            Style = SeatingStyle.Theater,
                            ArcDegrees = 140f,
                            FrontRowRadius = 4.0f,
                            RowSpacing = 1.5f,
                            SeatSpacing = 1.1f,
                            RiserHeight = 0.45f,
                        });
            }
        }
    }
}
