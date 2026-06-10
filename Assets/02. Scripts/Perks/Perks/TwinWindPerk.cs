using UnityEngine;

public class TwinWindPerk : IPerk
{
    public void Apply(GameObject player, string perkId)
    {
        var attack = player.GetComponent<PlayerAttack>();
        attack.EnableTwinWind(perkId);
    }
}
