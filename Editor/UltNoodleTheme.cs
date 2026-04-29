#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public static class UltNoodleTheme
{
    public static bool IsDark => EditorGUIUtility.isProSkin;

    public static void ApplyThemeSheet(VisualElement root)
    {
        string folder = UltNoodleEditor.EditorFolder + "/Styles";
        string name = IsDark ? "UltNoodleDark" : "UltNoodleLight";
        var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>($"{folder}/{name}.uss");
        if (sheet != null) root.styleSheets.Add(sheet);
    }

    public static Color PanelBg => IsDark ? new Color(0.18f, 0.18f, 0.18f) : new Color(0.85f, 0.85f, 0.85f);
    public static Color PanelBorder => IsDark ? new Color(0.25f, 0.25f, 0.25f) : new Color(0.60f, 0.60f, 0.60f);
    public static Color TextPrimary => IsDark ? Color.white : Color.black;
    public static Color TextSecondary => IsDark ? Color.gray : new Color(0.3f, 0.3f, 0.3f);
    public static Color TextFieldBackground => IsDark ? Color.gray : new Color(0.22f, 0.22f, 0.22f);
    public static Color NoteBg => new Color(1f, 0.96f, 0.66f); // sticky note yellow, same both themes
    public static Color NoteBorder => IsDark ? new Color(0.15f, 0.15f, 0.15f) : new Color(0.45f, 0.45f, 0.45f);
    public static Color NoteText => Color.black; // always dark on yellow
    public static Color HoverBg => IsDark ? new Color(0.17f, 0.17f, 0.17f) : new Color(0.82f, 0.82f, 0.82f);
    public static Color FlowPortColor => IsDark ? Color.white : new Color(0.1f, 0.1f, 0.1f);
}
#endif