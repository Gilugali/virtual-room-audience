# Web preview

A 3D classroom seen from the podium, in your browser, with no Unity.

It runs the **real simulation**. `../Runtime/Core` and `../Runtime/Simulation` are compiled to
WebAssembly, so who claps, when, and how it spreads is decided by the same C# the Unity package
runs. `scene.js` only draws.

## Run it

```bash
cd WebPreview~
dotnet run                       # → http://localhost:5000
```

**If it complains about a missing Microsoft.AspNetCore.App runtime**, your SDK doesn't have the dev
server's runtime version. You don't need it. Publish a static site instead:

```bash
dotnet publish -c Release -o out
node serve.js ./out/wwwroot 5080  # → http://localhost:5080
```

Use `serve.js`, not `python -m http.server`. Blazor won't boot unless `.wasm` is served as
`application/wasm`, and python gets that wrong.

## What to try

1. **Drag the view.** Look around the room, like turning your head at a podium.
2. **Click 🎙 Use my microphone, then speak.** The room reacts to your real voice. Get loud and
   heads lift. Trail off and they sink, one at a time.
3. **Click Strong finish, switch to Room view, do it again.** From above you can see the applause
   *spread*. It starts somewhere and travels, instead of everyone clapping on the same frame.
4. **Drag Contagion to 0.** The spread stops dead. Now it's 24 people clapping on their own private
   schedules, and the room stops feeling like a room.
5. **Stop talking.** Watch them fold forward and turn away.

Mic only works on `localhost` or https. That's a browser rule. Locally it just works.

## Using your own characters

1. Export your model as **`.glb`, rigged**.
2. Drop it in `wwwroot/models/`.
3. Add the filename to `MODEL_FILES` in `wwwroot/scene.js` (around line 203).

That's it. Height, hip position, and bone names are **measured from the model at load**, not
assumed. So a character exported at 2 units tall with Mixamo bone names sits at the right desk with
no tuning. If a model uses bone names nothing recognises, it tells you the name instead of quietly
seating a statue.

Works with Meshy, Tripo, or Rodin output, plus Mixamo, Ready Player Me, Synty, or your own.

**In Unity** you do it differently. Set `memberPrefab` on the `AudienceRoomBehaviour`. If the model
has an `Animator`, the package fires a trigger named after each reaction (`"Applause"`, `"Laugh"`,
`"Yawn"`). Leave it empty and you get the built-in plain characters.

Swapping characters never changes the simulation.

## About the avatars

The eight people in `wwwroot/models/` are real rigged characters from Quaternius' *Ultimate
Modular* packs, [CC0](https://quaternius.com). Public domain. Use them for anything, no
attribution needed.

They ship in a T-pose with **no animation clips**. Everything they do is built in `scene.js` by
rotating bones: sitting, clapping, raising a hand, yawning. Two things make that work:

- **Poses are written in the character's own frame** (+Z is where they face), and `pose()` converts
  that to whatever frame each bone actually has. Without this, a clap comes out as a shrug.
- **`BONES` maps each slot to the names every pipeline uses**: Quaternius, Mixamo, Ready Player Me,
  Unreal. That's what makes adding a model a two-line change.

## The catch

This previews the **simulation and the room**, not the Unity scene. There is no VR here. But every
number and behaviour you see is what your Unity build does, because it's the same code.
