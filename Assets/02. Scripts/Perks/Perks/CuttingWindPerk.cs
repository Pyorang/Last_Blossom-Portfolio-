using UnityEngine;

public class CuttingWindPerk : IPerk
{
    public void Apply(GameObject player, string perkId)
    {
        var attack = player.GetComponent<PlayerAttack>();
        attack.AddCritDamage(0.6f);
    }
}
