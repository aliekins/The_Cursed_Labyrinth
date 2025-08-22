/// \file LightAim.cs
/// \brief Rotates a child light to match the player's facing direction

using UnityEngine;
using UnityEngine.InputSystem.XR;

public sealed class LightAim : MonoBehaviour
{
    [SerializeField] private float angleOffset = -90f;

    [Header("Optional (auto-wired)")]
    [SerializeField] private TopDownController playerController;   
    [SerializeField] private Transform playerTransform;

    [Header("Cardinal Z-rotations (deg)")]
    [SerializeField] private float rotDown = -180f;  // forward
    [SerializeField] private float rotRight = -90f;
    [SerializeField] private float rotUp = 0f;
    [SerializeField] private float rotLeft = 90f;

    [Header("Smoothing")]
    [SerializeField] private float degreesPerSecond = 0f; 

    private float _currentZ;
    public void SetPlayer(TopDownController tc) => playerController = tc;

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

    private void ApplyZ(float z)
    {
        var e = transform.eulerAngles; 
        e.x = 0f; 
        e.y = 0f; 
        e.z = z;
        transform.eulerAngles = e;
    }
}