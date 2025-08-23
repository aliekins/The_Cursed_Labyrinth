using UnityEngine;

[DisallowMultipleComponent]
public sealed class FollowTarget2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField, Min(0f)] private float smoothTime = 0f;
    [SerializeField] private Vector3 worldOffset; 

    private Vector3 _velocity;

    private void Start()
    {
        if (!target)
        {
            Debug.LogError("FollowTarget2D requires a target to follow. Please assign a target in the inspector.");
            enabled = false; // Disable the script if no target is set
        }
        else
        {
            transform.position = new Vector3(transform.position.x + worldOffset.x, transform.position.y +worldOffset.y, target.position.z); // Ensure z is zero for 2D
        }
    }
    private void Update()
    {
        if (!target) return;

        var desired = target.position + worldOffset;
        desired.z = transform.position.z;
        if (smoothTime <= 0f)
            transform.position = desired;
        else
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);
    }

    public void SetTarget(Transform t) => target = t;
    public void SetOffset(Vector3 offset) => worldOffset = offset;
    public void SetSmooth(float t) => smoothTime = Mathf.Max(0f, t);
    public Transform Target => target;
}