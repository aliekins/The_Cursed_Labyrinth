using UnityEngine;

public sealed class CursedBiomeController : MonoBehaviour
{
    [Header("Curse Timing")]
    [SerializeField] private float graceSeconds = 120f;  
    [SerializeField] private float dps = 0.0002f;             // damage/second after grace

    private ISpecialSolver solver;
    private bool solved;
    private float t;
    private float damageAccumulator;

    private void Start()
    {
        solver = FindCurrentSolver();

        if (solver != null) 
            solver.OnSolved += HandleSolved;
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

        var hp = GetComponent<PlayerHealth>();
        if (!hp || hp.Current <= 0) return;

        damageAccumulator += dps * Time.deltaTime;

        int ticks = Mathf.FloorToInt(damageAccumulator);
        if (ticks <= 0) return;

        damageAccumulator -= ticks;
        hp.Damage(ticks);
    }

    private static ISpecialSolver FindCurrentSolver()
    {
        var monos = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var m in monos)
            if (m is ISpecialSolver s) 
                return s;

        return null;
    }
}