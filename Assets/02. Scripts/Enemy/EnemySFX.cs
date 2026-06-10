using System.Collections;
using UnityEngine;

public class EnemySFX : MonoBehaviour
{
    [Header("주기적 재생 설정")]
    [SerializeField] private bool _usePeriodicSound;
    [SerializeField] private string _periodicSoundBase;
    [SerializeField] private float _periodicMinInterval = 3f;
    [SerializeField] private float _periodicMaxInterval = 6f;
    [SerializeField] private int _periodicVariants = 2;

    [Header("루프 재생 설정")]
    [SerializeField] private bool _useLoopSound;
    [SerializeField] private string _loopSoundBase;
    [SerializeField] private int _loopVariants = 2;

    private AudioSource _loopSource;
    private Coroutine _periodicCoroutine;
    private Coroutine _loopCoroutine;
    private bool _isAlive;

    private Vector3 Position => transform.position;

    private void OnEnable()
    {
        _isAlive = true;

        if (_useLoopSound)
        {
            StartLoopSound();
        }

        if (_usePeriodicSound)
        {
            _periodicCoroutine = StartCoroutine(PeriodicSoundRoutine());
        }
    }

    private void OnDisable()
    {
        _isAlive = false;
        StopAllSounds();
    }

    private void StopAllSounds()
    {
        if (_loopSource != null && _loopSource.isPlaying)
        {
            _loopSource.Stop();
        }

        if (_periodicCoroutine != null)
        {
            StopCoroutine(_periodicCoroutine);
            _periodicCoroutine = null;
        }

        if (_loopCoroutine != null)
        {
            StopCoroutine(_loopCoroutine);
            _loopCoroutine = null;
        }
    }

    private void StartLoopSound()
    {
        if (_loopSource == null)
        {
            _loopSource = gameObject.AddComponent<AudioSource>();
            _loopSource.loop = true;
            _loopSource.spatialBlend = 1f;
            _loopSource.rolloffMode = AudioRolloffMode.Logarithmic;
            _loopSource.minDistance = 5f;
            _loopSource.maxDistance = 30f;
            _loopSource.dopplerLevel = 0f;
        }

        int variant = Random.Range(1, _loopVariants + 1);
        string fileName = $"{_loopSoundBase}_{variant:D2}";

        _loopCoroutine = StartCoroutine(LoadAndPlayLoop(fileName));
    }

    private IEnumerator LoadAndPlayLoop(string fileName)
    {
        var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<AudioClip>($"Audio/{fileName}");
        yield return handle;

        if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
        {
            if (_isAlive && _loopSource != null)
            {
                _loopSource.clip = handle.Result;
                _loopSource.volume = 0.5f;
                _loopSource.Play();
            }
        }

        _loopCoroutine = null;
    }

    private IEnumerator PeriodicSoundRoutine()
    {
        float initialDelay = Random.Range(0f, _periodicMaxInterval);
        yield return new WaitForSeconds(initialDelay);

        while (_isAlive)
        {
            int variant = Random.Range(1, _periodicVariants + 1);
            string fileName = $"{_periodicSoundBase}_{variant:D2}";
            AudioManager.Instance.PlaySFX3D(fileName, Position);

            float interval = Random.Range(_periodicMinInterval, _periodicMaxInterval);
            yield return new WaitForSeconds(interval);
        }
    }

    public void Play(string fileName)
    {
        AudioManager.Instance.PlaySFX3D(fileName, Position);
    }

    public void PlayFixed(string fileName)
    {
        AudioManager.Instance.PlaySFX3D(fileName, Position, false);
    }

    public void PlayRandom(string baseName, int variants = 2)
    {
        int variant = Random.Range(1, variants + 1);
        string fileName = $"{baseName}_{variant:D2}";
        AudioManager.Instance.PlaySFX3D(fileName, Position);
    }

    public void OnDeath()
    {
        _isAlive = false;
        StopAllSounds();
    }
}
