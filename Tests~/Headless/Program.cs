using System;
using System.Collections.Generic;
using System.Linq;
using VirtualRoom.Audience;

static class Program
{
    const float Dt = 1f / 60f;

    static int _failures;

    static void Main()
    {
        Console.WriteLine("=== Virtual Room :: audience simulation checks ===\n");

        Determinism();
        WarmRoomIsGenerous();
        ColdRoomChecksOut();
        JokeGetsLaughs();
        StrongFinishBringsTheHouseDown();
        NegativeReactionsCanBeDisabled();
        ContagionMakesWavesNotNoise();
        SeatingScales();
        ClassroomSeating();
        VenuesBuildDistinctRooms();
        TriggerReactionByName();

        Console.WriteLine();
        Console.WriteLine(_failures == 0
            ? "ALL CHECKS PASSED"
            : $"{_failures} CHECK(S) FAILED");

        Environment.Exit(_failures == 0 ? 0 : 1);
    }

    static void Determinism()
    {
        string A = Fingerprint(), B = Fingerprint();
        Check("Same seed reproduces the same room exactly", A == B, $"{A[..24]}... vs {B[..24]}...");

        static string Fingerprint()
        {
            var room = Room(seed: 7, mood: RoomMoodPreset.Polite);
            var log = new List<string>();
            room.Reacted += e => log.Add($"{room.Time:0.00}:{e.Member.Id}:{e.Reaction}");
            Run(room, 20f, energy: 0.6f);
            return string.Join("|", log);
        }
    }

    static void WarmRoomIsGenerous()
    {
        var room = Room(seed: 11, mood: RoomMoodPreset.Friendly);
        var counts = Tally(room, 30f, energy: 0.8f);

        int positive = counts.Where(c => Reactions.IsPositive(c.Key)).Sum(c => c.Value);
        int negative = counts.Where(c => Reactions.IsNegative(c.Key)).Sum(c => c.Value);

        Check("A warm, high-energy room is overwhelmingly positive",
            positive > negative * 5 && positive > 50,
            $"{positive} positive vs {negative} negative; mood ended {room.Mood}");
    }

    static void ColdRoomChecksOut()
    {
        var room = Room(seed: 11, mood: RoomMoodPreset.Cold);
        var counts = Tally(room, 30f, energy: 0.05f);

        int positive = counts.Where(c => Reactions.IsPositive(c.Key)).Sum(c => c.Value);
        int negative = counts.Where(c => Reactions.IsNegative(c.Key)).Sum(c => c.Value);

        Check("A cold room with a flat speaker yawns and checks out",
            negative > positive,
            $"{negative} negative vs {positive} positive; mood ended {room.Mood}");
    }

    static void JokeGetsLaughs()
    {
        var room = Room(seed: 3, mood: RoomMoodPreset.Friendly);
        int laughs = 0;
        room.Reacted += e => { if (e.Reaction == ReactionType.Laugh) laughs++; };

        Run(room, 5f, energy: 0.7f);
        int before = laughs;

        room.Cue(SpeakerCue.Joke);
        Run(room, 2.5f, energy: 0.7f);
        int after = laughs - before;

        Check("A joke actually gets a laugh out of the room",
            after >= 5, $"{after} people laughed in the 2.5s after the joke");
    }

    static void StrongFinishBringsTheHouseDown()
    {
        var room = Room(seed: 5, mood: RoomMoodPreset.Polite);
        var waves = new List<WaveEvent>();
        room.Waved += w => waves.Add(w);

        Run(room, 10f, energy: 0.6f);

        room.Cue(SpeakerCue.StrongFinish);

        float peak = 0f, settled;
        room.Speaker.Energy = 0.9f;
        for (float t = 0; t < 6f; t += Dt)
        {
            room.Tick(Dt);
            peak = Math.Max(peak, room.ApplauseMeter);
        }
        settled = room.ApplauseMeter;

        bool applauseWave = waves.Any(w => w.Reaction == ReactionType.Applause);
        Check("A strong finish triggers a room-wide applause WAVE",
            applauseWave,
            applauseWave ? waves.First(w => w.Reaction == ReactionType.Applause).ToString() : "no wave fired");

        Check("...the applause meter pins near the top during the ovation",
            peak > 0.6f, $"peaked at {peak:0.00}");

        Check("...and then the applause dies down again, like applause does",
            settled < peak * 0.8f, $"settled to {settled:0.00} from a peak of {peak:0.00}");
    }

    static void NegativeReactionsCanBeDisabled()
    {
        var config = Config(seed: 9);
        config.StartingMood = RoomMoodPreset.Cold;
        config.AllowNegativeReactions = false;

        var room = new AudienceRoom(config);
        var counts = Tally(room, 40f, energy: 0.05f);

        int negative = counts.Where(c => Reactions.IsNegative(c.Key)).Sum(c => c.Value);
        Check("Kind mode: nobody ever yawns, looks confused, or checks their phone",
            negative == 0, $"{negative} negative reactions leaked through");
    }

    static void ContagionMakesWavesNotNoise()
    {
        var with = Measure(contagion: 2.0f);
        var without = Measure(contagion: 0f);

        Check("Contagion makes reactions bunch into bursts in time",
            with.Fano > without.Fano * 1.25f,
            $"burstiness {with.Fano:0.00} with contagion vs {without.Fano:0.00} without " +
            $"(1.0 = pure random drizzle)");

        Check("Contagion makes reactions spread between NEIGHBOURS, not across the room",
            with.NeighbourDistance < without.NeighbourDistance,
            $"co-reacting people sit {with.NeighbourDistance:0.00}m apart with contagion " +
            $"vs {without.NeighbourDistance:0.00}m without (room baseline {with.Baseline:0.00}m)");

        static (double Fano, double NeighbourDistance, double Baseline) Measure(float contagion)
        {
            var config = Config(seed: 42);
            config.StartingMood = RoomMoodPreset.Friendly;
            config.ContagionGain = contagion;

            var room = new AudienceRoom(config);
            var events = new List<(float T, SeatPosition Seat)>();
            room.Reacted += e => events.Add((room.Time, e.Member.Seat));

            Run(room, 90f, energy: 0.75f);

            const float bin = 0.25f;
            var bins = new int[(int)(90f / bin) + 1];
            foreach (var e in events) bins[(int)(e.T / bin)]++;

            double mean = bins.Average();
            double variance = bins.Sum(b => (b - mean) * (b - mean)) / bins.Length;
            double fano = mean > 0 ? variance / mean : 0;

            var distances = new List<double>();
            for (int i = 0; i < events.Count; i++)
            {
                for (int j = i + 1; j < events.Count; j++)
                {
                    if (events[j].T - events[i].T > 0.5f) break;
                    distances.Add(events[i].Seat.DistanceTo(events[j].Seat));
                }
            }

            var seats = room.Members.Select(m => m.Seat).ToList();
            var all = new List<double>();
            for (int i = 0; i < seats.Count; i++)
                for (int j = i + 1; j < seats.Count; j++)
                    all.Add(seats[i].DistanceTo(seats[j]));

            return (fano,
                    distances.Count > 0 ? distances.Average() : 0,
                    all.Average());
        }
    }

    static void ClassroomSeating()
    {
        var settings = new SeatingSettings
        {
            Style = SeatingStyle.Classroom,
            FrontRowRadius = 3.6f,
            RowSpacing = 1.9f,
            SeatSpacing = 1.65f,
            AisleWidth = 1.3f,
            RiserHeight = 0f,
            Jitter = 0f,
        };

        var seats = SeatingLayout.Build(24, settings, seed: 1);

        int rows = seats.Select(s => s.Row).Distinct().Count();
        var perRow = seats.GroupBy(s => s.Row).Select(g => g.Count()).ToList();

        Check("Classroom seating puts everyone in straight rows in front of the speaker",
            seats.Length == 24 && seats.All(s => s.Z > 0f) && rows >= 3,
            $"{seats.Length} seats in {rows} rows of {string.Join("/", perRow)}");

        bool straight = seats.GroupBy(s => s.Row)
            .All(g => g.Max(s => s.Z) - g.Min(s => s.Z) < 0.01f);

        Check("...and each row is straight, not curved like the theatre layout",
            straight, straight ? "every row shares one Z" : "rows are bowed");

        var front = seats.Where(s => s.Row == 0).OrderBy(s => s.X).ToList();
        float widestGap = 0f;
        for (int i = 1; i < front.Count; i++)
            widestGap = Math.Max(widestGap, front[i].X - front[i - 1].X);

        Check("...with a centre aisle down the middle",
            widestGap > settings.SeatSpacing + 0.5f,
            $"widest gap in the front row is {widestGap:0.00}m vs {settings.SeatSpacing:0.00}m between neighbours");

        var leftEnd = seats.OrderBy(s => s.X).First();
        var rightEnd = seats.OrderByDescending(s => s.X).First();

        Check("...and the people at the ends of the rows turn inward to face you",
            leftEnd.FacingRadians > 0.05f && rightEnd.FacingRadians < -0.05f,
            $"left-hand seat turns {leftEnd.FacingRadians:0.00} rad, right-hand seat {rightEnd.FacingRadians:0.00} rad");
    }

    static void VenuesBuildDistinctRooms()
    {
        foreach (Venue v in Venues.All)
        {
            var preset = Venues.Preset(v);
            var room = new AudienceRoom(v);

            bool sized = room.Count == preset.Config.Size && room.Count > 0;
            bool allInFront = room.Members.All(m => m.Seat.Z > 0f);

            Check($"Venue {v,-14} builds a seated room the size it advertises",
                sized && allInFront,
                sized ? $"{room.Count} people, all in front of the speaker"
                      : $"expected {preset.Config.Size}, got {room.Count}");
        }

        var club = Venues.Preset(Venue.ComedyClub);
        var theater = Venues.Preset(Venue.Theater);

        Check("Comedy club is smaller and packed tighter than the theatre",
            club.Config.Size < theater.Config.Size &&
            club.Seating.SeatSpacing < theater.Seating.SeatSpacing,
            $"club: {club.Config.Size} people @ {club.Seating.SeatSpacing:0.00}m apart; " +
            $"theatre: {theater.Config.Size} @ {theater.Seating.SeatSpacing:0.00}m");

        Check("Comedy club runs hotter contagion than the theatre",
            club.Config.ContagionGain > theater.Config.ContagionGain,
            $"club gain {club.Config.ContagionGain:0.0} vs theatre {theater.Config.ContagionGain:0.0}");

        var classroom = Venues.Preset(Venue.Classroom);
        Check("Classroom applause is more muted than the theatre's",
            classroom.Config.WaveThreshold > theater.Config.WaveThreshold &&
            classroom.Config.ContagionGain < theater.Config.ContagionGain,
            $"classroom needs {classroom.Config.WaveThreshold:P0} of the room to wave (gain " +
            $"{classroom.Config.ContagionGain:0.0}) vs theatre {theater.Config.WaveThreshold:P0} " +
            $"(gain {theater.Config.ContagionGain:0.0})");

        var classroomRoom = new AudienceRoom(Venue.Classroom);
        bool straightRows = classroomRoom.Members
            .GroupBy(m => m.Seat.Row)
            .All(g => g.Max(m => m.Seat.Z) - g.Min(m => m.Seat.Z) < 0.2f);
        Check("Classroom venue seats people in straight rows, not an arc",
            straightRows, straightRows ? "every row shares one depth" : "rows are bowed like a theatre");
    }

    static void TriggerReactionByName()
    {
        var room = new AudienceRoom(Venue.Theater);
        int reactions = 0;
        room.Reacted += _ => reactions++;

        bool ran = room.TriggerReaction("Applause", intensity: 1f, spread: 1f, stagger: 0f);
        Check("TriggerReaction(\"Applause\") is accepted and makes the room react",
            ran && reactions > 0, $"accepted={ran}, {reactions} people reacted");

        bool lower = room.TriggerReaction("laugh", stagger: 0f);
        Check("Reaction names are case-insensitive (\"laugh\" works)", lower, $"accepted={lower}");

        int before = reactions;
        bool bogus = room.TriggerReaction("high-five");
        Check("An unknown reaction name is refused, not crashed on",
            !bogus && reactions == before, $"accepted={bogus}, extra reactions={reactions - before}");
    }

    static void SeatingScales()
    {
        foreach (int size in new[] { 1, 8, 30, 120 })
        {
            var seats = SeatingLayout.Theater(size, new SeatingSettings(), seed: 1);
            bool rightCount = seats.Length == size;
            bool allInFront = seats.All(s => s.Z > 0f);
            int rows = seats.Select(s => s.Row).Distinct().Count();

            string detail = !rightCount ? $"expected {size} seats, got {seats.Length}"
                : !allInFront ? "someone was seated BEHIND the speaker"
                : $"{rows} row(s), front row {seats.Min(s => s.Z):0.0}m out, back row {seats.Max(s => s.Z):0.0}m";

            Check($"Seating lays out {size,3} people in front of the speaker", rightCount && allInFront, detail);
        }
    }

    static RoomConfig Config(int seed) => new RoomConfig { Size = 30, Seed = seed };

    static AudienceRoom Room(int seed, RoomMoodPreset mood)
    {
        var c = Config(seed);
        c.StartingMood = mood;
        return new AudienceRoom(c);
    }

    static void Run(AudienceRoom room, float seconds, float energy)
    {
        room.Speaker.Energy = energy;
        for (float t = 0; t < seconds; t += Dt) room.Tick(Dt);
    }

    static Dictionary<ReactionType, int> Tally(AudienceRoom room, float seconds, float energy)
    {
        var counts = new Dictionary<ReactionType, int>();
        room.Reacted += e => counts[e.Reaction] = counts.GetValueOrDefault(e.Reaction) + 1;
        Run(room, seconds, energy);
        return counts;
    }

    static void Check(string what, bool passed, string detail)
    {
        if (!passed) _failures++;
        Console.WriteLine($"  [{(passed ? "PASS" : "FAIL")}] {what}");
        Console.WriteLine($"         {detail}");
    }
}
