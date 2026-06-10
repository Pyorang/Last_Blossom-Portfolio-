using UnityEngine;

public class WhirlWindPerk : IPerk
{
    public void Apply(GameObject player, string perkId)
    {
        var attack = player.GetComponent<PlayerAttack>();
        attack.EnableWhirlWind(perkId);
    }
}
