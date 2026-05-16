using UnityEngine;

namespace RocketsRanks
{
    public readonly struct TitleColorPreset
    {
        public readonly Color color;
        public readonly string LabelKey;

        public TitleColorPreset(string key, Color color)
        {
            this.color = color;
            LabelKey = "ROCKET_TitleColor_" + key;
        }
    }

    public static class TitleColorPresets
    {
        public static readonly Color Default = new(0.90f, 0.85f, 0.40f);

        public static readonly TitleColorPreset[] All =
        {
            new("Gold",   new Color(0.90f, 0.85f, 0.40f)),
            new("White",  new Color(1.00f, 1.00f, 1.00f)),
            new("Red",    new Color(0.90f, 0.40f, 0.40f)),
            new("Green",  new Color(0.40f, 0.90f, 0.40f)),
            new("Blue",   new Color(0.45f, 0.60f, 0.95f)),
            new("Purple", new Color(0.80f, 0.45f, 0.95f)),
            new("Cyan",   new Color(0.40f, 0.85f, 0.95f)),
            new("Orange", new Color(0.95f, 0.60f, 0.30f)),
        };
    }
}
