using UnityEngine;
using UnityEngine.Events;

public class SocketPad : MonoBehaviour
{
    public TokenType required;
    public bool filled { get; private set; }
    public UnityEvent onFilled;   // optional: glow, sfx, show placed sprite

    public bool TryPlace()
    {
        if (filled) return false;
        if (!PuzzleInventory.I) return false;
        if (!PuzzleInventory.I.Consume(required, 1)) return false;

        filled = true;
        onFilled?.Invoke();
        return true;
    }
}