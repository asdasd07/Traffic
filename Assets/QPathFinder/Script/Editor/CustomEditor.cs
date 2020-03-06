using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor.SceneManagement;

public class CustomGUI : Editor {
    public static GUIStyle GetStyleWithRichText(GUIStyle style = null) {
        style = style ?? new GUIStyle();
        style.richText = true;
        style.alignment = TextAnchor.MiddleCenter;
        return style;
    }
}
public class ColoredTexture {
    public Texture2D white, green, yellow, blue, red;
    public Texture2D[] colo;//przyjazd/dom/sklep/praca/wyjazd
    public ColoredTexture() {
        colo = new Texture2D[5];
        Color[] col = new Color[5] { new Color(0.9f, 0.9f, 0.9f), new Color(0, 1, 0), new Color(0, 0.75f, 1), new Color(1, 1, 0), new Color(1, 0.58f, 0.62f) };
        Color[][] fillColorArray = new Color[5][];
        for (int i = 0; i < 5; i++) {
            colo[i] = new Texture2D(20, 20);
            fillColorArray[i] = colo[i].GetPixels();
            for (var j = 0; j < fillColorArray[i].Length; ++j) {
                fillColorArray[i][j] = col[i];
            }
            colo[i].SetPixels(fillColorArray[i]);
            colo[i].Apply();
        }
        white = colo[0];
        green = colo[1];
        yellow = colo[2];
        blue = colo[3];
        red = colo[4];
    }

}