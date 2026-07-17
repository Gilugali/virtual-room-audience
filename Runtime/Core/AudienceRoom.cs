using System;
using System.Collections.Generic;

namespace VirtualRoom.Audience
{
    public sealed class SpeakerState
    {
        private float _energy = 0.5f;

        public float Energy
        {
            get => _energy;
            set => _energy = M.Clamp01(value);
        }
    }

    public sealed class AudienceRoom
    {
        private readonly RoomConfig _config;
        private readonly SeatingSettings _seating;
        private readonly RoomRandom _rng;
        private readonly MoodEngine _mood;
        private readonly ReactionPicker _picker;

        private readonly List<AudienceMember> _members = new List<AudienceMember>();
        private readonly List<PendingReaction> _pending = new List<PendingReaction>();
        private readonly List<TimedReaction> _recent = new List<TimedReaction>();
        private readonly float[] _waveCooldowns = new float[Reactions.All.Length];

        private int _nextId = 1;
        private float _time;
        private float _applauseMeter;

        private ReactionType _inviteReaction = ReactionType.None;
        private float _inviteStrength;
        private float _inviteTimeLeft;

        public RoomConfig Config => _config;

        public AudienceRoom(Venue venue) : this(Venues.Preset(venue)) { }

        private AudienceRoom(VenuePreset preset) : this(preset.Config, preset.Seating) { }

        public AudienceRoom(RoomConfig config = null, SeatingSettings seating = null)
        {
            _config = config ?? new RoomConfig();
            _seating = seating ?? new SeatingSettings();
            _rng = new RoomRandom(_config.Seed);
            _mood = new MoodEngine(_config);
            _picker = new ReactionPicker(_config, _rng);

            Speaker = new SpeakerState();

            Populate(_config.Size);
        }

        public SpeakerState Speaker { get; }

        public IReadOnlyList<AudienceMember> Members => _members;

        public int Count => _members.Count;

        public Mood Mood => _mood.Current;

        public RoomMood MoodLabel => _mood.Current.Label;

        public float ApplauseMeter => _applauseMeter;

        public float Time => _time;

        public event Action<ReactionEvent> Reacted;

        public event Action<WaveEvent> Waved;

        public event Action<RoomMood> MoodChanged;

        public event Action<AudienceMember> MemberJoined;
        public event Action<AudienceMember> MemberLeft;

        private RoomMood _lastMoodLabel;

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f) return;

            if (deltaTime > 0.25f) deltaTime = 0.25f;

            _time += deltaTime;

            _mood.Tick(deltaTime, Speaker.Energy);
            TickInvitation(deltaTime);
            TickPending(deltaTime);

            Mood mood = _mood.Current;
            for (int i = 0; i < _members.Count; i++)
            {
                TickMember(_members[i], deltaTime, mood);
            }

            TickWaves(deltaTime);
            TickApplauseMeter(deltaTime);
            TickMoodLabel();
        }

        public void Cue(SpeakerCue cue)
        {
            CueProfile profile = CueProfile.For(cue);

            _mood.ApplyCue(profile);

            if (profile.Invites != ReactionType.None && profile.InviteWindow > 0f)
            {
                _inviteReaction = profile.Invites;
                _inviteStrength = profile.InviteStrength;
                _inviteTimeLeft = profile.InviteWindow;
            }
        }

        public void TriggerReaction(ReactionType reaction, float intensity = 1f, float spread = 1f, float stagger = 0.45f)
        {
            if (reaction == ReactionType.None || _members.Count == 0) return;
            if (!_config.AllowNegativeReactions && Reactions.IsNegative(reaction)) return;

            intensity = M.Clamp01(intensity);
            spread = M.Clamp01(spread);

            for (int i = 0; i < _members.Count; i++)
            {
                AudienceMember m = _members[i];
                if (!_rng.Chance(spread)) continue;

                float eagerness = M.Clamp01(m.Personality.Enthusiasm * 0.6f + m.Personality.Suggestibility * 0.4f);
                float delay = stagger <= 0f ? 0f : _rng.Range(0f, stagger) * (1.2f - eagerness);

                float personal = M.Clamp01(intensity * (0.7f + m.Personality.Expressiveness * 0.5f));

                if (delay <= 0f) StartReaction(m, reaction, personal);
                else _pending.Add(new PendingReaction(m.Id, delay, reaction, personal));
            }
        }

        public bool TriggerReaction(string reaction, float intensity = 1f, float spread = 1f, float stagger = 0.45f)
        {
            if (!Enum.TryParse(reaction, ignoreCase: true, out ReactionType parsed) ||
                !Enum.IsDefined(typeof(ReactionType), parsed))
            {
                return false;
            }

            TriggerReaction(parsed, intensity, spread, stagger);
            return true;
        }

        public void Applaud(float intensity = 1f) => TriggerReaction(ReactionType.Applause, intensity);

        public void Cheer(float intensity = 1f) => TriggerReaction(ReactionType.Cheer, intensity);

        public void NudgeMood(float engagementDelta, float warmthDelta) => _mood.Nudge(engagementDelta, warmthDelta);

        public void SetMood(float engagement, float warmth) => _mood.Set(engagement, warmth);

        public void Populate(int size)
        {
            Clear();
            if (size <= 0) return;

            SeatPosition[] seats = SeatingLayout.Build(size, _seating, _config.Seed);

            for (int i = 0; i < size; i++)
            {
                var member = new AudienceMember(
                    id: _nextId++,
                    personality: Personality.Roll(_rng),
                    seat: seats[i],
                    attention: _mood.Current.Engagement,
                    wanderPhase: _rng.Range(0f, 100f));

                _members.Add(member);
                MemberJoined?.Invoke(member);
            }
        }

        public AudienceMember AddMember()
        {
            SeatPosition[] seats = SeatingLayout.Build(_members.Count + 1, _seating, _config.Seed);

            for (int i = 0; i < _members.Count; i++) _members[i].Seat = seats[i];

            var member = new AudienceMember(
                id: _nextId++,
                personality: Personality.Roll(_rng),
                seat: seats[seats.Length - 1],
                attention: _mood.Current.Engagement,
                wanderPhase: _rng.Range(0f, 100f));

            _members.Add(member);
            MemberJoined?.Invoke(member);
            return member;
        }

        public bool RemoveMember(int id)
        {
            for (int i = 0; i < _members.Count; i++)
            {
                if (_members[i].Id != id) continue;

                AudienceMember member = _members[i];
                _members.RemoveAt(i);
                _pending.RemoveAll(p => p.MemberId == id);
                MemberLeft?.Invoke(member);
                return true;
            }

            return false;
        }

        public void Clear()
        {
            for (int i = _members.Count - 1; i >= 0; i--)
            {
                AudienceMember member = _members[i];
                _members.RemoveAt(i);
                MemberLeft?.Invoke(member);
            }

            _pending.Clear();
            _recent.Clear();
        }

        public void Reset()
        {
            _mood.Reset();
            _pending.Clear();
            _recent.Clear();
            _time = 0f;
            _applauseMeter = 0f;
            _inviteReaction = ReactionType.None;
            _inviteTimeLeft = 0f;

            for (int i = 0; i < _waveCooldowns.Length; i++) _waveCooldowns[i] = 0f;

            foreach (AudienceMember m in _members)
            {
                m.CurrentReaction = ReactionType.None;
                m.ReactionTimeLeft = 0f;
                m.ReactionIntensity = 0f;
                m.SocialPressure = 0f;
                m.Cooldown = 0f;
                m.Attention = _mood.Current.Engagement;
            }

            _lastMoodLabel = _mood.Current.Label;
        }

        private void TickInvitation(float dt)
        {
            if (_inviteTimeLeft <= 0f) return;

            _inviteTimeLeft -= dt;
            if (_inviteTimeLeft <= 0f)
            {
                _inviteReaction = ReactionType.None;
                _inviteStrength = 0f;
            }
        }

        private void TickPending(float dt)
        {
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                PendingReaction p = _pending[i];
                p.Delay -= dt;

                if (p.Delay > 0f)
                {
                    _pending[i] = p;
                    continue;
                }

                _pending.RemoveAt(i);

                AudienceMember member = FindMember(p.MemberId);
                if (member != null) StartReaction(member, p.Reaction, p.Intensity);
            }
        }

        private void TickMember(AudienceMember m, float dt, in Mood mood)
        {
            m.SocialPressure = M.Damp(m.SocialPressure, 0f, _config.ContagionDecay, dt);

            m.WanderPhase += dt * 0.25f;
            float wander = (float)Math.Sin(m.WanderPhase) * 0.08f;
            float target = M.Clamp01(mood.Engagement * (0.55f + 0.45f * m.Personality.AttentionSpan) + wander);
            m.Attention = M.Damp(m.Attention, target, 1.2f, dt);

            if (m.ReactionTimeLeft > 0f)
            {
                m.ReactionTimeLeft -= dt;
                if (m.ReactionTimeLeft <= 0f)
                {
                    m.CurrentReaction = ReactionType.None;
                    m.ReactionIntensity = 0f;
                }
                return;
            }

            if (m.Cooldown > 0f)
            {
                m.Cooldown -= dt;
                return;
            }

            float spontaneous = _config.BaseReactionRate
                                * (0.35f + m.Personality.Expressiveness)
                                * (0.3f + m.Attention);

            float social = _config.ContagionGain
                           * m.SocialPressure
                           * m.Personality.Suggestibility
                           * (0.3f + 0.7f * m.Attention);

            float rate = spontaneous + social;

            bool invited = _inviteTimeLeft > 0f && _inviteReaction != ReactionType.None;
            if (invited)
            {
                rate += _inviteStrength * (0.4f + m.Personality.Enthusiasm * 0.8f);
            }

            if (!_rng.Chance(rate * dt)) return;

            ReactionType reaction = ReactionType.None;

            if (invited && _picker.AcceptsInvitation(_inviteReaction, mood, m.Personality))
            {
                reaction = _inviteReaction;
            }

            if (reaction == ReactionType.None)
            {
                reaction = _picker.Pick(mood, m.Personality);
            }

            if (reaction == ReactionType.None) return;

            float intensity = M.Clamp01(
                0.35f
                + m.Personality.Enthusiasm * 0.3f
                + m.Attention * 0.2f
                + m.SocialPressure * 0.25f);

            StartReaction(m, reaction, intensity);
        }

        private void StartReaction(AudienceMember m, ReactionType reaction, float intensity)
        {
            m.CurrentReaction = reaction;
            m.ReactionIntensity = intensity;
            m.ReactionTimeLeft = Reactions.DurationOf(reaction) * _rng.Range(0.85f, 1.2f);

            m.Cooldown = _config.MemberCooldown
                         * (1.4f - m.Personality.Expressiveness * 0.6f)
                         * _rng.Range(0.8f, 1.4f);

            Spread(m, reaction, intensity);

            _mood.ObserveReaction(reaction, intensity, _members.Count);
            _recent.Add(new TimedReaction(_time, reaction));

            Reacted?.Invoke(new ReactionEvent(m, reaction, intensity));
        }

        private void Spread(AudienceMember source, ReactionType reaction, float intensity)
        {
            float radius = _config.ContagionRadius;
            if (radius <= 0f || _config.ContagionGain <= 0f) return;

            float carry = Reactions.IsPositive(reaction) ? 1f : 0.5f;

            for (int i = 0; i < _members.Count; i++)
            {
                AudienceMember other = _members[i];
                if (ReferenceEquals(other, source)) continue;

                float distance = source.Seat.DistanceTo(other.Seat);
                if (distance >= radius) continue;

                float falloff = 1f - distance / radius;
                other.SocialPressure += falloff * falloff * intensity * carry;

                if (other.SocialPressure > _config.MaxSocialPressure)
                    other.SocialPressure = _config.MaxSocialPressure;
            }
        }

        private void TickWaves(float dt)
        {
            for (int i = 0; i < _waveCooldowns.Length; i++)
            {
                if (_waveCooldowns[i] > 0f) _waveCooldowns[i] -= dt;
            }

            float cutoff = _time - _config.WaveWindow;
            _recent.RemoveAll(r => r.Time < cutoff);

            if (_members.Count == 0 || Waved == null) return;

            int needed = (int)Math.Ceiling(_config.WaveThreshold * _members.Count);
            if (needed < 2) needed = 2;

            for (int i = 0; i < Reactions.All.Length; i++)
            {
                if (_waveCooldowns[i] > 0f) continue;

                ReactionType type = Reactions.All[i];

                int count = 0;
                for (int j = 0; j < _recent.Count; j++)
                {
                    if (_recent[j].Reaction == type) count++;
                }

                if (count < needed) continue;

                _waveCooldowns[i] = _config.WaveWindow * 2f;
                Waved.Invoke(new WaveEvent(type, count, count / (float)_members.Count));
            }
        }

        private void TickApplauseMeter(float dt)
        {
            if (_members.Count == 0)
            {
                _applauseMeter = 0f;
                return;
            }

            int positive = 0;
            for (int i = 0; i < _members.Count; i++)
            {
                if (_members[i].IsReacting && Reactions.IsPositive(_members[i].CurrentReaction)) positive++;
            }

            float target = M.Clamp01(positive / (_members.Count * 0.5f));
            _applauseMeter = M.Damp(_applauseMeter, target, 3f, dt);
        }

        private void TickMoodLabel()
        {
            RoomMood label = _mood.Current.Label;
            if (label == _lastMoodLabel) return;

            _lastMoodLabel = label;
            MoodChanged?.Invoke(label);
        }

        private AudienceMember FindMember(int id)
        {
            for (int i = 0; i < _members.Count; i++)
            {
                if (_members[i].Id == id) return _members[i];
            }

            return null;
        }

        private struct PendingReaction
        {
            public readonly int MemberId;
            public float Delay;
            public readonly ReactionType Reaction;
            public readonly float Intensity;

            public PendingReaction(int memberId, float delay, ReactionType reaction, float intensity)
            {
                MemberId = memberId;
                Delay = delay;
                Reaction = reaction;
                Intensity = intensity;
            }
        }

        private readonly struct TimedReaction
        {
            public readonly float Time;
            public readonly ReactionType Reaction;

            public TimedReaction(float time, ReactionType reaction)
            {
                Time = time;
                Reaction = reaction;
            }
        }
    }
}
