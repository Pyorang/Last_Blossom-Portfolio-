using UnityEngine;

public class LastingWindPerk : IPerk
{
    public void Apply(GameObject player, string perkId)
    {
        var awakening = player.GetComponent<AwakeningComponent>();
        awakening.AddAwakeningDuration(10f);
    }
}
