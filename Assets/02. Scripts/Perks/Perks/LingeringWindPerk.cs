using UnityEngine;

public class LingeringWindPerk : IPerk
{
    public void Apply(GameObject player, string perkId)
    {
        var attack = player.GetComponent<PlayerAttack>();
        attack.EnableLingeringWind(perkId);
    }
}
