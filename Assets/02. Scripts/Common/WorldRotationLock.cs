using UnityEngine;

public class WorldRotationLock : MonoBehaviour
{
    private Quaternion _fixedRotation;

    private void Start()
    {
        _fixedRotation = transform.rotation;
    }

    private void LateUpdate()
    {
        transform.rotation = _fixedRotation;
    }
}
