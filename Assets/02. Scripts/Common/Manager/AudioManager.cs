using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public enum AudioType
{
    BGM,
    SFX
}

public class AudioManager : SingletonBehaviour<AudioManager>
{
    private const string AUDIO_PATH = "Audio";

    [Header("SFX 풀 설정")]
    [SerializeField] private int _sfxPoolSize = 30;
    [SerializeField] private int _defaultMaxConcurrent = 4;

    [Header("피치 랜덤화")]
    [SerializeField] private float _pitchMin = 0.95f;
    [SerializeField] private float _pitchMax = 1.05f;

    [Header("3D 사운드 설정")]
    [SerializeField] private float _minDistance = 3f;
    [SerializeField] private float _maxDistance = 18f;
    [SerializeField] private float _spatialBlend3D = 1f;

    private AudioSource _bgmSource;
    private AudioSource[] _sfxPool;
    private float _sfxVolume = 1f;

    private Dictionary<string, AudioClip> _clips = new();
    private Dictionary<string, AsyncOperationHandle<AudioClip>> _handles = new();
    private Dictionary<string, int> _playingCounts = new();

    protected override void Init()
    {
        base.Init();
        CreateBGMSource();
        CreateSFXPool();
    }

    private void Start()
    {
        SyncUserSettings();
    }

    private void OnDestroy()
    {
        ReleaseAllClips();
    }

    private void CreateBGMSource()
    {
        var bgmObject = new GameObject("BGM");
        bgmObject.transform.SetParent(transform);
        _bgmSource = bgmObject.AddComponent<AudioSource>();
        _bgmSource.loop = true;
    }

    private void CreateSFXPool()
    {
        var poolObject = new GameObject("SFX_Pool");
        poolObject.transform.SetParent(transform);

        _sfxPool = new AudioSource[_sfxPoolSize];
        for (int i = 0; i < _sfxPoolSize; i++)
        {
            var sourceObject = new GameObject($"SFX_{i}");
            sourceObject.transform.SetParent(poolObject.transform);

            var source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = _minDistance;
            source.maxDistance = _maxDistance;
            source.dopplerLevel = 0f;

            _sfxPool[i] = source;
        }
    }

    public void PlaySFX(string fileName, bool randomPitch = true)
    {
        PlaySFXInternal(fileName, Vector3.zero, false, _defaultMaxConcurrent, randomPitch);
    }

    public void PlaySFX(string fileName, int maxConcurrent, bool randomPitch = true)
    {
        PlaySFXInternal(fileName, Vector3.zero, false, maxConcurrent, randomPitch);
    }

    public void PlaySFX3D(string fileName, Vector3 position, bool randomPitch = true)
    {
        PlaySFXInternal(fileName, position, true, _defaultMaxConcurrent, randomPitch);
    }

    public void PlaySFX3D(string fileName, Vector3 position, int maxConcurrent, bool randomPitch = true)
    {
        PlaySFXInternal(fileName, position, true, maxConcurrent, randomPitch);
    }

    private void PlaySFXInternal(string fileName, Vector3 position, bool is3D, int maxConcurrent, bool randomPitch)
    {
        if (!CanPlay(fileName, maxConcurrent))
        {
            return;
        }

        if (_clips.TryGetValue(fileName, out var clip))
        {
            PlayClip(fileName, clip, position, is3D, randomPitch);
            return;
        }

        LoadClipAsync(fileName, loadedClip =>
        {
            if (loadedClip != null && CanPlay(fileName, maxConcurrent))
            {
                PlayClip(fileName, loadedClip, position, is3D, randomPitch);
            }
        });
    }

    private bool CanPlay(string fileName, int maxConcurrent)
    {
        _playingCounts.TryGetValue(fileName, out int count);
        return count < maxConcurrent;
    }

    private void PlayClip(string fileName, AudioClip clip, Vector3 position, bool is3D, bool randomPitch)
    {
        var source = GetAvailableSource();
        if (source == null)
        {
            return;
        }

        source.clip = clip;
        source.volume = _sfxVolume;
        source.pitch = randomPitch ? UnityEngine.Random.Range(_pitchMin, _pitchMax) : 1f;
        source.spatialBlend = is3D ? _spatialBlend3D : 0f;
        source.transform.position = position;
        source.Play();

        StartCoroutine(TrackPlayback(fileName, clip.length));
    }

    private AudioSource GetAvailableSource()
    {
        foreach (var source in _sfxPool)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }
        return null;
    }

    private IEnumerator TrackPlayback(string fileName, float duration)
    {
        _playingCounts.TryGetValue(fileName, out int count);
        _playingCounts[fileName] = count + 1;

        yield return new WaitForSecondsRealtime(duration);

        _playingCounts[fileName]--;
        if (_playingCounts[fileName] <= 0)
        {
            _playingCounts.Remove(fileName);
        }
    }

    public void Play(AudioType audioType, string fileName, Action onComplete = null)
    {
        if (audioType == AudioType.SFX)
        {
            PlaySFX(fileName);
            onComplete?.Invoke();
            return;
        }

        if (_clips.TryGetValue(fileName, out var cachedClip))
        {
            PlayBGM(cachedClip);
            onComplete?.Invoke();
            return;
        }

        LoadClipAsync(fileName, clip =>
        {
            if (clip != null)
            {
                PlayBGM(clip);
            }
            onComplete?.Invoke();
        });
    }

    private void PlayBGM(AudioClip clip)
    {
        if (_bgmSource.isPlaying)
        {
            _bgmSource.Stop();
        }
        _bgmSource.clip = clip;
        _bgmSource.Play();
    }

    private void LoadClipAsync(string fileName, Action<AudioClip> onLoaded)
    {
        if (_handles.ContainsKey(fileName))
        {
            return;
        }

        string address = $"{AUDIO_PATH}/{fileName}";
        var handle = Addressables.LoadAssetAsync<AudioClip>(address);
        _handles[fileName] = handle;

        handle.Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                _clips[fileName] = op.Result;
                onLoaded?.Invoke(op.Result);
            }
            else
            {
                Debug.LogWarning($"[AudioManager] 오디오 클립 로드 실패: {fileName}");
                _handles.Remove(fileName);
                onLoaded?.Invoke(null);
            }
        };
    }

    public void PreloadClip(string fileName, Action onComplete = null)
    {
        if (_clips.ContainsKey(fileName))
        {
            onComplete?.Invoke();
            return;
        }

        LoadClipAsync(fileName, _ => onComplete?.Invoke());
    }

    public void PreloadClips(string[] fileNames, Action onComplete = null)
    {
        int remaining = fileNames.Length;
        if (remaining == 0)
        {
            onComplete?.Invoke();
            return;
        }

        foreach (var fileName in fileNames)
        {
            PreloadClip(fileName, () =>
            {
                remaining--;
                if (remaining <= 0)
                {
                    onComplete?.Invoke();
                }
            });
        }
    }

    private void ReleaseAllClips()
    {
        foreach (var handle in _handles.Values)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
        _handles.Clear();
        _clips.Clear();
        _playingCounts.Clear();
    }

    public void StopAllSFX()
    {
        foreach (var source in _sfxPool)
        {
            source.Stop();
        }
        _playingCounts.Clear();
    }

    public void SetVolume(AudioType audioType, float volume)
    {
        switch (audioType)
        {
            case AudioType.BGM:
                _bgmSource.volume = volume;
                break;
            case AudioType.SFX:
                _sfxVolume = volume;
                break;
        }
    }

    public float GetVolume(AudioType audioType)
    {
        return audioType == AudioType.BGM ? _bgmSource.volume : _sfxVolume;
    }

    public void SetPitch(AudioType audioType, float pitch)
    {
        if (audioType == AudioType.BGM)
        {
            _bgmSource.pitch = pitch;
        }
    }

    public void Pause(AudioType audioType)
    {
        if (audioType == AudioType.BGM)
        {
            _bgmSource.Pause();
        }
    }

    public void Resume(AudioType audioType)
    {
        if (audioType == AudioType.BGM)
        {
            _bgmSource.UnPause();
        }
    }

    public void Stop(AudioType audioType)
    {
        if (audioType == AudioType.BGM)
        {
            _bgmSource.Stop();
        }
        else
        {
            StopAllSFX();
        }
    }

    public void StopAll()
    {
        _bgmSource.Stop();
        StopAllSFX();
    }

    public void Mute()
    {
        SetVolume(AudioType.BGM, 0f);
        SetVolume(AudioType.SFX, 0f);
    }

    public void Unmute()
    {
        SetVolume(AudioType.BGM, 1f);
        SetVolume(AudioType.SFX, 1f);
    }

    public void SyncUserSettings()
    {
        var userSettingsData = UserDataManager.Instance.GetUserData<UserSettingsData>();
        AudioListener.volume = userSettingsData.MasterValue;
        SetVolume(AudioType.BGM, userSettingsData.BGMvalue);
        SetVolume(AudioType.SFX, userSettingsData.SFXvalue);
    }
}
