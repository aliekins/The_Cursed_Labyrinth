/// \file PlayerLight.cs
/// \brief Makes the Spot Light 2D object follow a target (the player).
using UnityEngine;

public sealed class PlayerLight : MonoBehaviour
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