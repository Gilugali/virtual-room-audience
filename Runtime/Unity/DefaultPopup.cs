using UnityEngine;

namespace VirtualRoom.Audience.Unity
{
    internal static class DefaultPopup
    {
        public static ReactionPopup Create(Vector3 worldPosition, Transform parent)
        {
            var go = new GameObject("Reaction Popup");
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.position = worldPosition;

            Font font = BuiltinFont.Get();

            if (font != null)
            {
                var text = go.AddComponent<TextMesh>();
                text.font = font;
                text.text = "";
                text.characterSize = 0.08f;
                text.fontSize = 64;
                text.anchor = TextAnchor.MiddleCenter;
                text.alignment = TextAlignment.Center;

                go.GetComponent<MeshRenderer>().material = font.material;
            }
            else
            {
                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.transform.SetParent(go.transform, false);
                quad.transform.localScale = Vector3.one * 0.18f;
                Object.Destroy(quad.GetComponent<Collider>());
            }

            return go.AddComponent<ReactionPopup>();
        }
    }

    internal static class BuiltinFont
    {
        private static Font _font;
        private static bool _resolved;

        public static Font Get()
        {
            if (_resolved) return _font;
            _resolved = true;

            _font = TryLoad("LegacyRuntime.ttf") ?? TryLoad("Arial.ttf");

            if (_font == null)
            {
                Debug.LogWarning("[VirtualRoom] No built-in font found, so reaction popups will be " +
                                 "plain coloured shapes. Assign your own prefab to " +
                                 "AudienceRoomBehaviour.reactionPopupPrefab to fix this properly.");
            }

            return _font;
        }

        private static Font TryLoad(string name)
        {
            try
            {
                return Resources.GetBuiltinResource<Font>(name);
            }
            catch
            {
                return null;
            }
        }
    }
}
