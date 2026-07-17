using UnityEngine;

namespace VirtualRoom.Audience.Unity
{
    public static class ProceduralAvatar
    {
        private static readonly Color[] Skin =
        {
            Rgb255(255, 215, 176), Rgb255(247, 196, 154), Rgb255(232, 168, 119), Rgb255(207, 137, 86),
            Rgb255(169, 102, 60),  Rgb255(125, 74, 44),   Rgb255(255, 226, 198), Rgb255(156, 95, 56),
        };

        private static readonly Color[] Hair =
        {
            Rgb255(42, 29, 22),   Rgb255(74, 44, 26),   Rgb255(138, 90, 43),  Rgb255(217, 164, 65),
            Rgb255(232, 106, 69), Rgb255(142, 75, 208), Rgb255(47, 111, 208), Rgb255(39, 194, 160),
            Rgb255(240, 90, 143), Rgb255(59, 59, 70),   Rgb255(247, 226, 168), Rgb255(26, 26, 32),
        };

        private static readonly Color[] Shirt =
        {
            Rgb255(79, 142, 247),  Rgb255(242, 88, 91),  Rgb255(47, 191, 143), Rgb255(155, 110, 243),
            Rgb255(255, 176, 56),  Rgb255(89, 194, 232), Rgb255(24, 169, 153), Rgb255(240, 122, 176),
            Rgb255(63, 91, 216),   Rgb255(255, 133, 81), Rgb255(139, 195, 74), Rgb255(176, 92, 230),
        };

        private static readonly Color[] Trouser =
        {
            Rgb255(59, 74, 99), Rgb255(74, 85, 104), Rgb255(43, 58, 85),
            Rgb255(107, 92, 71), Rgb255(57, 65, 80), Rgb255(90, 74, 107),
        };

        private static readonly Color Ink = Rgb255(36, 29, 43);
        private static readonly Color EyeWhite = Rgb255(253, 253, 255);
        private static readonly Color Blush = Rgb255(255, 143, 160);
        private static readonly Color Mouth = Rgb255(92, 47, 58);

        public static AudienceMemberView Build(Transform parent, int variant)
        {
            var rng = new System.Random(variant * 7919);
            float Next(float min, float max) => min + (float)rng.NextDouble() * (max - min);

            var root = new GameObject("Audience Member");
            root.transform.SetParent(parent, false);

            float h = Next(0.94f, 1.08f);

            Color skin = Skin[rng.Next(Skin.Length)];
            Color hair = Hair[rng.Next(Hair.Length)];
            Color shirt = Shirt[rng.Next(Shirt.Length)];
            Color trouser = Trouser[rng.Next(Trouser.Length)];

            float hip = 0.45f * h;
            float shoulder = 0.80f * h;
            float headY = 1.00f * h;

            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 0.11f;

                Transform thigh = Primitive(PrimitiveType.Capsule, root.transform, "Thigh",
                    new Vector3(x, hip - 0.03f, 0.16f), new Vector3(0.15f, 0.13f, 0.15f), trouser);
                thigh.localRotation = Quaternion.Euler(90f, 0f, 0f);

                Primitive(PrimitiveType.Capsule, root.transform, "Shin",
                    new Vector3(x, hip - 0.19f, 0.30f), new Vector3(0.14f, 0.14f, 0.14f), trouser);

                Transform shoe = Primitive(PrimitiveType.Sphere, root.transform, "Shoe",
                    new Vector3(x, 0.06f, 0.36f), new Vector3(0.15f, 0.10f, 0.26f), Ink);
                shoe.name = "Shoe";
            }

            var body = new GameObject("Body").transform;
            body.SetParent(root.transform, false);
            body.localPosition = new Vector3(0f, hip, 0f);

            Primitive(PrimitiveType.Sphere, body, "Torso",
                new Vector3(0f, 0.20f, 0f), new Vector3(0.42f, 0.44f, 0.34f), shirt);

            var head = new GameObject("Head").transform;
            head.SetParent(root.transform, false);
            head.localPosition = new Vector3(0f, headY, 0f);

            Primitive(PrimitiveType.Sphere, head, "Skull",
                Vector3.zero, new Vector3(0.40f, 0.42f, 0.40f), skin);

            BuildHair(head, hair, rng);
            BuildFace(head, skin, hair);

            Transform leftArm = BuildArm(root.transform, "Arm.L", -1, shoulder, shirt, skin);
            Transform rightArm = BuildArm(root.transform, "Arm.R", 1, shoulder, shirt, skin);

            var anchor = new GameObject("Popup Anchor").transform;
            anchor.SetParent(root.transform, false);
            anchor.localPosition = new Vector3(0f, 1.45f * h, 0f);

            var view = root.AddComponent<AudienceMemberView>();
            view.body = body;
            view.head = head;
            view.leftArm = leftArm;
            view.rightArm = rightArm;
            view.popupAnchor = anchor;

            return view;
        }

        private static Transform BuildArm(Transform root, string name, int side, float shoulderY,
            Color sleeve, Color skin)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(root, false);
            pivot.localPosition = new Vector3(side * 0.20f, shoulderY, 0f);
            pivot.localRotation = Quaternion.Euler(-20f, 0f, side * 6f);

            Primitive(PrimitiveType.Capsule, pivot, "Limb",
                new Vector3(0f, -0.15f, 0f), new Vector3(0.11f, 0.15f, 0.11f), sleeve);

            Primitive(PrimitiveType.Sphere, pivot, "Hand",
                new Vector3(0f, -0.34f, 0f), new Vector3(0.14f, 0.13f, 0.12f), skin);

            return pivot;
        }

        private static void BuildFace(Transform head, Color skin, Color hair)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 0.082f;

                Primitive(PrimitiveType.Sphere, head, "Eye",
                    new Vector3(x, 0.015f, 0.185f), new Vector3(0.10f, 0.115f, 0.05f), EyeWhite);

                Primitive(PrimitiveType.Sphere, head, "Pupil",
                    new Vector3(side * 0.085f, 0.008f, 0.20f), new Vector3(0.062f, 0.07f, 0.03f), Ink);

                Primitive(PrimitiveType.Sphere, head, "Shine",
                    new Vector3(side * 0.098f, 0.038f, 0.215f), Vector3.one * 0.025f, Color.white);

                Transform brow = Primitive(PrimitiveType.Cube, head, "Brow",
                    new Vector3(side * 0.085f, 0.088f, 0.175f), new Vector3(0.075f, 0.018f, 0.02f), hair);
                brow.localRotation = Quaternion.Euler(0f, 0f, side * -9f);

                Primitive(PrimitiveType.Sphere, head, "Ear",
                    new Vector3(side * 0.198f, -0.01f, 0f), new Vector3(0.045f, 0.08f, 0.06f), skin);

                Primitive(PrimitiveType.Sphere, head, "Cheek",
                    new Vector3(side * 0.115f, -0.05f, 0.16f), new Vector3(0.07f, 0.05f, 0.02f), Blush);
            }

            Primitive(PrimitiveType.Sphere, head, "Nose",
                new Vector3(0f, -0.035f, 0.19f), Vector3.one * 0.045f, skin);

            Primitive(PrimitiveType.Sphere, head, "Mouth",
                new Vector3(-0.038f, -0.072f, 0.183f), new Vector3(0.032f, 0.024f, 0.02f), Mouth);
            Primitive(PrimitiveType.Sphere, head, "Mouth",
                new Vector3(0f, -0.088f, 0.19f), new Vector3(0.036f, 0.026f, 0.02f), Mouth);
            Primitive(PrimitiveType.Sphere, head, "Mouth",
                new Vector3(0.038f, -0.072f, 0.183f), new Vector3(0.032f, 0.024f, 0.02f), Mouth);
        }

        private static void BuildHair(Transform head, Color hair, System.Random rng)
        {
            int style = rng.Next(6);

            Primitive(PrimitiveType.Sphere, head, "Hair",
                new Vector3(0f, 0.07f, -0.02f), new Vector3(0.43f, 0.36f, 0.44f), hair);

            switch (style)
            {
                case 0:
                    Primitive(PrimitiveType.Sphere, head, "Hair.Back",
                        new Vector3(0f, -0.05f, -0.07f), new Vector3(0.40f, 0.34f, 0.32f), hair);
                    break;

                case 1:
                    Primitive(PrimitiveType.Sphere, head, "Hair.Bun",
                        new Vector3(0f, 0.24f, -0.03f), Vector3.one * 0.17f, hair);
                    break;

                case 2:
                    Primitive(PrimitiveType.Sphere, head, "Hair.Tail",
                        new Vector3(0f, -0.02f, -0.22f), new Vector3(0.14f, 0.26f, 0.14f), hair);
                    break;

                case 3:
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Primitive(PrimitiveType.Sphere, head, "Hair.Pigtail",
                            new Vector3(side * 0.21f, 0.04f, -0.06f), Vector3.one * 0.16f, hair);
                    }
                    break;

                case 4:
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i / 6f * Mathf.PI * 2f;
                        Primitive(PrimitiveType.Sphere, head, "Hair.Curl",
                            new Vector3(Mathf.Cos(a) * 0.15f, 0.14f + Mathf.Sin(i * 2.1f) * 0.04f,
                                Mathf.Sin(a) * 0.15f - 0.02f),
                            Vector3.one * 0.17f, hair);
                    }
                    break;

            }
        }

        private static Transform Primitive(PrimitiveType type, Transform parent, string name,
            Vector3 position, Vector3 scale, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;

            Object.Destroy(go.GetComponent<Collider>());

            var renderer = go.GetComponent<Renderer>();

            renderer.material.color = color;

            return go.transform;
        }

        private static Color Rgb255(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f);
    }
}
