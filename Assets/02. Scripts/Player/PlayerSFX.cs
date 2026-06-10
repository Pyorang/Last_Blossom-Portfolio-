using UnityEngine;

public class PlayerSFX : MonoBehaviour
{
    private const string Attack1 = "플레이어_일반공격휘두름_01";
    private const string Attack2 = "플레이어_일반공격휘두름_02";
    private const string Attack3 = "플레이어_일반공격휘두름_03";
    private const string SkillCast = "플레이어_E스킬발동";
    private const string SkillHit = "플레이어_E스킬근접공격";
    private const string Evade = "플레이어_회피발동";
    private const string DrawSword = "플레이어_시작컷씬_칼잡기";
    private const string SheatheSword = "플레이어_칼집어넣음";
    private const string Awakening = "플레이어_각성_시작";
    private const string AwakeningIng = "플레이어_각성_유지";
    private const string KatanaPutIn_Swing = "플레이어_카타나집어넣기_칼돌림";
    private const string KatanaPutIn_End = "플레이어_칼집어넣음";
    private const string Hit01 = "플레이어_피격시_01";
    private const string Hit02 = "플레이어_피격시_02";
    private const string AcidHit = "원거리발사체_피격";

    private bool _useAcidDamageSFX;
    private const string SpecialAttack_Baldo = "플레이어궁극기_발도";
    private const string SpecialAttack_Slash_01 = "플레이어궁극기_이펙트1(회전)";
    private const string SpecialAttack_Slash_02 = "플레이어궁극기_이펙트2(벽력일섬)";

    private const string HitNormal1 = "적_피격_일반공격_1타";
    private const string HitNormal2 = "적_피격_일반공격_2타";
    private const string HitNormal3 = "적_피격_일반공격_3타";
    private const string HitSkillMelee = "적_피격_E스킬근접";
    private const string HitSkillProjectile = "적_피격_E스킬투사체";

    private const string StartVoice = "WOMAN14_A000";

    [Header("보이스 파일 (Addressables에 등록된 이름)")]
    [SerializeField] private string[] _attackVoices;
    [SerializeField] private string[] _damagedVoices;

    private int _lastAttackVoiceIndex = -1;
    private int _lastDamagedVoiceIndex = -1;

    public void PlayAttack1()
    {
        AudioManager.Instance.PlaySFX(Attack1);
    }

    public void PlayAttack2()
    {
        AudioManager.Instance.PlaySFX(Attack2);
    }

    public void PlayAttack3()
    {
        AudioManager.Instance.PlaySFX(Attack3);
    }

    public void PlaySkillCast()
    {
        AudioManager.Instance.PlaySFX(SkillCast);
    }

    public void PlaySkillHit()
    {
        AudioManager.Instance.PlaySFX(SkillHit);
    }

    public void PlayEvade()
    {
        AudioManager.Instance.PlaySFX(Evade);
    }

    public void PlayDrawSword()
    {
        AudioManager.Instance.PlaySFX(DrawSword);
    }

    public void PlaySheatheSword()
    {
        AudioManager.Instance.PlaySFX(SheatheSword);
    }

    public void PlayAwakening()
    {
        AudioManager.Instance.PlaySFX(Awakening);
    }

    public void PlayAwakeningIng()
    {
        AudioManager.Instance.PlaySFX(AwakeningIng);
    }

    public void PlayKatanaPutIn_Swing()
    {
        AudioManager.Instance.PlaySFX(KatanaPutIn_Swing);
    }

    public void PlayKatanaPutIn_End()
    {
        AudioManager.Instance.PlaySFX(KatanaPutIn_End);
    }

    public void PlayHitSFXAuto()
    {
        if (_useAcidDamageSFX)
        {
            _useAcidDamageSFX = false;
            AudioManager.Instance.PlaySFX(AcidHit);
        }
        else
        {
            AudioManager.Instance.PlaySFX(Random.value > 0.5f ? Hit01 : Hit02);
        }
    }

    public void SetAcidDamageSFX()
    {
        _useAcidDamageSFX = true;
    }

    public void PlaySpecialAttack_Baldo()
    {
        AudioManager.Instance.PlaySFX(SpecialAttack_Baldo);
    }

    public void PlaySpecialAttack_Slash_01()
    {
        AudioManager.Instance.PlaySFX(SpecialAttack_Slash_01);
    }

    public void PlaySpecialAttack_Slash_02()
    {
        AudioManager.Instance.PlaySFX(SpecialAttack_Slash_02);
    }

    public void PlayStartVoice()
    {
        AudioManager.Instance.PlaySFX(StartVoice);
    }

    public void Play(string fileName)
    {
        AudioManager.Instance.PlaySFX3D(fileName, transform.position);
    }

    public void PlayFixed(string fileName)
    {
        AudioManager.Instance.PlaySFX3D(fileName, transform.position, false);
    }

    public void PlayHitSFX(PlayerHitType hitType)
    {
        string sfxName = hitType switch
        {
            PlayerHitType.Normal1 => HitNormal1,
            PlayerHitType.Normal2 => HitNormal2,
            PlayerHitType.Normal3 => HitNormal3,
            PlayerHitType.SkillMelee => HitSkillMelee,
            PlayerHitType.SkillProjectile => HitSkillProjectile,
            PlayerHitType.Ultimate => HitNormal3,
            PlayerHitType.Perk => HitNormal3,
            _ => HitNormal1
        };

        AudioManager.Instance.PlaySFX(sfxName);
    }

    public void PlayAttackVoice()
    {
        _lastAttackVoiceIndex = PlayRandomVoice(_attackVoices, _lastAttackVoiceIndex);
    }

    public void PlayDamagedVoice()
    {
        _lastDamagedVoiceIndex = PlayRandomVoice(_damagedVoices, _lastDamagedVoiceIndex);
    }

    private int PlayRandomVoice(string[] voices, int lastIndex)
    {
        if (voices == null || voices.Length == 0) return -1;

        int index;
        if (voices.Length == 1)
        {
            index = 0;
        }
        else
        {
            do
            {
                index = Random.Range(0, voices.Length);
            } while (index == lastIndex);
        }

        AudioManager.Instance.PlaySFX(voices[index], 1, false);
        return index;
    }
}
