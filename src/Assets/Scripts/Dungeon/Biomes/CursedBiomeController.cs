using UnityEngine;

public sealed class CursedBiomeController : MonoBehaviour
{
    [Header("Curse Timing")]
    [SerializeField] private float graceSeconds = 60f;  
    [SerializeField] private float dps = 0.05f;             // damage/second after grace

    private ISpecialSolver solver;
    private bool solved;
    private float t;
    private float damageAccumulator;

    private void Start()
    {
        solver = FindCurrentSolver();

        if (solver != null)
        {
            Debug.Log($"[CursedBiomeController] Binding to solver: {solver.GetType().Name}", solver as MonoBehaviour);
            solver.OnSolved += HandleSolved;
        }
        else { Debug.LogWarning("[CursedBiomeController] No solver found in scene!", this); }
    }

    private void OnDestroy()
    {
        if (solver != null) 
            solver.OnSolved -= HandleSolved;
    }

    private void HandleSolved() => solved = true;

    private void Update()
    {
        if (solved) return;

        t += Time.deltaTime;
        if (t < graceSeconds) return;

        //Debug.Log($"[CursedBiomeController] Applying curse damage tick", this);
        var hp = GetComponent<PlayerHealth>();
        if (!hp || hp.Current <= 0) return;

        damageAccumulator += dps * Time.deltaTime;

        int ticks = Mathf.FloorToInt(damageAccumulator);
        if (ticks <= 0) return;

        damageAccumulator -= ticks;
        //Debug.Log($"[CursedBiomeController] Applying {ticks} curse damage", this);
        hp.Damage(ticks);
    }

    private static ISpecialSolver FindCurrentSolver()
    {
        var monos = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var m in monos)
            if (m is ISpecialSolver s)
            {
                Debug.Log($"[CursedBiomeController] Found solver: {s.GetType().Name} on {s}", s as MonoBehaviour);
                return s;
            }

        return null;
    }
}