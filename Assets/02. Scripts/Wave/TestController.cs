using UnityEngine;

public class TestController : MonoBehaviour
{
    [SerializeField] private HealthComponent _playerHealthComponent;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (WaveManager.Instance == null)
            {
                Debug.LogError("[WaveTestController] WaveManager가 없음!");
                return;
            }

            if (WaveManager.Instance.IsWaveActive)
            {
                return;
            }

            int nextWave = WaveManager.Instance.CurrentWaveId + 1;
            WaveManager.Instance.StartWave(nextWave);
        }

        if(Input.GetKeyDown(KeyCode.H))
        {
            _playerHealthComponent.TakeDamage(50);
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            WaveManager.Instance.KillRandomMonster();
        }
    }
}
