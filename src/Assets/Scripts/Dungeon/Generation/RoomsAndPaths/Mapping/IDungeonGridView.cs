public interface IDungeonGridView
{
    int Width { get; }
    int Height { get; }
    bool InBounds(int x, int y);
    string GetKind(int x, int y);  
    int GetRoomId(int x, int y);   // -1 if none
}