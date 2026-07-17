using UnityEngine;

namespace VirtualRoom.Audience.Unity
{
    public abstract class SpeakerEnergySource : MonoBehaviour
    {
        public abstract float GetEnergy();
    }

    [AddComponentMenu("Virtual Room/Speaker Energy (Manual Slider)")]
    public sealed class ManualEnergySource : SpeakerEnergySource
    {
        [Range(0f, 1f)]
        [Tooltip("Speaker energy, 0..1. Adjustable in Play mode.")]
        public float energy = 0.5f;

        public override float GetEnergy() => energy;
    }

#if !UNITY_WEBGL || UNITY_EDITOR
    [AddComponentMenu("Virtual Room/Speaker Energy (Microphone)")]
    public sealed class MicrophoneEnergySource : SpeakerEnergySource
    {
        [Tooltip("Leave blank to use the system default microphone.")]
        [SerializeField] private string device = "";

        [Tooltip("How much quiet counts as loud. Raise this if the room ignores you.")]
        [SerializeField] private float sensitivity = 12f;

        [Tooltip("Anything below this is treated as silence, so room hum doesn't hold their attention.")]
        [SerializeField] private float noiseFloor = 0.006f;

        [Tooltip("How fast the energy value chases your voice. Lower = smoother, laggier.")]
        [SerializeField] private float smoothing = 8f;

        private const int SampleWindow = 256;

        private readonly float[] _samples = new float[SampleWindow];
        private AudioClip _clip;
        private string _activeDevice;
        private float _energy;

        public bool IsListening => _clip != null;

        public float RawLoudness { get; private set; }

        private void OnEnable()
        {
            if (Microphone.devices.Length == 0)
            {
                Debug.LogWarning("[VirtualRoom] No microphone found — the audience won't hear you. " +
                                 "Use a ManualEnergySource, or set Room.Speaker.Energy yourself.", this);
                return;
            }

            _activeDevice = string.IsNullOrEmpty(device) ? Microphone.devices[0] : device;
            _clip = Microphone.Start(_activeDevice, loop: true, lengthSec: 1, frequency: AudioSettings.outputSampleRate);
        }

        private void OnDisable()
        {
            if (_activeDevice != null) Microphone.End(_activeDevice);
            _clip = null;
            _energy = 0f;
        }

        private void Update()
        {
            RawLoudness = Sample();
            _energy = Mathf.Lerp(_energy, RawLoudness, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
        }

        private float Sample()
        {
            if (_clip == null) return 0f;

            int position = Microphone.GetPosition(_activeDevice) - SampleWindow;
            if (position < 0) return 0f;

            _clip.GetData(_samples, position);

            float sum = 0f;
            for (int i = 0; i < SampleWindow; i++) sum += _samples[i] * _samples[i];

            float rms = Mathf.Sqrt(sum / SampleWindow);
            return Mathf.Clamp01((rms - noiseFloor) * sensitivity);
        }

        public override float GetEnergy() => _energy;
    }
#endif
}
