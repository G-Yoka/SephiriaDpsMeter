using System;

namespace SephiriaDpsMeter
{
    // Pure room-matching rules, tested without Unity or a running game.
    internal sealed class RoomScope
    {
        internal readonly string FloorGuid;
        internal readonly int AreaId;
        private readonly float minX, minY, maxX, maxY;

        private RoomScope(string floorGuid, int areaId, float left, float bottom, float right, float top)
        {
            FloorGuid = floorGuid;
            AreaId = areaId;
            minX = left;
            minY = bottom;
            maxX = right;
            maxY = top;
        }

        internal static RoomScope Create(string floorGuid, int areaId, float left, float bottom, float right, float top)
        {
            if (string.IsNullOrEmpty(floorGuid) || areaId == 0 ||
                !IsFinite(left) || !IsFinite(bottom) || !IsFinite(right) || !IsFinite(top) ||
                right <= left || top <= bottom)
                return null;
            return new RoomScope(floorGuid, areaId, left, bottom, right, top);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        internal bool Contains(float x, float y)
        {
            // Match the game's inclusive comparison at the room boundary.
            return x >= minX && x <= maxX && y >= minY && y <= maxY;
        }

        internal bool IsSameRoom(RoomScope other)
        {
            return other != null && AreaId == other.AreaId &&
                string.Equals(FloorGuid, other.FloorGuid, StringComparison.Ordinal);
        }

        internal bool AllowsDamage(string ownerFloor, float ownerX, float ownerY, float targetX, float targetY)
        {
            return !string.IsNullOrEmpty(ownerFloor) &&
                string.Equals(FloorGuid, ownerFloor, StringComparison.Ordinal) &&
                Contains(ownerX, ownerY) && Contains(targetX, targetY);
        }

        internal static RoomScope SelectContaining(RoomScope selected, RoomScope candidate, float x, float y)
        {
            if (candidate == null || !candidate.Contains(x, y))
                return selected;
            if (selected == null)
                return candidate;
            double selectedArea = ((double)selected.maxX - selected.minX) * ((double)selected.maxY - selected.minY);
            double candidateArea = ((double)candidate.maxX - candidate.minX) * ((double)candidate.maxY - candidate.minY);
            return candidateArea < selectedArea || (candidateArea == selectedArea && candidate.AreaId < selected.AreaId)
                ? candidate : selected;
        }
    }
}
