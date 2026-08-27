using System;
using SephiriaDpsMeter;

internal static class DpsRoomScopeTests
{
    private static int passed;

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("FAIL: " + name);
        passed++;
        Console.WriteLine("PASS: " + name);
    }

    private static void Main()
    {
        RoomScope roomA = RoomScope.Create("floor-1", 1, 0, 0, 10, 10);
        RoomScope roomB = RoomScope.Create("floor-1", 2, 20, 0, 30, 10);
        RoomScope otherFloor = RoomScope.Create("floor-2", 1, 0, 0, 10, 10);
        Check(roomA.AllowsDamage("floor-1", 2, 2, 8, 8), "same-room player and target included");
        Check(!roomA.AllowsDamage("floor-1", 22, 2, 28, 8), "other-room player and target excluded on same floor");
        Check(!roomA.AllowsDamage("floor-2", 2, 2, 8, 8), "other-floor player excluded even with overlapping coordinates");
        Check(!roomA.AllowsDamage("floor-1", 2, 2, 28, 8), "lingering remote target or summon damage excluded");
        Check(!roomA.AllowsDamage("floor-1", 22, 2, 8, 8), "owner in another room excluded despite local target");
        Check(roomA.AllowsDamage("floor-1", 2, 2, 8, 8), "resolved summon owner in local room included");
        Check(!roomA.AllowsDamage(null, 2, 2, 8, 8), "unknown owner floor excluded");
        Check(!roomA.AllowsDamage("", 2, 2, 8, 8), "empty owner floor excluded");
        Check(roomA.Contains(0, 0) && roomA.Contains(10, 10), "boundary matches native inclusive comparisons");
        Check(!roomA.Contains(10.01f, 5), "adjacent-room position not rounded into local room");
        Check(!roomA.Contains(float.NaN, 5), "invalid position excluded");
        Check(!roomA.Contains(float.PositiveInfinity, 5), "infinite position excluded");
        Check(!roomA.IsSameRoom(roomB), "changing area within same floor requires new statistics");
        Check(!roomA.IsSameRoom(otherFloor), "changing floor requires new statistics");
        Check(roomA.IsSameRoom(RoomScope.Create("floor-1", 1, 0, 0, 10, 10)), "same room retains statistics");
        Check(!roomA.IsSameRoom(null), "unknown room cannot match active room");
        Check(RoomScope.Create("", 1, 0, 0, 10, 10) == null, "missing local floor rejected");
        Check(RoomScope.Create("floor-1", 0, 0, 0, 10, 10) == null, "missing area identity rejected");
        Check(RoomScope.Create("floor-1", 1, 0, 0, 0, 10) == null, "uninitialized zero-width bounds rejected");
        Check(RoomScope.Create("floor-1", 1, 10, 0, 0, 10) == null, "inverted bounds rejected");
        Check(RoomScope.Create("floor-1", 1, 0, 0, float.PositiveInfinity, 10) == null, "non-finite bounds rejected");
        Check(RoomScope.Create("floor-1", -42, 0, 0, 10, 10) != null, "negative Unity instance IDs allowed");
        Check(RoomScope.SelectContaining(null, roomA, 2, 2) == roomA, "find local area");
        Check(RoomScope.SelectContaining(roomA, roomB, 2, 2) == roomA, "ignore other room when selecting area");
        RoomScope outer = RoomScope.Create("floor-1", 3, -50, -50, 50, 50);
        Check(RoomScope.SelectContaining(outer, roomA, 2, 2) == roomA, "prefer specific room over enclosing area");
        Check(RoomScope.SelectContaining(roomA, outer, 2, 2) == roomA, "area choice independent of discovery order");
        Check(RoomScope.SelectContaining(null, null, 2, 2) == null, "unknown areas fail closed");
        Check(!roomB.AllowsDamage("floor-1", 22, 2, 8, 8), "old-room feedback rejected after moving to new room");
        Check(roomB.AllowsDamage("floor-1", 22, 2, 28, 8), "new-room damage accepted after transition");
        Console.WriteLine("Room scope tests passed: " + passed);
    }
}
