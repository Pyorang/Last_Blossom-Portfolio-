using UnityEngine;

public class PiercingWindPerk : IPerk
{
    public void Apply(GameObject player, string perkId)
    {
        var attack = player.GetComponent<PlayerAttack>();
        attack.EnablePiercingWind(perkId);
    }
}
