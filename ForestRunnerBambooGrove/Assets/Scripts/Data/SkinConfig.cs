using UnityEngine;

namespace ForestRunner
{
    /// <summary>ScriptableObject describing a character skin (purchasable in the shop).</summary>
    [CreateAssetMenu(fileName = "Skin", menuName = "Forest Runner/Skin Config")]
    public class SkinConfig : ScriptableObject
    {
        public int skinIndex;
        public string skinName = "Explorer";
        public int price = 0;
        public Color shirt = new Color(0.8f, 0.3f, 0.2f, 1f);
        public Color pants = new Color(0.25f, 0.3f, 0.45f, 1f);
        public Color hat = new Color(0.75f, 0.6f, 0.3f, 1f);
        public Color skinTone = new Color(0.95f, 0.76f, 0.6f, 1f);
    }
}
