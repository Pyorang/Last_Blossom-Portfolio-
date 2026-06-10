using UnityEngine;

public class SwiftWindPerk : IPerk
{
    public void Apply(GameObject player, string perkId)
    {
        var attack = player.GetComponent<PlayerAttack>();
        attack.SetJustDamageBonus(1.3f);
    }
}
