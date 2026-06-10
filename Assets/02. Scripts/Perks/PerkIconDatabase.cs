using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PerkIconDatabase", menuName = "Game/Perk Icon Database")]
public class PerkIconDatabase : ScriptableObject
{
    [System.Serializable]
    public class PerkIconEntry
    {
        public string PerkId;
        public Sprite Icon;
    }

    [SerializeField] private List<PerkIconEntry> _entries = new();
    [SerializeField] private Sprite _defaultIcon;

    private Dictionary<string, Sprite> _iconDict;

    public Sprite GetIcon(string perkId)
    {
        if (_iconDict == null)
        {
            _iconDict = new Dictionary<string, Sprite>();
            foreach (var entry in _entries)
                _iconDict[entry.PerkId] = entry.Icon;
        }

        return _iconDict.TryGetValue(perkId, out var icon) ? icon : _defaultIcon;
    }
}