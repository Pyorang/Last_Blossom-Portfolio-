using UnityEngine;

public class SuctionWindPerk : IPerk
{
    public void Apply(GameObject player, string perkId)
    {
        var attack = player.GetComponent<PlayerAttack>();
        attack.EnableSuctionWind(perkId);
    }
}
