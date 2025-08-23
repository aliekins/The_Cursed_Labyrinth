using System.Collections.Generic;

public sealed class DungeonMapIndexBuilder
{
    public sealed class Options
    {
        public string CorridorPrefix = "floor_corridor";
        public System.Func<int, string> SpecialOfOrNull;
    }

    public static DungeonMapIndex Build(DungeonGrid grid, IReadOnlyList<Room> rooms, Options opt = null)
    {
        var view = new DungeonGridView(grid);
        opt ??= new Options();

        var roomId = RoomIdMapBuilder.Build(view);
        var cor = CorridorLabeler.Build(view, opt.CorridorPrefix);
        var walls = WallScanner.Build(view);
        var roomBundles = RoomBundleBuilder.Build(view, rooms, opt.SpecialOfOrNull);

        return new DungeonMapIndex(roomId, cor.CorridorId, walls, cor.Components, roomBundles);
    }
}