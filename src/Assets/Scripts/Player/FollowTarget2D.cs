using UnityEngine;

[DisallowMultipleComponent]
public sealed class FollowTarget2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField, Min(0f)] private float smoothTime = 0f;
    [SerializeField] private Vector3 worldOffset; 

    private Vector3 _velocity;

    private void LateUpdate()
    {
        if (!target) return;

        var desired = target.position + worldOffset;
        desired.z = transform.position.z;
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);
    }

    public void SetTarget(Transform t) => target = t;
    public void SetOffset(Vector3 offset) => worldOffset = offset;
    public void SetSmooth(float t) => smoothTime = Mathf.Max(0f, t);
}