public sealed class DungeonGridView : IDungeonGridView
{
    private readonly DungeonGrid _g;
    public DungeonGridView(DungeonGrid g) => _g = g;
    public int Width => _g.Width;
    public int Height => _g.Height;
    public bool InBounds(int x, int y) => _g.InBounds(x, y);
    public string GetKind(int x, int y) => _g.Kind[x, y];
    public int GetRoomId(int x, int y) => _g.RoomId[x, y];
}