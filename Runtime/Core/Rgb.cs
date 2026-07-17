namespace VirtualRoom.Audience
{
    public readonly struct Rgb
    {
        public readonly float R;
        public readonly float G;
        public readonly float B;

        public Rgb(float r, float g, float b)
        {
            R = r;
            G = g;
            B = b;
        }

        public string ToHex()
        {
            int r = Channel(R), g = Channel(G), b = Channel(B);
            return $"#{r:x2}{g:x2}{b:x2}";
        }

        private static int Channel(float v)
        {
            int i = (int)(M.Clamp01(v) * 255f + 0.5f);
            return i < 0 ? 0 : i > 255 ? 255 : i;
        }
    }
}
