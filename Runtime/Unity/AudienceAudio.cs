using UnityEngine;

namespace VirtualRoom.Audience.Unity
{
    [AddComponentMenu("Virtual Room/Audience Audio")]
    [RequireComponent(typeof(AudienceRoomBehaviour))]
    public class AudienceAudio : MonoBehaviour
    {
        [Header("Crowd bed")]
        [Tooltip("Looping crowd noise. Its volume follows the applause meter, so the room gets louder as it warms up.")]
        public AudioClip ambience;

        [Range(0f, 1f)] public float ambienceVolume = 0.5f;

        [Tooltip("How much crowd noise you hear from a completely dead room.")]
        [Range(0f, 1f)] public float ambienceFloor = 0.08f;

        [Header("Big moments (played when the whole room does something at once)")]
        public AudioClip applauseWave;
        public AudioClip cheerWave;
        public AudioClip laughWave;

        [Tooltip("Played for negative waves, such as a mass yawn or a wave of confusion.")]
        public AudioClip negativeWave;

        private AudienceRoomBehaviour _room;
        private AudioSource _bed;
        private AudioSource _oneShots;

        private void Awake()
        {
            _room = GetComponent<AudienceRoomBehaviour>();

            _bed = gameObject.AddComponent<AudioSource>();
            _bed.loop = true;
            _bed.playOnAwake = false;
            _bed.spatialBlend = 0f;
            _bed.volume = 0f;

            _oneShots = gameObject.AddComponent<AudioSource>();
            _oneShots.loop = false;
            _oneShots.playOnAwake = false;
            _oneShots.spatialBlend = 0f;
        }

        private bool _subscribed;

        private void OnEnable()
        {
            TrySubscribe();

            if (ambience != null)
            {
                _bed.clip = ambience;
                _bed.Play();
            }
        }

        private void Start() => TrySubscribe();

        private void TrySubscribe()
        {
            if (_subscribed || _room?.Room == null) return;

            _room.Room.Waved += OnWaved;
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (_subscribed && _room?.Room != null) _room.Room.Waved -= OnWaved;
            _subscribed = false;

            _bed.Stop();
        }

        private void Update()
        {
            if (_room?.Room == null || ambience == null) return;

            TrySubscribe();

            float target = Mathf.Lerp(ambienceFloor, 1f, _room.ApplauseMeter) * ambienceVolume;
            _bed.volume = Mathf.Lerp(_bed.volume, target, 1f - Mathf.Exp(-4f * Time.deltaTime));
        }

        private void OnWaved(WaveEvent wave)
        {
            AudioClip clip = ClipFor(wave.Reaction);
            if (clip == null) return;

            _oneShots.PlayOneShot(clip, Mathf.Clamp01(0.35f + wave.Fraction));
        }

        private AudioClip ClipFor(ReactionType reaction)
        {
            switch (reaction)
            {
                case ReactionType.Applause: return applauseWave;
                case ReactionType.Cheer: return cheerWave;
                case ReactionType.Laugh: return laughWave;
                default: return Reactions.IsNegative(reaction) ? negativeWave : null;
            }
        }
    }
}
