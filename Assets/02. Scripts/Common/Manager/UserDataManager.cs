using System.Collections.Generic;
using UnityEngine;

public class UserDataManager : SingletonBehaviour<UserDataManager>
{
    public bool ExistsSavedData { get; private set; } = false;
    private readonly List<IUserData> _userDataList = new();
    public IReadOnlyList<IUserData> UserDataList => _userDataList;

    protected override void Init()
    {
        base.Init();

        ExistsSavedData = PlayerPrefs.GetInt(nameof(ExistsSavedData)) == 1;
        _userDataList.Add(new UserSettingsData());

        if (ExistsSavedData)
        {
            LoadUserData();
        }
        else
        {
            SetDefaultData();
        }
    }

    public void SetDefaultData()
    {
        foreach (var data in UserDataList)
        {
            data.SetDefaultData();
        }
    }

    public void LoadUserData()
    {
        if (ExistsSavedData)
        {
            foreach (var data in UserDataList)
            {
                data.LoadData();
            }
        }
    }

    public void SaveUserData()
    {
        foreach (var data in UserDataList)
        {
            if (data.SaveData() == false)
            {
                Debug.LogError($"[UserDataManager] Failed to save data for {data.GetType().Name}");
                return;
            }
        }

        PlayerPrefs.SetInt(nameof(ExistsSavedData), 1);
        PlayerPrefs.Save();
        ExistsSavedData = true;
    }

    public T GetUserData<T>() where T : class, IUserData
    {
        for (int i = 0; i < _userDataList.Count; i++)
        {
            if (_userDataList[i] is T result)
            {
                return result;
            }
        }
        return null;
    }
}