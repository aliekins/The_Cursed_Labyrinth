// Assets/Scripts/Sound/AudioKeys.cs
public static class AudioKeys
{
    public const string BreakProp = "sfx_break_prop";
    public const string DropItem = "sfx_drop_item";
    public const string PickItem = "sfx_pick_item";
    public const string HpHit = "sfx_hp_hit";
    public const string HpDie = "sfx_hp_die";
    public const string LeverFlip = "sfx_lever_flip";
    public const string DoorOpen = "sfx_door_open";
    public const string SpikeTick = "sfx_spike_tick";

    public static string MusicForBiome(int biomeIndex) => biomeIndex switch
    {
        0 => "music_biome1",
        1 => "music_biome2",
        2 => "music_biome3",
        _ => "music_biome1"
    };
    public const string MusicEnding = "music_ending";
}