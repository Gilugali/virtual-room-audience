# Virtual Room — Audience

A Unity package that puts a fake audience in your scene. They clap, laugh, nod, and cheer. Lose
them and they yawn and stop listening.

For public-speaking apps. Works in VR, desktop, and browser. No art needed to start.

## 1. Install

1. Window → Package Manager
2. Click **+** → _Add package from disk_
3. Pick `package.json`

Needs Unity 2021.3 or newer.

## 2. Add an audience

1. Make an empty GameObject **where the speaker stands**. In VR, that's the player's head.
2. Add the **Audience Room** component.
3. Pick a **Venue**: Theater, Classroom, Conference Hall, or Comedy Club.
4. Press Play.

The audience is built around that GameObject and faces it. That's why step 1 matters.

Want to see everything first? Package Manager → Samples → Import **Quick Start**, press Play, then
press the number keys.

## 3. Drive it from code

**Step 1. Set the energy every frame.** A value from 0 to 1. How much the speaker is giving the
room.

```csharp
audience.Room.Speaker.Energy = myMicLoudness;
```

This is the main input. Energy holds attention. Attention drives everything else.

**Step 2. Send a cue when the speaker does something.**

```csharp
audience.Cue(SpeakerCue.Joke);
```

Cues: `Greeting`, `Joke`, `KeyPoint`, `Question`, `Pause`, `Stumble`, `Silence`, `StrongFinish`.

A cue is an invitation, not an order. A cold room will not laugh at a joke.

**Step 3. Or trigger a reaction directly.**

```csharp
audience.Trigger("Cheer");
```

Use this when the reaction comes from your code instead of the speaker. A game event, a lesson
script, a network message. It ignores the room's mood.

Names: `Applause`, `Cheer`, `Laugh`, `Nod`, `Wow`, `Love`, `ThumbsUp`, `Question`, `Confused`,
`Yawn`, `Distracted`.

**Step 4. Read the room.**

```csharp
audience.Room.MoodLabel        // Cold to Electric
audience.Room.ApplauseMeter    // 0..1, good for a UI meter or crowd volume
audience.Waved += w => ...;    // the whole room reacted at once
```

## Common jobs

**Use your own characters**

1. Set `memberPrefab` to any model. Ready Player Me, Mixamo, Synty, your own.
2. Has an `Animator`? The package fires a trigger named after each reaction. Add clips for the
   ones you want.
3. No `Animator`? Add `AudienceMemberView` and point it at the head and arm bones. Built-in
   animation drives them.

The default characters are plain on purpose, so the package runs with zero setup.

**Add sound**

1. Add the `AudienceAudio` component.
2. Assign clips. Crowd noise follows the applause meter.

**Use a real microphone**

1. Add a `MicrophoneEnergySource`.

Not in WebGL. Unity has no microphone there, so set energy yourself.

**Change venue while running**

```csharp
audience.SetVenue(Venue.Classroom);
```

This rebuilds the crowd. Subscribe to events on the **component**, not on `Room`, or your handlers
get lost in the swap.

**Use your own reaction visuals**

Set `reactionPopupPrefab` to anything with a `ReactionPopup` component. Default is floating text.

**Use it without Unity**

`Runtime/Core` and `Runtime/Simulation` are plain C# with no Unity. Make an `AudienceRoom` and call
`Tick(deltaTime)` each frame.

## Tuning

Settings are read live, so you can change them in Play mode and watch the crowd change.

| Setting                  | Do this                                            |
| ------------------------ | -------------------------------------------------- |
| `ContagionGain`          | `0` = people ignore each other. `2` = normal. `3.5` = room moves as one. |
| `AllowNegativeReactions` | `false` = kind room, nobody yawns or checks out.   |
| `Size`                   | How many people. Only applies when the room is built. |

## How it works

**Contagion.** Someone reacts. They nudge the people near them. Those people are now more likely
to react, and they nudge their own neighbours. So applause spreads out from one person, instead of
everyone rolling dice alone.

**Two moods.** Engagement asks "are they listening?". Warmth asks "do they like you?". They are
separate because they are different rooms. Bored but friendly. Attentive but cold. Engagement
moves fast and drops as soon as energy drops. Warmth moves slowly and takes minutes to earn.

## Tests

```bash
cd Tests~/Headless
dotnet run
```

Seeded. Same seed, same room, every time.

## Browser preview

A 3D classroom seen from the podium, with mic input. Same simulation as Unity, compiled to
WebAssembly. See [`WebPreview~/README.md`](WebPreview~/README.md).

## Layout

```
Runtime/
  Core/         Public API. Plain C#, no Unity. AudienceRoom is the entry point.
  Simulation/   Mood, contagion, reaction picking.
  Unity/        Components, avatars, popups, mic, audio.
Samples~/       Quick Start sample.
Tests~/         Headless checks.
```
