using UnityEngine;

public class SuddenGustPerk : IPerk
{
    public void Apply(GameObject player, string perkId)
    {
        var awakening = player.GetComponent<AwakeningComponent>();
        awakening.AddHitAwakeningRecoveryPercent(0.5f);
    }
}
