using UnityEngine;

public class ReturnWindPerk : IPerk
{
    public void Apply(GameObject player, string perkId)
    {
        var attack = player.GetComponent<PlayerAttack>();
        attack.EnableReturnWind(perkId);
    }
}
