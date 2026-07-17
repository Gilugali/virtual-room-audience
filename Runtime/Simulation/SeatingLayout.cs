using System;
using System.Collections.Generic;

namespace VirtualRoom.Audience
{
    public static class SeatingLayout
    {
        public static SeatPosition[] Build(int count, SeatingSettings settings, int seed = 0)
        {
            settings = settings ?? new SeatingSettings();

            return settings.Style == SeatingStyle.Classroom
                ? Classroom(count, settings, seed)
                : Theater(count, settings, seed);
        }

        public static SeatPosition[] Classroom(int count, SeatingSettings settings, int seed = 0)
        {
            if (count <= 0) return Array.Empty<SeatPosition>();
            settings = settings ?? new SeatingSettings();

            var rng = new RoomRandom(seed);
            var seats = new List<SeatPosition>(count);

            int perRow = settings.SeatsPerRow > 0
                ? settings.SeatsPerRow
                : Math.Max(2, (int)Math.Round(Math.Sqrt(count * 1.6)));

            int rows = (int)Math.Ceiling(count / (float)perRow);

            for (int row = 0; row < rows; row++)
            {
                int remaining = count - seats.Count;
                int take = Math.Min(perRow, remaining);
                if (take <= 0) break;

                float z = settings.FrontRowRadius + row * settings.RowSpacing;
                float y = row * settings.RiserHeight;

                float half = (take - 1) * 0.5f;

                for (int col = 0; col < take; col++)
                {
                    float offset = (col - half) * settings.SeatSpacing;

                    if (settings.AisleWidth > 0f && take > 3)
                    {
                        offset += (col < take / 2f ? -1f : 1f) * settings.AisleWidth * 0.5f;
                    }

                    float x = offset + rng.Range(-settings.Jitter, settings.Jitter);
                    float zj = z + rng.Range(-settings.Jitter, settings.Jitter);

                    float facing = (float)Math.Atan2(-x, -zj);

                    seats.Add(new SeatPosition(row, col, x, y, zj, facing));
                }
            }

            return seats.ToArray();
        }

        public static SeatPosition[] Theater(int count, SeatingSettings settings, int seed = 0)
        {
            if (count <= 0) return Array.Empty<SeatPosition>();
            settings = settings ?? new SeatingSettings();

            var rng = new RoomRandom(seed);
            var seats = new List<SeatPosition>(count);
            float arc = (float)(settings.ArcDegrees * Math.PI / 180.0);

            int row = 0;
            while (seats.Count < count)
            {
                float radius = settings.FrontRowRadius + row * settings.RowSpacing;

                float arcLength = arc * radius;
                int capacity = Math.Max(1, (int)Math.Floor(arcLength / settings.SeatSpacing) + 1);
                int take = Math.Min(capacity, count - seats.Count);

                float usedArc = take > 1 ? arc * ((take - 1) / (float)(capacity - 1 < 1 ? 1 : capacity - 1)) : 0f;
                float start = -usedArc * 0.5f;
                float step = take > 1 ? usedArc / (take - 1) : 0f;

                for (int i = 0; i < take; i++)
                {
                    float angle = take > 1 ? start + step * i : 0f;

                    float jx = rng.Range(-settings.Jitter, settings.Jitter);
                    float jz = rng.Range(-settings.Jitter, settings.Jitter);

                    float x = (float)Math.Sin(angle) * radius + jx;
                    float z = (float)Math.Cos(angle) * radius + jz;
                    float y = row * settings.RiserHeight;

                    float facing = (float)Math.Atan2(-x, -z);

                    seats.Add(new SeatPosition(row, i, x, y, z, facing));
                }

                row++;

                if (row > 500) break;
            }

            return seats.ToArray();
        }
    }
}
