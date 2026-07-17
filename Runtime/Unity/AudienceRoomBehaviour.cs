using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VirtualRoom.Audience.Unity
{
    [System.Serializable] public class ReactionUnityEvent : UnityEvent<ReactionType> { }
    [System.Serializable] public class WaveUnityEvent : UnityEvent<ReactionType, int> { }
    [System.Serializable] public class MoodUnityEvent : UnityEvent<RoomMood> { }

    [AddComponentMenu("Virtual Room/Audience Room")]
    [DisallowMultipleComponent]
    public class AudienceRoomBehaviour : MonoBehaviour
    {
        [Header("The room")]
        [Tooltip("Pick a venue and its size, seating and mood are applied on Play, overwriting the fields below. Untick to hand-tune instead.")]
        public bool useVenue = true;

        [Tooltip("Which kind of room. Theater, Classroom, Conference hall, or Comedy club.")]
        public Venue venue = Venue.Theater;

        public RoomConfig config = new RoomConfig();
        public SeatingSettings seating = new SeatingSettings();

        [Header("Visuals")]
        [Tooltip("Leave empty to build audience members from primitives at runtime.")]
        public GameObject memberPrefab;

        [Tooltip("Leave empty for a plain floating-text popup.")]
        public ReactionPopup reactionPopupPrefab;

        public bool showReactionPopups = true;

        [Tooltip("Who the popups turn to face. Defaults to Camera.main — set this to the VR head.")]
        public Camera viewer;

        [Header("Speaker input")]
        [Tooltip("Where 'how much energy is the speaker giving' comes from. Auto-found on this object if left empty.")]
        public SpeakerEnergySource energySource;

        [Range(0f, 1f)]
        [Tooltip("Used only when there's no energy source at all.")]
        public float manualEnergy = 0.5f;

        [Header("Events (wire these up in the Inspector)")]
        public ReactionUnityEvent onReaction;

        [Tooltip("The whole room did something at once. Hook your crowd-roar audio here.")]
        public WaveUnityEvent onWave;

        public MoodUnityEvent onMoodChanged;

        public AudienceRoom Room { get; private set; }

        public event System.Action<ReactionEvent> Reacted;

        public event System.Action<WaveEvent> Waved;

        public event System.Action<RoomMood> MoodChanged;

        public float ApplauseMeter => Room?.ApplauseMeter ?? 0f;

        public RoomMood MoodLabel => Room?.MoodLabel ?? RoomMood.Polite;

        private readonly Dictionary<int, AudienceMemberView> _views = new Dictionary<int, AudienceMemberView>();
        private Transform _audienceRoot;

        private void Awake()
        {
            if (energySource == null) energySource = GetComponent<SpeakerEnergySource>();
            if (viewer == null) viewer = Camera.main;

            _audienceRoot = new GameObject("Audience").transform;
            _audienceRoot.SetParent(transform, false);

            if (useVenue)
            {
                VenuePreset preset = Venues.Preset(venue);
                config = preset.Config;
                seating = preset.Seating;
            }

            Room = new AudienceRoom(config, seating);

            Room.MemberJoined += OnMemberJoined;
            Room.MemberLeft += OnMemberLeft;
            Room.Reacted += OnReacted;
            Room.Waved += OnWaved;
            Room.MoodChanged += OnMoodChangedInternal;

            foreach (AudienceMember member in Room.Members) OnMemberJoined(member);
        }

        private void OnDestroy()
        {
            if (Room == null) return;

            Room.MemberJoined -= OnMemberJoined;
            Room.MemberLeft -= OnMemberLeft;
            Room.Reacted -= OnReacted;
            Room.Waved -= OnWaved;
            Room.MoodChanged -= OnMoodChangedInternal;
        }

        private void Update()
        {
            if (Room == null) return;

            Room.Speaker.Energy = energySource != null ? energySource.GetEnergy() : manualEnergy;
            Room.Tick(Time.deltaTime);
        }

        public void Cue(SpeakerCue cue) => Room?.Cue(cue);

        public void CueGreeting() => Cue(SpeakerCue.Greeting);
        public void CueJoke() => Cue(SpeakerCue.Joke);
        public void CueKeyPoint() => Cue(SpeakerCue.KeyPoint);
        public void CueQuestion() => Cue(SpeakerCue.Question);
        public void CuePause() => Cue(SpeakerCue.Pause);
        public void CueStumble() => Cue(SpeakerCue.Stumble);
        public void CueSilence() => Cue(SpeakerCue.Silence);
        public void CueStrongFinish() => Cue(SpeakerCue.StrongFinish);

        public void Trigger(ReactionType reaction, float intensity = 1f, float spread = 1f)
            => Room?.TriggerReaction(reaction, intensity, spread);

        public bool Trigger(string reaction, float intensity = 1f, float spread = 1f)
            => Room?.TriggerReaction(reaction, intensity, spread) ?? false;

        public void SetVenue(Venue newVenue)
        {
            venue = newVenue;
            useVenue = true;

            VenuePreset preset = Venues.Preset(newVenue);
            config = preset.Config;
            seating = preset.Seating;

            if (Room == null) return;

            Room.MemberJoined -= OnMemberJoined;
            Room.MemberLeft -= OnMemberLeft;
            Room.Reacted -= OnReacted;
            Room.Waved -= OnWaved;
            Room.MoodChanged -= OnMoodChangedInternal;

            foreach (AudienceMemberView view in _views.Values)
                if (view != null) Destroy(view.gameObject);
            _views.Clear();

            Room = new AudienceRoom(config, seating);
            Room.MemberJoined += OnMemberJoined;
            Room.MemberLeft += OnMemberLeft;
            Room.Reacted += OnReacted;
            Room.Waved += OnWaved;
            Room.MoodChanged += OnMoodChangedInternal;

            foreach (AudienceMember member in Room.Members) OnMemberJoined(member);
        }

        public void ApplaudNow() => Room?.Applaud();

        public void CheerNow() => Room?.Cheer();

        public void ResetRoom() => Room?.Reset();

        public void Repopulate(int size)
        {
            config.Size = size;
            Room?.Populate(size);
        }

        private void OnMemberJoined(AudienceMember member)
        {
            AudienceMemberView view;

            if (memberPrefab != null)
            {
                GameObject instance = Instantiate(memberPrefab, _audienceRoot);

                view = instance.GetComponent<AudienceMemberView>();
                if (view == null) view = instance.AddComponent<AudienceMemberView>();
            }
            else
            {
                view = ProceduralAvatar.Build(_audienceRoot, member.Id);
            }

            view.Member = member;
            view.name = $"Audience Member {member.Id}";

            _views[member.Id] = view;

            SyncSeats();
        }

        private void SyncSeats()
        {
            foreach (AudienceMember member in Room.Members)
            {
                if (!_views.TryGetValue(member.Id, out AudienceMemberView view) || view == null) continue;

                view.transform.localPosition = new Vector3(member.Seat.X, member.Seat.Y, member.Seat.Z);
                view.transform.localRotation = Quaternion.Euler(0f, member.Seat.FacingRadians * Mathf.Rad2Deg, 0f);
            }
        }

        private void OnMemberLeft(AudienceMember member)
        {
            if (!_views.TryGetValue(member.Id, out AudienceMemberView view)) return;

            _views.Remove(member.Id);
            if (view != null) Destroy(view.gameObject);
        }

        private void OnReacted(ReactionEvent e)
        {
            if (_views.TryGetValue(e.Member.Id, out AudienceMemberView view) && view != null)
            {
                view.OnReacted(e.Reaction, e.Intensity);
            }

            Reacted?.Invoke(e);
            onReaction?.Invoke(e.Reaction);
        }

        private void OnWaved(WaveEvent e)
        {
            Waved?.Invoke(e);
            onWave?.Invoke(e.Reaction, e.Count);
        }

        private void OnMoodChangedInternal(RoomMood mood)
        {
            MoodChanged?.Invoke(mood);
            onMoodChanged?.Invoke(mood);
        }

        internal ReactionPopup CreatePopup(Vector3 worldPosition)
        {
            if (reactionPopupPrefab != null)
            {
                return Instantiate(reactionPopupPrefab, worldPosition, Quaternion.identity, _audienceRoot);
            }

            return DefaultPopup.Create(worldPosition, _audienceRoot);
        }

        private void OnDrawGizmosSelected()
        {
            RoomConfig previewConfig = config;
            SeatingSettings previewSeating = seating;
            if (useVenue)
            {
                VenuePreset preset = Venues.Preset(venue);
                previewConfig = preset.Config;
                previewSeating = preset.Seating;
            }

            SeatPosition[] seats = SeatingLayout.Build(previewConfig.Size, previewSeating, previewConfig.Seed);

            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.6f);
            foreach (SeatPosition seat in seats)
            {
                Vector3 world = transform.TransformPoint(new Vector3(seat.X, seat.Y, seat.Z));
                Gizmos.DrawWireSphere(world, 0.25f);
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.4f);
            Gizmos.DrawRay(transform.position, transform.forward * 1.5f);
        }
    }
}
