using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaletteModule : MonoBehaviour
{
    [System.Serializable]
    public struct Palette
    {
        public Color[] palette;
    }
    [SerializeField]
    public Palette[] palettes = new Palette[] { new Palette() { palette = new Color[] { Color.red, Color.green, Color.blue, Color.black} } };

    void Awake()
    {
        // palettes = new List<Color[]>();
        // Color[] palette = new Color[4];
        // palette[0] = Color.red;
        // palette[1] = Color.green;
        // palette[2] = Color.blue;
        // palette[3] = Color.black;
        // palettes.Add(palette);
    }
}
