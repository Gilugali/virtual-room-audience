using System;

namespace VirtualRoom.Audience
{
    public readonly struct SeatPosition
    {
        public readonly int Row;

        public readonly int Column;

        public readonly float X;
        public readonly float Y;
        public readonly float Z;

        public readonly float FacingRadians;

        public SeatPosition(int row, int column, float x, float y, float z, float facingRadians)
        {
            Row = row;
            Column = column;
            X = x;
            Y = y;
            Z = z;
            FacingRadians = facingRadians;
        }

        public float DistanceTo(in SeatPosition other)
        {
            float dx = X - other.X;
            float dy = Y - other.Y;
            float dz = Z - other.Z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }

    public enum SeatingStyle
    {
        Theater,

        Classroom,
    }

    [Serializable]
    public class SeatingSettings
    {
        public SeatingStyle Style = SeatingStyle.Theater;

        public float ArcDegrees = 120f;

        public int SeatsPerRow = 0;

        public float AisleWidth = 1.2f;

        public float FrontRowRadius = 3.0f;

        public float RowSpacing = 1.4f;

        public float SeatSpacing = 1.1f;

        public float RiserHeight = 0.35f;

        public float Jitter = 0.12f;

        public SeatingSettings Clone() => (SeatingSettings)MemberwiseClone();
    }
}
