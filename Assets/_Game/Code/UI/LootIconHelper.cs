using System.Collections.Generic;
using ROS.Game.Core;
using UnityEngine;

namespace ROS.Game.UI
{
    internal static class LootIconHelper
    {
        private static readonly Dictionary<ItemType, Texture2D> _cache =
            new Dictionary<ItemType, Texture2D>();

        internal static Texture2D GetTexture(
            Sprite icon,
            ItemType itemType
        )
        {
            if (icon != null)
                return icon.texture;

            if (_cache.TryGetValue(itemType, out Texture2D cached))
                return cached;

            Color c = ColorForType(itemType);
            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, c);
            tex.Apply();
            _cache[itemType] = tex;
            return tex;
        }

        internal static Color GetIconColor(ItemType type) => ColorForType(type);

        private static Color ColorForType(ItemType type) => type switch
        {
            ItemType.Weapon     => new Color(0.90f, 0.28f, 0.20f),
            ItemType.Ammo       => new Color(0.95f, 0.80f, 0.10f),
            ItemType.Healing    => new Color(0.18f, 0.82f, 0.30f),
            ItemType.Armor      => new Color(0.30f, 0.52f, 0.85f),
            ItemType.Helmet     => new Color(0.35f, 0.70f, 0.85f),
            ItemType.Backpack   => new Color(0.82f, 0.52f, 0.18f),
            ItemType.Throwable  => new Color(0.72f, 0.28f, 0.85f),
            ItemType.Attachment => new Color(0.60f, 0.60f, 0.60f),
            _                   => new Color(0.55f, 0.55f, 0.55f),
        };
    }
}
