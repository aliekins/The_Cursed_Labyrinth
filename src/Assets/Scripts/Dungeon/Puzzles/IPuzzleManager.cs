/// \file IPuzzleManager.cs
/// \brief Runtime side of a puzzle prefab.
public interface IPuzzleManager
{
    /// <summary>Called after instantiation to provide plan + controller context.</summary>
    void Init(PuzzlePlan plan, DungeonController controller);
}
