using UnityEngine;
using VirtualRoom.Audience;
using VirtualRoom.Audience.Unity;

namespace VirtualRoom.Audience.Samples
{
    [RequireComponent(typeof(AudienceRoomBehaviour))]
    public class AudienceDemo : MonoBehaviour
    {
        private AudienceRoomBehaviour _audience;

        [Tooltip("With no microphone, hold SPACE to pretend you're speaking with energy.")]
        [SerializeField] private float fakeEnergyWhileSpeaking = 0.85f;
        [SerializeField] private float fakeEnergyWhileQuiet = 0.15f;

        private void Start()
        {
            _audience = GetComponent<AudienceRoomBehaviour>();

            _audience.Reacted += e =>
                Debug.Log($"Person #{e.Member.Id} did: {e.Reaction} (intensity {e.Intensity:0.00})");

            _audience.Waved += w =>
                Debug.Log($"<color=yellow>*** THE ROOM {w.Reaction.ToString().ToUpper()}S *** " +
                          $"({w.Count} people, {w.Fraction:P0} of the room)</color>");

            _audience.MoodChanged += mood =>
                Debug.Log($"<color=cyan>The room is now: {mood}</color>");

            _audience.Cue(SpeakerCue.Greeting);
        }

        private void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            _audience.manualEnergy = Input.GetKey(KeyCode.Space)
                ? fakeEnergyWhileSpeaking
                : fakeEnergyWhileQuiet;

            if (Input.GetKeyDown(KeyCode.Alpha1)) _audience.Cue(SpeakerCue.Joke);
            if (Input.GetKeyDown(KeyCode.Alpha2)) _audience.Cue(SpeakerCue.KeyPoint);
            if (Input.GetKeyDown(KeyCode.Alpha3)) _audience.Cue(SpeakerCue.Question);
            if (Input.GetKeyDown(KeyCode.Alpha4)) _audience.Cue(SpeakerCue.Stumble);
            if (Input.GetKeyDown(KeyCode.Alpha5)) _audience.Cue(SpeakerCue.StrongFinish);

            if (Input.GetKeyDown(KeyCode.C)) _audience.CheerNow();
            if (Input.GetKeyDown(KeyCode.A)) _audience.ApplaudNow();

            if (Input.GetKeyDown(KeyCode.L)) _audience.Trigger(ReactionType.Love, spread: 0.6f);
            if (Input.GetKeyDown(KeyCode.Q)) _audience.Trigger("Question");

            if (Input.GetKeyDown(KeyCode.V)) _audience.SetVenue(NextVenue(_audience.venue));

            if (Input.GetKeyDown(KeyCode.R)) _audience.ResetRoom();
#endif
        }

        private void OnGUI()
        {
            AudienceRoom room = _audience != null ? _audience.Room : null;
            if (room == null) return;

            GUI.Box(new Rect(10, 10, 320, 150), "");

            GUILayout.BeginArea(new Rect(22, 20, 300, 140));

            GUILayout.Label($"<b>{_audience.venue} — the room is: {room.MoodLabel}</b>", Style());
            GUILayout.Label($"Attention:  {Bar(room.Mood.Engagement)}", Style());
            GUILayout.Label($"They like you:  {Bar(room.Mood.Warmth)}", Style());
            GUILayout.Label($"Applause:  {Bar(room.ApplauseMeter)}", Style());
            GUILayout.Space(6);
            GUILayout.Label("<i>SPACE</i> speak   <i>1</i> joke  <i>2</i> point  <i>3</i> ask  <i>4</i> stumble  <i>5</i> finish", Style());
            GUILayout.Label("<i>A</i> applaud  <i>C</i> cheer  <i>L</i> love  <i>Q</i> question   <i>V</i> venue   <i>R</i> reset", Style());

            GUILayout.EndArea();
        }

        private static Venue NextVenue(Venue current)
        {
            int i = System.Array.IndexOf(Venues.All, current);
            return Venues.All[(i + 1) % Venues.All.Length];
        }

        private static string Bar(float value)
        {
            int filled = Mathf.RoundToInt(Mathf.Clamp01(value) * 20f);
            return new string('|', filled).PadRight(20, '.') + $" {value:P0}";
        }

        private static GUIStyle Style() => new GUIStyle(GUI.skin.label) { richText = true };
    }
}
