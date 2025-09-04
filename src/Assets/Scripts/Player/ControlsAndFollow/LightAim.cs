/**
 * @file LightAim.cs
 * @brief Rotates a child light to match the player's facing direction.
 * @ingroup Player
 */
using UnityEngine;

public sealed class LightAim : MonoBehaviour
{
    #region config and setters
    [Header("Optional (auto-wired)")]
    [SerializeField] private TopDownController playerController;   
    [SerializeField] private Transform playerTransform;

    [Header("Smoothing")]
    [SerializeField] private float degreesPerSecond = 0f; 

    private float _currentZ;
    public void SetPlayer(TopDownController tc) => playerController = tc;
    #endregion

    #region cycle
    private void Awake()
    {
        var follow = GetComponent<FollowTarget2D>();
        if (follow != null)
        {
            playerTransform = follow.Target;
            if (playerTransform != null)
                playerController = playerTransform.GetComponent<TopDownController>();
        }

        if (!playerController)
            playerController = FindAnyObjectByType<TopDownController>(FindObjectsInactive.Exclude);
        if (playerController && !playerTransform)
            playerTransform = playerController.transform;

        _currentZ = transform.eulerAngles.z;
    }

    private void Update()
    {
        if (!playerController) return;

        var dir = playerController.FacingDir;
        if (dir.sqrMagnitude <= 0.001f) return;

        float targetZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

        if (degreesPerSecond <= 0f)
            _currentZ = targetZ;
        else
            _currentZ = Mathf.MoveTowardsAngle(_currentZ, targetZ, degreesPerSecond * Time.deltaTime);

        ApplyZ(_currentZ);
    }
    #endregion

    #region helpers
    private void ApplyZ(float z)
    {
        var e = transform.eulerAngles; 
        e.x = 0f; 
        e.y = 0f; 
        e.z = z;
        transform.eulerAngles = e;
    }
    #endregion
}