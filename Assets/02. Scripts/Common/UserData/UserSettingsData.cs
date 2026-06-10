using System;
using UnityEngine;

public class UserSettingsData : IUserData
{
    public float MasterValue { get; set; }
    public float BGMvalue { get; set; }
    public float SFXvalue { get; set; }

    public void SetDefaultData()
    {
        MasterValue = 1f;
        BGMvalue = 0.5f;
        SFXvalue = 0.5f;
    }

    public bool LoadData()
    {
        if (PlayerPrefs.HasKey(nameof(BGMvalue)) && PlayerPrefs.HasKey(nameof(SFXvalue)))
        {
            MasterValue = PlayerPrefs.GetFloat(nameof(MasterValue), 1f);
            BGMvalue = PlayerPrefs.GetFloat(nameof(BGMvalue));
            SFXvalue = PlayerPrefs.GetFloat(nameof(SFXvalue));

            Debug.Log("데이터 로드 성공");
            return true;
        }

        Debug.LogWarning("저장된 데이터가 없습니다.");
        return false;
    }

    public bool SaveData()
    {
        try
        {
            PlayerPrefs.SetFloat(nameof(MasterValue), MasterValue);
            PlayerPrefs.SetFloat(nameof(BGMvalue), BGMvalue);
            PlayerPrefs.SetFloat(nameof(SFXvalue), SFXvalue);

            PlayerPrefs.Save();

            Debug.Log("데이터 저장 성공");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"데이터 저장 실패: {e.Message}");
            return false;
        }
    }
}