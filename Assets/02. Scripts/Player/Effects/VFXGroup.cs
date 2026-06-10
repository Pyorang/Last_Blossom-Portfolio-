using UnityEngine;

public class VFXGroup : MonoBehaviour
{
    [SerializeField] private ParticleSystem[] _particleSystems;

    private void Awake()
    {
        foreach (var ps in _particleSystems)
        {
            if (ps != null)
                ps.gameObject.SetActive(false);
        }
    }

    public void PlayAll()
    {
        foreach (var ps in _particleSystems)
        {
            if (ps != null)
            {
                ps.gameObject.SetActive(true);
                ps.Play();
            }
        }
    }
}
