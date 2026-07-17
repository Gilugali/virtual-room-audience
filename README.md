# Virtual Room — Audience

A Unity package that puts a fake audience in your scene. The people clap, laugh, nod, and
applaud — or yawn and stop paying attention if you lose them.

Made for public-speaking apps. Works in VR, on desktop, and in the browser. Nothing is
networked — the people are simulated on your own machine. You don't need any art to start.

## Install

Window → Package Manager → **+** → _Add package from disk_ → pick `package.json`.

Needs Unity 2021.3 or newer.

## Add an audience

1. Make an empty GameObject **where the speaker stands**. The audience is built around it and
   faces it. In VR, that's the player's head.
2. Add the **Audience Room** component.
3. Pick a **Venue**: Theater, Classroom, Conference Hall, or Comedy Club.
4. Press Play.

To see everything the package does, import the **Quick Start** sample (Package Manager →
Samples → Import) and press the number keys.

## Using it from your code

Four things to know.

**Set the speaker's energy.** How much the speaker is giving the room, from 0 to 1. Set it every
frame. This is the main input — energy holds the room's attention, and attention drives
everything else.

**Send a cue** to tell the room what the speaker just did: `Greeting`, `Joke`, `KeyPoint`,
`Question`, `Pause`, `Stumble`, `Silence`, `StrongFinish`. A cue is an invitation, not an order —
a cold room will not laugh at a joke.

**Trigger a reaction** when the reaction comes from your own code or data instead of from the
speaker — a game event, a lesson script, a network message. This ignores the room's mood. You can
pass a reaction name as a string, which makes it easy to drive the crowd from a config file, a
webhook, or a `UnityEvent`.

**Read the room.** You can ask for the mood (Cold through Electric), how engaged people are, how
warm they feel toward the speaker, and the applause level — good for a UI meter or crowd volume.
There are also events for when one person reacts and when the whole room reacts at once.

```csharp
void Update()
{
    audience.Room.Speaker.Energy = myMicLoudness;
}

public void OnJokeLanded() => audience.Cue(SpeakerCue.Joke);
public void OnCheerButton() => audience.Trigger("Cheer");
```

## Making it yours

**Your own characters.** Set `memberPrefab` to any model — Ready Player Me, Mixamo, Synty, your
own. If it has an `Animator`, the package fires a trigger named after each reaction, so add clips
for the ones you want. If it doesn't, add an `AudienceMemberView` component, point it at the head
and arm bones, and the built-in animation drives them. The default characters are plain on
purpose — they exist so the package runs with no setup, not for shipping.

**Your own reaction visuals.** Set `reactionPopupPrefab` to anything with a `ReactionPopup`
component — sprites, particles, whatever. The default is floating text so the package needs no
assets.

**Sound.** Add the `AudienceAudio` component and assign clips. Crowd noise follows the applause
level.

**A real microphone.** Add a `MicrophoneEnergySource` and the room reacts to a real voice. This
does not work in WebGL — Unity has no microphone there — so in WebGL set the energy yourself.

**Tuning.** Settings are read live every tick, so you can change them in Play mode and watch the
crowd change. `ContagionGain` sets how much people copy each other: 0 and they ignore everyone,
3.5 and the room moves as one. `AllowNegativeReactions = false` gives you a kind room where
nobody yawns or checks out.

**Changing venue while running.** Call `audience.SetVenue(...)`. This rebuilds the crowd, so
subscribe to events on the **component** rather than on `Room` and your handlers will survive it.

**Without Unity.** `Runtime/Core` and `Runtime/Simulation` are plain C# with no Unity in them. You
can reference them from any C# project, make an `AudienceRoom`, and call `Tick` yourself each
frame.

## How it works

**Contagion.** When someone reacts, they nudge the people near them, strongest on the closest.
Those people become more likely to react, and they nudge their own neighbours. So applause spreads
outward from one person instead of everyone rolling dice on their own.

**Two kinds of mood.** Engagement (are they listening?) and warmth (do they like you?) are tracked
separately, because they're different rooms — bored but friendly, or attentive but cold.
Engagement moves fast and drains as soon as energy drops. Warmth moves slowly and is earned over
minutes.

## Tests

The simulation is seeded, so the same seed gives the same room every time. The whole crowd runs
headlessly in about a second:

```bash
cd Tests~/Headless
dotnet run
```

## Browser preview

A 3D classroom seen from the podium, with microphone input. It runs the same simulation as Unity,
compiled to WebAssembly. See [`WebPreview~/README.md`](WebPreview~/README.md).

## Layout

```
Runtime/
  Core/         Public API. Plain C#, no Unity. AudienceRoom is the entry point.
  Simulation/   Mood, contagion, and reaction picking.
  Unity/        Components, avatars, popups, mic, audio.
Samples~/       Quick Start sample.
Tests~/         Headless checks.
```
