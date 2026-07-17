# Web preview

A 3D classroom, seen from the podium, full of people who react to you. In your browser, with no Unity.

**This is not a mock.** It compiles `../Runtime/Core` and `../Runtime/Simulation` — the actual
simulation — to WebAssembly and runs it. Who claps, when, how hard, and how it spreads is decided by
exactly the same C# the Unity package runs. `scene.js` only *draws*; it decides nothing.

That's the whole point of keeping those folders free of `UnityEngine`.

## Run it

```bash
cd WebPreview~
dotnet run                       # → http://localhost:5000
```

If `dotnet run` complains about a missing **Microsoft.AspNetCore.App** runtime, your SDK doesn't ship
the dev server's exact runtime version. You don't need it — publish a static site and serve that:

```bash
dotnet publish -c Release -o out
node serve.js ./out/wwwroot 5080  # → http://localhost:5080
```

(`serve.js` exists because Blazor won't boot unless `.wasm` is served as `application/wasm`, which
`python -m http.server` gets wrong.)

## What to try

- **Drag the view** to look around the room, like turning your head at a podium.
- **🎙 Use my microphone**, then speak. The room reacts to your actual voice — get loud and heads
  lift, trail off and you watch them sink, one at a time. Browsers only allow mic access on
  `localhost` or https, so this works locally out of the box.
- **Strong finish**, then switch to **Room view** and do it again. From above you can see the
  applause *spread* — it starts somewhere and travels, instead of everyone clapping on the same frame.
- **Drag Contagion to 0.** The spread stops dead and it becomes 24 people clapping on their own
  private schedules. The room stops feeling like a room within seconds. That one number is the
  difference between a crowd and a spreadsheet.
- **Dead air.** Stop giving them anything and watch them fold forward and turn away.

## The avatars

The people in `models/` are **real rigged 3D characters** — eight of them, from Quaternius' *Ultimate
Modular* packs, [CC0](https://quaternius.com) (public domain: use them for anything, no attribution).
Past a certain level of fidelity the geometry *is* the art: you cannot tune your way to a face with a
jaw and a hairline out of spheres and capsules, so at that point you stop sculpting with code and
load a model.

They arrive standing in a T-pose with **no animation clips at all**, so everything you see them do —
sitting down, resting, clapping, raising a hand, stretching into a yawn — is built in `scene.js` by
rotating their bones. Two things make that tractable:

- **Every pose is written in the character's own frame** (+Z is where they're facing), and `pose()`
  converts it into whatever private frame each bone happens to have. Arm bones are rolled to point
  down the limb, so reasoning in bone-local space is how you write a clap that comes out as a shrug.
- **`BONES` maps each slot to the names every pipeline uses** — Quaternius, Mixamo, Ready Player Me,
  Unreal. That's what makes the next section a two-line change instead of a rewrite.

### Using your own characters

This is the seam. If you want a specific look — the Pixar-ish stylised kids that AI 3D generators
(Meshy, Tripo, Rodin) produce from a reference image, or a paid pack like Synty — the pipeline
already takes them:

1. Export as **`.glb`, rigged**, and drop it in `models/`.
2. Add its filename to `MODEL_FILES` at the top of the People section in `scene.js`.

That's it. Height, hip position and bone names are all *measured from the model at load*, not assumed,
so a character exported at 2 units tall with Mixamo bone names sits down at the right desk with no
tuning. If a model uses bone names nothing here recognises, it says so by name instead of quietly
seating a statue.

**Unity/VR** takes the same models a different way: set `memberPrefab` on the `AudienceRoomBehaviour`.
If your model has an `Animator`, the package fires a trigger named after each reaction (`"Applause"`,
`"Laugh"`, `"Yawn"`…), so you get real animation clips with no changes to the room code. Leave it
empty and you get the built-in cartoon people, built from primitives, no art needed.

Nothing about the simulation changes when you swap any of this. The crowd brain and the crowd's face
are deliberately separate things.

## The catch

This previews the *simulation and the room*, not the Unity scene. There's no VR here. But every
number, every behaviour, and every event you see is what your Unity build will do — because it's the
same code.
