using UnityEngine;

public class BurningWindPerk : IPerk
{
    public void Apply(GameObject player, string perkId)
    {
        var attack = player.GetComponent<PlayerAttack>();
        attack.SetESkillCoefficient(0.8f);
    }
}
