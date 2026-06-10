using UnityEngine;

public class WildWindPerk : IPerk
{
    public void Apply(GameObject player, string perkId)
    {
        var attack = player.GetComponent<PlayerAttack>();
        attack.EnableWildWind(perkId);
    }
}
