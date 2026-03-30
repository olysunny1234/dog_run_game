using UnityEngine;

/// <summary>
/// Fixed-angle camera that follows the dog's position but NOT rotation.
/// The world stays still when the dog turns — only MMB drag rotates the view.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [HideInInspector] public Transform target;
    public float distance    = 9f;
    public float height      = 4f;
    public float smoothSpeed = 5f;
    public float minDistance  = 3f;
    public float maxDistance  = 25f;
    public float zoomSpeed   = 4f;
    public float orbitSpeed  = 3f;

    private float _camYaw;
    private float _camPitch = 25f;
    private float _targetDistance;

    private void Start()
    {
        _targetDistance = distance;
        // Initialize yaw to look from behind the dog's starting direction
        if (target != null)
            _camYaw = target.eulerAngles.y;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // MMB drag = orbit camera around the dog
        if (Input.GetMouseButton(2))
        {
            _camYaw   += Input.GetAxis("Mouse X") * orbitSpeed;
            _camPitch -= Input.GetAxis("Mouse Y") * orbitSpeed;
            _camPitch  = Mathf.Clamp(_camPitch, 5f, 80f);
        }

        // Scroll wheel = zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            _targetDistance -= scroll * zoomSpeed;
            _targetDistance = Mathf.Clamp(_targetDistance, minDistance, maxDistance);
        }
        distance = Mathf.Lerp(distance, _targetDistance, Time.deltaTime * 8f);

        // Position camera at fixed yaw/pitch around the dog (no auto-rotation)
        float radPitch       = _camPitch * Mathf.Deg2Rad;
        float horizontalDist = distance * Mathf.Cos(radPitch);
        float verticalDist   = distance * Mathf.Sin(radPitch);

        Quaternion rot = Quaternion.Euler(0f, _camYaw, 0f);
        Vector3 desiredPos = target.position + rot * new Vector3(0f, verticalDist, -horizontalDist);

        // Smooth position follow
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * 0.9f);
    }
}
