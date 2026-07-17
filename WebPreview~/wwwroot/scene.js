// Rendering only. All behaviour comes from the C# simulation (Runtime/Core, Runtime/Simulation),
// which pushes per-member state into update() each frame.
//
// Characters are rigged glTF models in ./models with no animation clips — every pose here is built
// by rotating bones.

import * as THREE from "./vendor/three.module.min.js";
import { GLTFLoader } from "./vendor/GLTFLoader.js";
import * as SkeletonUtils from "./vendor/SkeletonUtils.js";

// Must match VirtualRoom.Audience.ReactionType.
const R = {
  None: 0, Applause: 1, Cheer: 2, Laugh: 3, Nod: 4, Wow: 5, Love: 6,
  ThumbsUp: 7, Question: 8, Confused: 9, Yawn: 10, Distracted: 11,
};

const GLYPH = {
  [R.Applause]: "👏", [R.Cheer]: "🙌", [R.Laugh]: "😂", [R.Nod]: "🙂",
  [R.Wow]: "😮", [R.Love]: "❤️", [R.ThumbsUp]: "👍", [R.Question]: "🙋",
  [R.Confused]: "🤨", [R.Yawn]: "🥱", [R.Distracted]: "📱",
};

const CEILING = 3.9;   // metres; the overview camera must stay below this

let renderer, scene, camera, clock;
let audience = [];      // one Person per simulated member, same order as Room.Members
let holders = [];       // one chair-seat group per member: person + their desk and chair
let popups = [];
let canvas;
let raf = 0;

// Models load async, so seat() can be called before they exist.
let modelsReady = false;
let pendingSeats = null;

const view = {
  mode: "podium",
  yaw: 0,
  pitch: -0.04,
  dragging: false,
  lastX: 0,
  lastY: 0,
};

// ---------------------------------------------------------------------------
// Building the room
// ---------------------------------------------------------------------------

function buildRoom() {
  scene = new THREE.Scene();
  scene.background = new THREE.Color(0xbfc6cf);
  scene.fog = new THREE.Fog(0xbfc6cf, 24, 50);

  scene.add(new THREE.HemisphereLight(0xf2f6ff, 0x6d7480, 1.15));
  scene.add(new THREE.AmbientLight(0xffffff, 0.45));

  // Key light sits at the front of the room, behind the speaker, so it hits the audience's faces.
  const key = new THREE.DirectionalLight(0xfff2e0, 2.4);
  key.position.set(-6, 9, -9);
  key.castShadow = true;
  key.shadow.mapSize.set(2048, 2048);
  key.shadow.camera.near = 1;
  key.shadow.camera.far = 40;
  key.shadow.camera.left = -18;
  key.shadow.camera.right = 18;
  key.shadow.camera.top = 18;
  key.shadow.camera.bottom = -18;
  key.shadow.bias = -0.0012;
  scene.add(key);

  const floor = new THREE.Mesh(
    new THREE.PlaneGeometry(40, 40),
    new THREE.MeshStandardMaterial({ color: 0xdfe2e6, roughness: 0.9, metalness: 0 })
  );
  floor.rotation.x = -Math.PI / 2;
  floor.receiveShadow = true;
  scene.add(floor);

  const grid = new THREE.GridHelper(40, 40, 0xc0c6cc, 0xd3d8dd);
  grid.position.y = 0.003;
  scene.add(grid);

  const wallMat = new THREE.MeshStandardMaterial({ color: 0xeceef0, roughness: 0.97 });

  // Walls and ceiling pass cast=false — see addBox.
  addBox(scene, 24, CEILING, 0.2, 0, CEILING / 2, 16, wallMat, false);     // back
  addBox(scene, 0.2, CEILING, 22, -11, CEILING / 2, 5, wallMat, false);    // left
  addBox(scene, 0.2, CEILING, 22, 11, CEILING / 2, 5, wallMat, false);     // right
  addBox(scene, 24, CEILING, 0.2, 0, CEILING / 2, -6, wallMat, false);     // front (behind the podium)
  // Emissive: the ceiling's underside faces away from every light, so it renders near-black without it.
  addBox(scene, 24, 0.2, 22, 0, CEILING, 5,
    new THREE.MeshStandardMaterial({
      color: 0xf6f7f8, roughness: 1, emissive: 0xb9c0c8, emissiveIntensity: 0.55,
    }), false);

  // Ceiling light panels.
  const panel = new THREE.MeshStandardMaterial({
    color: 0xffffff, emissive: 0xfff6e2, emissiveIntensity: 1.4, roughness: 1,
  });
  for (const px of [-4.5, 4.5]) {
    for (const pz of [1, 6, 11]) {
      addBox(scene, 2.2, 0.05, 0.7, px, CEILING - 0.06, pz, panel, false);
    }
  }

  // Skirting boards.
  const skirt = new THREE.MeshStandardMaterial({ color: 0x3a4048 });
  addBox(scene, 24, 0.12, 0.06, 0, 0.06, 15.88, skirt, false);
  addBox(scene, 0.06, 0.12, 22, -10.88, 0.06, 5, skirt, false);
  addBox(scene, 0.06, 0.12, 22, 10.88, 0.06, 5, skirt, false);

  buildWhiteboard();
  buildPodium();
}

/** Whiteboard and projector screen on the front wall. */
function buildWhiteboard() {
  addBox(scene, 6.4, 1.7, 0.06, -1.4, 1.75, -5.85,
    new THREE.MeshStandardMaterial({ color: 0xfdfdfd, roughness: 0.35 }));
  addBox(scene, 6.6, 0.08, 0.1, -1.4, 0.86, -5.83,
    new THREE.MeshStandardMaterial({ color: 0x8d939b, roughness: 0.5, metalness: 0.4 }));

  const slide = new THREE.Mesh(
    new THREE.PlaneGeometry(3.1, 1.75),
    new THREE.MeshStandardMaterial({
      color: 0x1a2440,
      emissive: 0x2b3f78,
      emissiveIntensity: 0.7,
      roughness: 0.6,
    })
  );
  slide.position.set(4.4, 1.9, -5.8);
  scene.add(slide);

  addBox(scene, 3.3, 1.95, 0.05, 4.4, 1.9, -5.86,
    new THREE.MeshStandardMaterial({ color: 0x22262c, roughness: 0.8 }));
}

function addBox(parent, w, h, d, x, y, z, mat, cast = true) {
  const m = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), mat);
  m.position.set(x, y, z);

  // Walls and ceiling must pass cast=false: a shadow-casting ceiling blocks the key light from above
  // and puts the whole room in shadow.
  m.castShadow = cast;
  m.receiveShadow = true;

  parent.add(m);
  return m;
}

/** Speaker's desk and laptop, at the podium camera position. */
function buildPodium() {
  const wood = new THREE.MeshStandardMaterial({ color: 0xa9764a, roughness: 0.6 });
  const metal = new THREE.MeshStandardMaterial({ color: 0x40454c, roughness: 0.4, metalness: 0.6 });

  const desk = new THREE.Group();
  desk.position.set(0, 0, 0.95);

  addBox(desk, 1.8, 0.06, 0.8, 0, 0.75, 0, wood);
  addBox(desk, 0.05, 0.75, 0.05, -0.8, 0.375, -0.32, metal);
  addBox(desk, 0.05, 0.75, 0.05, 0.8, 0.375, -0.32, metal);
  addBox(desk, 0.05, 0.75, 0.05, -0.8, 0.375, 0.32, metal);
  addBox(desk, 0.05, 0.75, 0.05, 0.8, 0.375, 0.32, metal);

  const laptop = new THREE.Group();
  laptop.position.set(0, 0.78, 0.02);
  addBox(laptop, 0.42, 0.015, 0.3, 0, 0, 0, new THREE.MeshStandardMaterial({ color: 0xb9bec4, roughness: 0.35, metalness: 0.7 }));

  // Lid sits on the far side of the keyboard and tips back toward the camera.
  const lid = new THREE.Group();
  lid.position.set(0, 0.008, 0.14);
  lid.rotation.x = 1.2;
  addBox(lid, 0.42, 0.28, 0.012, 0, 0.14, 0,
    new THREE.MeshStandardMaterial({ color: 0xb9bec4, roughness: 0.35, metalness: 0.7 }));
  addBox(lid, 0.39, 0.25, 0.004, 0, 0.14, -0.009,
    new THREE.MeshStandardMaterial({ color: 0x1a2b4a, emissive: 0x2c4a80, emissiveIntensity: 0.9 }));
  laptop.add(lid);

  desk.add(laptop);
  scene.add(desk);
}
// ---------------------------------------------------------------------------
// People
// ---------------------------------------------------------------------------
//
// Models arrive in a T-pose with no animation clips; all posing below is done by rotating bones.

/** Seeded PRNG, so a given member id always produces the same person. */
function rand(seed) {
  let s = (seed * 9301 + 49297) % 233280;
  return () => {
    s = (s * 9301 + 49297) % 233280;
    return s / 233280;
  };
}

function mat(color, rough = 0.8) {
  return new THREE.MeshStandardMaterial({ color, roughness: rough });
}

// Eight models across 24+ seats, so seats repeat models in rotation with per-person height variation.
const MODEL_FILES = [
  "M_Casual", "W_Casual", "M_Suit", "W_Formal",
  "M_Casual2", "W_Punk", "M_Punk", "W_Suit",
];

// Metres, before per-person variation. The room is modelled at real scale (0.46m seat, 0.75m desk),
// so models are scaled to this rather than the furniture being scaled to the models.
const PERSON_HEIGHT = 1.75;

let prototypes = [];

/**
 * Bone-name aliases per slot, covering the common rig naming conventions (Quaternius, Mixamo, UE).
 * First match wins; findBone throws if a model has none of them. Bones not listed here are untouched.
 */
const BONES = {
  hips: ["Hips", "mixamorig:Hips", "Bip01_Pelvis", "pelvis"],
  abdomen: ["Abdomen", "Spine", "mixamorig:Spine", "spine_01"],
  chest: ["Chest", "Spine2", "mixamorig:Spine2", "UpperChest", "spine_03"],
  head: ["Head", "mixamorig:Head", "head"],

  upperArmL: ["UpperArmL", "LeftArm", "mixamorig:LeftArm", "upperarm_l"],
  lowerArmL: ["LowerArmL", "LeftForeArm", "mixamorig:LeftForeArm", "lowerarm_l"],
  upperArmR: ["UpperArmR", "RightArm", "mixamorig:RightArm", "upperarm_r"],
  lowerArmR: ["LowerArmR", "RightForeArm", "mixamorig:RightForeArm", "lowerarm_r"],

  upperLegL: ["UpperLegL", "LeftUpLeg", "mixamorig:LeftUpLeg", "thigh_l"],
  lowerLegL: ["LowerLegL", "LeftLeg", "mixamorig:LeftLeg", "calf_l"],
  footL: ["FootL", "LeftFoot", "mixamorig:LeftFoot", "foot_l"],
  upperLegR: ["UpperLegR", "RightUpLeg", "mixamorig:RightUpLeg", "thigh_r"],
  lowerLegR: ["LowerLegR", "RightLeg", "mixamorig:RightLeg", "calf_r"],
  footR: ["FootR", "RightFoot", "mixamorig:RightFoot", "foot_r"],
};

/** Load each model once. Seats clone these, sharing geometry and textures on the GPU. */
async function loadPeople() {
  const loader = new GLTFLoader();

  prototypes = await Promise.all(
    MODEL_FILES.map((name) =>
      loader.loadAsync(`./models/${name}.glb`).then((gltf) => {
        const model = gltf.scene;

        model.traverse((o) => {
          if (!o.isMesh) return;
          o.castShadow = true;
          o.receiveShadow = true;

          // A skinned mesh keeps its exported T-pose bounding box; posed bones move the silhouette
          // outside it, so three.js culls people who are still on screen.
          o.frustumCulled = false;
        });

        model.updateMatrixWorld(true);

        // Height and hip height, in the model's own units — both vary per export. Seating derives
        // scale and vertical offset from these.
        const height = new THREE.Box3().setFromObject(model).getSize(new THREE.Vector3()).y;
        const hips = findBone(model, "hips");

        model.userData.height = height;
        model.userData.hipY = hips.getWorldPosition(new THREE.Vector3()).y;

        return model;
      })
    )
  );
}

/** First bone from BONES[slot] that this model actually has. */
function findBone(model, slot) {
  for (const name of BONES[slot]) {
    const bone = model.getObjectByName(name);
    if (bone) return bone;
  }

  throw new Error(`This model has no ${slot} bone (looked for: ${BONES[slot].join(", ")}).`);
}

// --- posing bones -----------------------------------------------------------
//
// Arm and leg bones are not axis-aligned in the bind pose; their axes roll to point down the limb.
// All angles below are therefore given in the character's frame (+Z forward, +Y up), and pose()
// converts to the bone's local frame.

const _e = new THREE.Euler();
const _q = new THREE.Quaternion();

function pose(bone, x, y, z) {
  const d = bone.userData;
  _q.setFromEuler(_e.set(x, y, z));

  // local = parentBind⁻¹ · rotation · parentBind · bindLocal
  bone.quaternion.copy(d.parentInverse).multiply(_q).multiply(d.parentWorld).multiply(d.bind);
}

/**
 * Seated pose and the rest pose reactions blend out of. Radians, in the character's frame.
 *
 * Arm sign conventions: negative `lift` swings the arm forward (toward the speaker). Positive
 * `cross` gathers arms toward the midline, negative spreads them apart. Positive `bend` folds the
 * forearm forward. poseArm mirrors all three between left and right.
 */
const SIT = { thigh: -1.5, shin: 1.25, foot: 0.28 };
const REST = { lift: -0.12, cross: 0.06, bend: 0.55 };
const ARM_DOWN = 1.42;   // T-pose to hanging at the side

function poseArm(rig, side, lift, cross, bend) {
  const s = side === "l" ? -1 : 1;
  pose(rig.arms[side].upper, lift, 0, s * (ARM_DOWN + cross));

  // The forearm points sideways in the T-pose, so folding it forward is a yaw, not a pitch.
  pose(rig.arms[side].lower, 0, s * bend, 0);
}

/**
 * Build one seated person from a prototype model.
 *
 * Must use SkeletonUtils.clone: Object3D.clone copies the mesh but keeps a reference to the original
 * skeleton, so every clone would share one set of bones and pose identically.
 */
function buildPerson(id) {
  const rng = rand(id + 1);
  const proto = prototypes[id % prototypes.length];
  const root = SkeletonUtils.clone(proto);

  // Capture bind orientation and parent frame while the model is still untransformed; pose() needs both.
  root.updateMatrixWorld(true);

  const bones = {};
  root.traverse((o) => {
    if (!o.isBone) return;

    const parentWorld = o.parent.getWorldQuaternion(new THREE.Quaternion());
    o.userData.bind = o.quaternion.clone();
    o.userData.parentWorld = parentWorld;
    o.userData.parentInverse = parentWorld.clone().invert();

    bones[o.name] = o;
  });

  const find = (slot) => {
    for (const name of BONES[slot]) if (bones[name]) return bones[name];
    throw new Error(`This model has no ${slot} bone (looked for: ${BONES[slot].join(", ")}).`);
  };

  const rig = {
    hips: find("hips"),
    abdomen: find("abdomen"),
    chest: find("chest"),
    head: find("head"),
    arms: {
      l: { upper: find("upperArmL"), lower: find("lowerArmL") },
      r: { upper: find("upperArmR"), lower: find("lowerArmR") },
    },
  };

  // Legs never move after this, so they're posed once rather than per frame.
  for (const side of ["L", "R"]) {
    pose(find(`upperLeg${side}`), SIT.thigh, 0, 0);
    pose(find(`lowerLeg${side}`), SIT.shin, 0, 0);
    pose(find(`foot${side}`), SIT.foot, 0, 0);
  }

  // Scale to PERSON_HEIGHT with +/-6% variation.
  const scale = (PERSON_HEIGHT * (0.94 + rng() * 0.12)) / proto.userData.height;
  root.scale.setScalar(scale);

  // Offset so the hips, not the feet, land on the chair seat (the holder group's origin).
  const baseY = -proto.userData.hipY * scale;
  root.position.y = baseY;

  return {
    root, rig, baseY,
    phase: rng() * 100,     // desyncs the idle animation across the room
    slump: 0,
    reaction: R.None,
    intensity: 0,
    progress: 0,
    attention: 0.5,
  };
}

/**
 * Desk and chair for one person.
 *
 * All coordinates here are relative to the chair seat, not the floor, because the holder group sits
 * at seat height: seat is y=0, floor is y=-SEAT_H, desktop is y=0.75-SEAT_H.
 */
const SEAT_H = 0.46;   // metres

function buildFurniture(parent) {
  const wood = mat(0xc79a63, 0.7);
  const metal = new THREE.MeshStandardMaterial({ color: 0x596270, roughness: 0.5, metalness: 0.5 });
  const seatMat = mat(0x3c4a63, 0.85);

  const deskTop = 0.75 - SEAT_H;   // desks are 0.75m off the floor

  // desk
  addBox(parent, 0.95, 0.04, 0.48, 0, deskTop, 0.62, wood);
  addBox(parent, 0.04, 0.75, 0.04, -0.42, deskTop - 0.375, 0.62, metal);
  addBox(parent, 0.04, 0.75, 0.04, 0.42, deskTop - 0.375, 0.62, metal);

  // chair
  addBox(parent, 0.44, 0.05, 0.42, 0, -0.03, 0.06, seatMat);
  addBox(parent, 0.44, 0.46, 0.05, 0, 0.2, -0.17, seatMat);

  for (const sx of [-0.18, 0.18]) {
    for (const sz of [-0.14, 0.24]) {
      addBox(parent, 0.035, SEAT_H, 0.035, sx, -SEAT_H / 2, sz, metal);
    }
  }
}

// ---------------------------------------------------------------------------
// Reaction popups (emoji, billboarded)
// ---------------------------------------------------------------------------

const glyphTextures = {};
let palette = [];   // hex per ReactionType, supplied by C# Reactions.ColorOf

function glyphTexture(reaction) {
  if (glyphTextures[reaction]) return glyphTextures[reaction];

  const c = document.createElement("canvas");
  c.width = c.height = 128;
  const ctx = c.getContext("2d");

  // Coloured disc behind the emoji: readable at distance, and still conveys the reaction on systems
  // with no colour-emoji font, where the glyph renders as an outline or tofu box.
  ctx.beginPath();
  ctx.arc(64, 64, 58, 0, Math.PI * 2);
  ctx.fillStyle = palette[reaction] || "#ffffff";
  ctx.fill();

  ctx.strokeStyle = "rgba(0,0,0,.28)";
  ctx.lineWidth = 5;
  ctx.stroke();

  ctx.font = "72px 'Noto Color Emoji', 'Apple Color Emoji', 'Segoe UI Emoji', system-ui, sans-serif";
  ctx.textAlign = "center";
  ctx.textBaseline = "middle";
  ctx.fillStyle = "#1a1a1a";
  ctx.fillText(GLYPH[reaction] || "•", 64, 68);

  const tex = new THREE.CanvasTexture(c);
  tex.colorSpace = THREE.SRGBColorSpace;
  glyphTextures[reaction] = tex;
  return tex;
}

function spawnPopup(person, reaction) {
  // depthTest left on so popups occlude against room geometry; they sit above head height and are
  // rarely occluded in practice.
  const sprite = new THREE.Sprite(
    new THREE.SpriteMaterial({ map: glyphTexture(reaction), transparent: true, depthWrite: false })
  );

  // Track the head bone, not the mesh: the mesh bounds don't follow the posed skeleton.
  const p = new THREE.Vector3();
  person.rig.head.getWorldPosition(p);
  sprite.position.copy(p).add(new THREE.Vector3(0, 0.3, 0));
  sprite.scale.setScalar(0.32);
  sprite.renderOrder = 999;

  scene.add(sprite);
  popups.push({ sprite, age: 0 });
}
// ---------------------------------------------------------------------------
// Animation
// ---------------------------------------------------------------------------
//
// Reactions accumulate into pose variables and the rig is posed once at the end. pose() writes
// absolute rotations, not additive ones, so a reaction touching bones directly would wipe out the
// idle and slump.

function animatePerson(p, t, dt) {
  const { rig } = p;

  p.phase += dt;

  // Slump tracks inverse attention, always on, independent of any reaction.
  const targetSlump = 1 - p.attention;
  p.slump += (targetSlump - p.slump) * Math.min(1, dt * 2.5);

  const breath = Math.sin(p.phase * 1.5) * 0.012;
  const sway = Math.sin(p.phase * 0.6) * 0.05;

  let spineX = p.slump * 0.30 + breath * 0.3;
  let spineY = sway * 0.3 * p.slump;
  let spineZ = sway * 0.2;

  let headX = p.slump * 0.34;
  let headY = sway * 0.6;
  let headZ = 0;

  let lift = REST.lift, cross = REST.cross, bend = REST.bend;
  let rLift = null, rCross = 0, rBend = 0;   // non-null overrides the right arm, for one-handed reactions
  let bob = 0;

  const active = p.reaction !== R.None && p.progress < 1;

  if (active) {
    // Sine envelope over progress: in, hold, out.
    const env = Math.sin(Math.max(0, Math.min(1, p.progress)) * Math.PI);
    const a = env * (0.55 + 0.45 * p.intensity);

    switch (p.reaction) {
      case R.Applause: {
        // Hands meet at chest height, ~5 claps/sec. Must clear the desk to be visible, and stay
        // below the chin so it doesn't read as covering the face.
        const clap = (Math.sin(p.phase * 30) + 1) * 0.5;
        lift = mix(REST.lift, -0.62, a);
        cross = mix(REST.cross, 0.34 + clap * 0.14, a);
        bend = mix(REST.bend, 1.2, a);
        bob = Math.abs(Math.sin(p.phase * 12)) * 0.012 * a;
        headX -= 0.06 * a;
        break;
      }

      case R.Cheer:
        // Negative cross keeps the arms wide; raised straight up they land across the face.
        lift = mix(REST.lift, -2.7, a);
        cross = mix(REST.cross, -0.45, a);
        bend = mix(REST.bend, 0.35, a);
        bob = Math.abs(Math.sin(p.phase * 9)) * 0.05 * a;
        headX -= 0.22 * a;
        break;

      case R.Laugh: {
        const rock = Math.sin(p.phase * 13) * 0.09 * a;
        spineX += -0.16 * a + rock;
        headX -= 0.3 * a;
        bend = mix(REST.bend, 1.0, a);
        bob = Math.abs(Math.sin(p.phase * 11)) * 0.018 * a;
        break;
      }

      case R.Nod:
        headX += Math.sin(p.phase * 7) * 0.28 * a;
        break;

      case R.Wow:
        // Lean back, hands up and out.
        spineX -= 0.18 * a;
        headX -= 0.22 * a;
        lift = mix(REST.lift, -1.1, a);
        cross = mix(REST.cross, -0.35, a);
        bend = mix(REST.bend, 1.3, a);
        break;

      case R.Love:
        // Hands clasped at the chest.
        lift = mix(REST.lift, -0.55, a);
        cross = mix(REST.cross, 0.55, a);
        bend = mix(REST.bend, 1.7, a);
        headZ += Math.sin(p.phase * 2) * 0.12 * a;
        break;

      case R.ThumbsUp:
        // Right arm only; left stays at rest.
        rLift = mix(REST.lift, -0.85, a);
        rCross = mix(REST.cross, 0.25, a);
        rBend = mix(REST.bend, 1.7, a);
        headX += 0.05 * a;
        break;

      case R.Question: {
        // Raise and hold: driven off progress, not the envelope, so the hand stays up for the
        // duration instead of pumping back down.
        const raise = Math.min(1, p.progress * 6);
        rLift = mix(REST.lift, -2.95, raise);
        rCross = mix(REST.cross, -0.12, raise);
        rBend = mix(REST.bend, 0.12, raise);
        headX -= 0.05 * raise;
        break;
      }

      case R.Confused:
        // Head cocked, right hand toward the chin.
        headZ += 0.3 * a;
        headX += 0.1 * a;
        rLift = mix(REST.lift, -0.55, a);
        rCross = mix(REST.cross, 0.45, a);
        rBend = mix(REST.bend, 1.95, a);
        break;

      case R.Yawn:
        // Head back, arms up and out.
        headX -= 0.45 * a;
        spineX -= 0.1 * a;
        lift = mix(REST.lift, -2.35, a);
        cross = mix(REST.cross, -0.55, a);
        bend = mix(REST.bend, 0.7, a);
        break;

      case R.Distracted:
        // Look down and away.
        headY += 0.7 * a;
        headX += 0.45 * a;
        spineY += 0.25 * a;
        break;
    }
  }

  // --- write the pose to the bones ---

  // Spine fold is split 45/55 across abdomen and chest rather than hinging at one joint.
  pose(rig.abdomen, spineX * 0.45, spineY * 0.5, spineZ * 0.5);
  pose(rig.chest, spineX * 0.55, spineY * 0.5, spineZ * 0.5);
  pose(rig.head, headX, headY, headZ);

  poseArm(rig, "l", lift, cross, bend);
  poseArm(rig, "r", rLift === null ? lift : rLift,
    rLift === null ? cross : rCross,
    rLift === null ? bend : rBend);

  p.root.position.y = p.baseY + bob;
}

function mix(from, to, a) {
  return from + (to - from) * a;
}

// ---------------------------------------------------------------------------
// Camera
// ---------------------------------------------------------------------------

function updateCamera() {
  if (view.mode === "podium") {
    // Podium, eye height, set back so the desk and laptop frame the bottom of the shot.
    camera.position.set(0, 1.58, -1.15);
    camera.rotation.order = "YXZ";
    camera.rotation.y = Math.PI + view.yaw;   // face +Z, toward the audience
    camera.rotation.x = view.pitch;
    camera.fov = 60;
  } else {
    // Overview from above and behind the podium. Height must stay under CEILING.
    const a = view.yaw * 0.8;
    camera.position.set(4.5 * Math.sin(a), CEILING - 0.7, -4.2 - 0.8 * Math.cos(a));
    camera.lookAt(0, 0.85, 8.5);
    camera.fov = 62;
  }

  camera.updateProjectionMatrix();
}

function bindLook() {
  const down = (e) => {
    view.dragging = true;
    view.lastX = e.clientX ?? e.touches[0].clientX;
    view.lastY = e.clientY ?? e.touches[0].clientY;
  };

  const move = (e) => {
    if (!view.dragging) return;
    const x = e.clientX ?? e.touches[0].clientX;
    const y = e.clientY ?? e.touches[0].clientY;

    view.yaw -= (x - view.lastX) * 0.0035;
    view.pitch -= (y - view.lastY) * 0.0025;
    view.pitch = Math.max(-0.5, Math.min(0.35, view.pitch));

    view.lastX = x;
    view.lastY = y;
  };

  const up = () => (view.dragging = false);

  canvas.addEventListener("pointerdown", down);
  window.addEventListener("pointermove", move);
  window.addEventListener("pointerup", up);
}

// ---------------------------------------------------------------------------
// Seating
// ---------------------------------------------------------------------------

/** Walks the flat [x, y, z, facing] array from C#, creating one holder group per member. */
function eachSeat(seats, fn) {
  for (let i = 0; i < seats.length / 4; i++) {
    const x = seats[i * 4], y = seats[i * 4 + 1], z = seats[i * 4 + 2], facing = seats[i * 4 + 3];

    const holder = new THREE.Group();
    holder.position.set(x, y + SEAT_H, z);   // group origin is the chair seat
    holder.rotation.y = facing;

    scene.add(holder);
    holders.push(holder);

    fn(holder, i);
  }
}

function clearSeats() {
  for (const h of holders) scene.remove(h);
  holders = [];
  audience = [];
}

function seatFurnitureOnly(seats) {
  clearSeats();
  eachSeat(seats, (holder) => buildFurniture(holder));
}

function seatNow(seats) {
  clearSeats();

  eachSeat(seats, (holder, i) => {
    const person = buildPerson(i);
    holder.add(person.root);
    buildFurniture(holder);

    person.holder = holder;
    audience.push(person);
  });
}

// ---------------------------------------------------------------------------
// Public API — called from Blazor
// ---------------------------------------------------------------------------

window.vrScene = {
  /**
   * seats:  flat [x, y, z, facing] per member, from the C# SeatingLayout.
   * colors: hex string per ReactionType, from the C# Reactions.ColorOf.
   */
  init(selector, seats, colors) {
    palette = colors || [];
    canvas = document.querySelector(selector);

    renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    renderer.shadowMap.enabled = true;
    renderer.shadowMap.type = THREE.PCFSoftShadowMap;
    renderer.toneMapping = THREE.ACESFilmicToneMapping;
    renderer.toneMappingExposure = 1.05;

    camera = new THREE.PerspectiveCamera(62, 1, 0.1, 100);
    clock = new THREE.Clock();

    buildRoom();
    bindLook();
    this.resize();

    // Don't block the first frame on the model download (~6MB): seat the furniture now and add the
    // people once the models resolve.
    this.seat(seats);
    loadPeople().then(() => {
      modelsReady = true;
      if (pendingSeats) seatNow(pendingSeats);
    });

    window.addEventListener("resize", () => this.resize());

    const loop = () => {
      raf = requestAnimationFrame(loop);
      const dt = Math.min(0.05, clock.getDelta());
      const t = clock.elapsedTime;

      for (const p of audience) animatePerson(p, t, dt);

      // Popups drift up and fade over 1.5s.
      for (let i = popups.length - 1; i >= 0; i--) {
        const pop = popups[i];
        pop.age += dt;
        pop.sprite.position.y += dt * 0.45;
        pop.sprite.material.opacity = Math.max(0, 1 - pop.age / 1.5);

        if (pop.age > 1.5) {
          scene.remove(pop.sprite);
          pop.sprite.material.dispose();
          popups.splice(i, 1);
        }
      }

      updateCamera();
      renderer.render(scene, camera);
    };

    cancelAnimationFrame(raf);
    loop();
  },

  /**
   * Rebuild the audience at new seats; called when the room size changes. If the models haven't
   * loaded, seats the furniture and defers the people to loadPeople's continuation.
   */
  seat(seats) {
    pendingSeats = seats;
    if (modelsReady) seatNow(seats);
    else seatFurnitureOnly(seats);
  },

  /**
   * Called per frame from C#. `state` is flat, 4 numbers per member in Room.Members order:
   * [reaction, intensity, progress, attention].
   */
  update(state) {
    for (let i = 0; i < audience.length; i++) {
      const p = audience[i];
      const o = i * 4;

      const reaction = state[o] | 0;

      // Popup fires on the transition into a new reaction, not while it's held.
      if (reaction !== R.None && reaction !== p.reaction) spawnPopup(p, reaction);

      p.reaction = reaction;
      p.intensity = state[o + 1];
      p.progress = state[o + 2];
      p.attention = state[o + 3];
    }
  },

  setView(mode) {
    view.mode = mode;
    view.yaw = 0;
    view.pitch = mode === "podium" ? -0.04 : 0;
  },

  resize() {
    if (!canvas) return;
    const w = canvas.clientWidth || 960;
    const h = canvas.clientHeight || 540;
    renderer.setSize(w, h, false);
    camera.aspect = w / h;
    camera.updateProjectionMatrix();
  },
};
