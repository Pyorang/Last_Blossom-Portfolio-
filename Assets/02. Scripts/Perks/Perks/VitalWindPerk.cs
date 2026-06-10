using UnityEngine;

public class VitalWindPerk : IPerk
{
    public void Apply(GameObject player, string perkId)
    {
        var attack = player.GetComponent<PlayerAttack>();
        attack.EnableVitalWind(perkId);
    }
}
