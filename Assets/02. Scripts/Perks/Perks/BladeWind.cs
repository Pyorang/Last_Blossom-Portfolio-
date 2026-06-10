using UnityEngine;

public class BladeWind : IPerk
{
    public void Apply(GameObject player, string perkId)
    {
        var attack = player.GetComponent<PlayerAttack>();
        attack.AddCritRate(0.15f);
    }
}
