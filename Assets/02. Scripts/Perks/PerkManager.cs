using UnityEngine;

public class PerkManager : SingletonBehaviour<PerkManager>
{
    [SerializeField] private GameObject _player;

    private PlayerPerkHandler _perkHandler;

    protected override void Init()
    {
        IsDestroyOnLoad = true;
        base.Init();
    }

    private void Start()
    {
        _perkHandler = _player.GetComponent<PlayerPerkHandler>();
    }

    public void ApplyPerk(string perkId)
    {
        IPerk perk = PerkFactory.Create(perkId);
        if (perk != null)
        {
            perk.Apply(_player, perkId);
            _perkHandler?.RegisterPerk(perkId);
        }
    }

    public void ApplyPerk(IPerk perk, string perkId)
    {
        perk.Apply(_player, perkId);
        _perkHandler?.RegisterPerk(perkId);
    }
}
