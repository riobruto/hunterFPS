using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform _focusTarget = default;
    [SerializeField, Range(1, 50)] private float _distance = 15f;
    [SerializeField, Min(0f)] private float _focusRadius = 1f;
    [SerializeField, Range(0f, 1f)] private float _focusCentering = 0.5f;
    [SerializeField, Range(1f, 360f)] private float _rotationSpeed = 90f;
    [SerializeField, Range(-89f, 89f)] private float _minVerticalAngle = -30f, _maxVerticalAngle = 60f;
    [SerializeField, Min(0f)] private float _alignDelay = 5f;
    [SerializeField, Range(0f, 90f)] float _alignSmoothRange = 45f;

    private void Awake()
    {
        _focusPoint = _focusTarget.position;
        transform.localRotation = Quaternion.Euler(_orbitAngles);
    }

    private Vector2 _orbitAngles;

    private void Update()
    {
        

        UpdateFocusPoint();
        Quaternion lookRotation;
        if (ManualRotation() || AutomaticRotation())
        {
            ConstrainAngles();
            lookRotation = Quaternion.Euler(_orbitAngles);
        }
        else
        {
            lookRotation = transform.localRotation;
        }

        _distance = Mathf.Clamp(_distance + Input.mouseScrollDelta.y, 1,100);

        Vector3 lookDirection = lookRotation * Vector3.forward;
        Vector3 lookPosition = _focusPoint - lookDirection * _distance;

        transform.SetPositionAndRotation(lookPosition, lookRotation);
    }

    private Vector3 _focusPoint, _previousFocusPoint;
    private float _lastManualRotationTime;

    private bool ManualRotation()
    {
        Vector2 input = Input.GetKey(KeyCode.LeftShift) ? new Vector2(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X")) : Vector2.zero;

        const float e = 0.001f;
        if (input.x < -e || input.x > e || input.y < -e || input.y > e)
        {
            _orbitAngles += _rotationSpeed * Time.unscaledDeltaTime * input;
            _lastManualRotationTime = Time.unscaledTime;
            return true;
        }
        return false;
    }

    private bool AutomaticRotation()
    {
        if (Time.unscaledTime - _lastManualRotationTime < _alignDelay)
        {
            return false;
        }


        Vector2 movement = new Vector2(
            _focusPoint.x - _previousFocusPoint.x,
            _focusPoint.z - _previousFocusPoint.z
        );

        float movementDeltaSqr = movement.sqrMagnitude;

        if (movementDeltaSqr < 0.0001f)
        {
            return false;
        }

        float headingAngle = GetAngle(movement / Mathf.Sqrt(movementDeltaSqr));
        float rotationChange = _rotationSpeed * Mathf.Min(Time.unscaledDeltaTime, movementDeltaSqr);

        float deltaAbs = Mathf.Abs(Mathf.DeltaAngle(_orbitAngles.y, headingAngle));
        
        if (deltaAbs < _alignSmoothRange)
        {
            rotationChange *= deltaAbs / _alignSmoothRange;
        }
        else if (180f - deltaAbs < _alignSmoothRange)
        {
            rotationChange *= (180f - deltaAbs) / _alignSmoothRange;
        }

        _orbitAngles.y = Mathf.MoveTowardsAngle(_orbitAngles.y, headingAngle, rotationChange);

        return true;
    }

    private void ConstrainAngles()
    {
        _previousFocusPoint = _focusPoint;
        _orbitAngles.x =
            Mathf.Clamp(_orbitAngles.x, _minVerticalAngle, _maxVerticalAngle);

        if (_orbitAngles.y < 0f)
        {
            _orbitAngles.y += 360f;
        }
        else if (_orbitAngles.y >= 360f)
        {
            _orbitAngles.y -= 360f;
        }
    }

    private void UpdateFocusPoint()
    {
        _previousFocusPoint = _focusPoint;

        Vector3 targetPoint = _focusTarget.position;
        float t = 1;
        if (_distance > 0.01f && _focusCentering > 0f)
        {
            t = Mathf.Pow(1f - _focusCentering, Time.unscaledDeltaTime);
        }

        if (_focusRadius > 0f)
        {
            float distance = Vector3.Distance(targetPoint, _focusPoint);
            if (distance > _focusRadius)
            {
                t = Mathf.Min(t, _focusRadius / distance);
            }
            _focusPoint = Vector3.Lerp(targetPoint, _focusPoint, t);
        }
        else
        {
            _focusPoint = targetPoint;
        }
        
    }

    private static float GetAngle(Vector2 direction)
    {
        float angle = Mathf.Acos(direction.y) * Mathf.Rad2Deg;
        return direction.x < 0f ? 360f - angle : angle; 
    }

    private void OnValidate()
    {
        if (_maxVerticalAngle < _minVerticalAngle)
        {
            _maxVerticalAngle = _minVerticalAngle;
        }
    }
}