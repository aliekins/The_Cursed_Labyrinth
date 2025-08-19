/// \file CameraFollow.cs
/// \brief Makes the camera follow a target (the player).
using UnityEngine;

public sealed class CameraFollow : MonoBehaviour
{
    [SerializeField] private float smoothTime = 0.2f;

    private Transform target;
    private Vector3 velocity;

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = target.position;
        targetPos.z = transform.position.z; 
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);
    }

    public void SetTarget(Transform newTarget) => target = newTarget;
}