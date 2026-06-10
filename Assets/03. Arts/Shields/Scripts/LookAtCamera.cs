using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    private Camera _camera;
    
    private void Start()
    {
        _camera = Camera.main;
        
        if (_camera == null)
        {
            enabled = false;
        }
    }
    
    private void Update()
    {
        if (_camera == null)
        {
            return;
        }
        
        transform.forward = _camera.transform.position - transform.position;
    }
}
