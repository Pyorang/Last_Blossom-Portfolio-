using UnityEngine;

public class TailWind : IPerk
{
    public void Apply(GameObject player, string perkId)
    {
        var attack = player.GetComponent<PlayerAttack>();
        attack.AddHitEnergyRecovery(0.15f);
    }
}
