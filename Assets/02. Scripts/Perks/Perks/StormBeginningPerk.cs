using UnityEngine;

public class StormBeginningPerk : IPerk
{
    public void Apply(GameObject player, string perkId)
    {
        var attack = player.GetComponent<PlayerAttack>();
        attack.UnlockUltimate();
    }
}
