using UnityEngine;

public class TyphoonPerk : IPerk
{
    public void Apply(GameObject player, string perkId)
    {
        var attack = player.GetComponent<PlayerAttack>();
        attack.EnableTyphoon(perkId);
    }
}
