// Assets/Editor/LevelDataEditor.cs
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelData))]
public class LevelDataEditor : Editor
{
    private LevelData level;
    private SerializedProperty widthProp;
    private SerializedProperty heightProp;
    private SerializedProperty cellSizeProp;
    private SerializedProperty cellsProp;
    private SerializedProperty cellColorsProp;
    private SerializedProperty cellMaterialsProp;
    private SerializedProperty countdownProp;

    private void OnEnable()
    {
        level = (LevelData)target;
        widthProp = serializedObject.FindProperty("width");
        heightProp = serializedObject.FindProperty("height");
        cellSizeProp = serializedObject.FindProperty("cellSize");
        cellsProp = serializedObject.FindProperty("cells");
        cellColorsProp = serializedObject.FindProperty("cellColors");
        cellMaterialsProp = serializedObject.FindProperty("cellMaterials");
        countdownProp = serializedObject.FindProperty("countdown");

        // ensure cells list is valid on enable
        level.ResizeCells();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw basic properties and detect changes so we can resize the grid immediately
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(widthProp);
        EditorGUILayout.PropertyField(heightProp);
        EditorGUILayout.PropertyField(cellSizeProp);
        EditorGUILayout.PropertyField(countdownProp);
        if (EditorGUI.EndChangeCheck())
        {
            // Apply changes to serialized props first
            serializedObject.ApplyModifiedProperties();

            // Resize underlying cells list and refresh UI
            level.ResizeCells();
            EditorUtility.SetDirty(level);

            // Re-fetch serialized representation (important after Resize)
            serializedObject.Update();
        }

        // NEW: Draw live counts of materials/colors used by passenger cells
        DrawColorCounts();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Grid Editor (top row first)", EditorStyles.boldLabel);

        if (level.width <= 0 || level.height <= 0)
        {
            EditorGUILayout.HelpBox("Width and Height must be positive to edit grid.", MessageType.Info);
            serializedObject.ApplyModifiedProperties();
            return;
        }

        DrawGridDropdowns();

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear All"))
        {
            Undo.RecordObject(level, "Clear Grid");
            for (int i = 0; i < level.cells.Count; i++)
            {
                level.cells[i] = CellType.Empty;
                level.cellColors[i] = Color.white;
                level.cellMaterials[i] = null;
            }
            serializedObject.Update();
            EditorUtility.SetDirty(level);
        }
        if (GUILayout.Button("Fill All with Passenger"))
        {
            Undo.RecordObject(level, "Fill Grid");
            for (int i = 0; i < level.cells.Count; i++) 
                level.cells[i] = CellType.ColorPassenger;
            serializedObject.Update();
            EditorUtility.SetDirty(level);
        }
        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawGridDropdowns()
    {
        int w = level.width;
        int h = level.height;

        // The enum names for the popup options
        string[] options = System.Enum.GetNames(typeof(CellType));

        // button width calculation (adjust to taste)
        float btnWidth = Mathf.Clamp(64, 40, 120);

        // Draw rows top -> bottom so inspector shows top row first
        for (int y = h - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);
            for (int x = 0; x < w; x++)
            {
                int idx = y * w + x;
                if (idx < 0 || idx >= cellsProp.arraySize || idx >= cellColorsProp.arraySize || idx >= cellMaterialsProp.arraySize) continue;

                SerializedProperty cellProp = cellsProp.GetArrayElementAtIndex(idx);
                SerializedProperty colorProp = cellColorsProp.GetArrayElementAtIndex(idx);
                SerializedProperty materialProp = cellMaterialsProp.GetArrayElementAtIndex(idx);
                int enumVal = cellProp.enumValueIndex;

                // Display a compact enum popup
                EditorGUI.BeginChangeCheck();
                int newVal = EditorGUILayout.Popup(enumVal, options, GUILayout.Width(btnWidth), GUILayout.Height(20));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(level, "Change Cell Type");
                    cellProp.enumValueIndex = newVal;
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(level);
                }

                // If this cell is a passenger-type, show a compact color field + material field next to it
                var cellType = (CellType)cellProp.enumValueIndex;
                if (cellType == CellType.ColorPassenger || cellType == CellType.HiddenColorPassenger)
                {
                    EditorGUILayout.BeginVertical(GUILayout.Width(56));
                    EditorGUI.BeginChangeCheck();
                    Color newColor = EditorGUILayout.ColorField(GUIContent.none, colorProp.colorValue, false, false, false, GUILayout.Width(48), GUILayout.Height(16));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(level, "Change Cell Color");
                        colorProp.colorValue = newColor;
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(level);
                    }

                    EditorGUI.BeginChangeCheck();
                    Object newMat = EditorGUILayout.ObjectField(materialProp.objectReferenceValue, typeof(Material), false, GUILayout.Width(56), GUILayout.Height(16));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(level, "Change Cell Material");
                        materialProp.objectReferenceValue = (Material)newMat;
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(level);
                    }
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    // maintain consistent spacing for non-color cells
                    GUILayout.Space(56);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    /// <summary>
    /// Draws a small summary box that lists how many passenger cells use each material,
    /// and how many use only plain colors (no material). Updates live.
    /// GPT GENERATED FOR DEMO PURPOSES ONLY - may need adjustments.
    /// </summary>
    private void DrawColorCounts()
    {
        if (level == null) return;

        // Count only passenger-type cells
        Dictionary<Material, int> matCounts = new Dictionary<Material, int>();
        Dictionary<Color, int> colorCounts = new Dictionary<Color, int>();

        int totalPassengers = 0;

        int w = level.width;
        int h = level.height;
        int total = Mathf.Max(0, w * h);

        for (int idx = 0; idx < total; idx++)
        {
            if (idx >= level.cells.Count) break;
            var t = level.cells[idx];
            if (t == CellType.ColorPassenger || t == CellType.HiddenColorPassenger)
            {
                totalPassengers++;
                Material mat = (idx < level.cellMaterials.Count) ? level.cellMaterials[idx] : null;
                if (mat != null)
                {
                    if (!matCounts.ContainsKey(mat)) matCounts[mat] = 0;
                    matCounts[mat]++;
                }
                else
                {
                    Color col = (idx < level.cellColors.Count) ? level.cellColors[idx] : Color.white;
                    if (!colorCounts.ContainsKey(col)) colorCounts[col] = 0;
                    colorCounts[col]++;
                }
            }
        }

        // Draw the UI
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Passenger Color / Material counts", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Total passenger cells: {totalPassengers}");

        // Materials first
        if (matCounts.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("By Material:", EditorStyles.miniBoldLabel);
            foreach (var kv in matCounts)
            {
                EditorGUILayout.BeginHorizontal();
                // show material object field (readonly)
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(kv.Key, typeof(Material), false, GUILayout.Width(160));
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.LabelField($"× {kv.Value}", GUILayout.Width(50));
                EditorGUILayout.EndHorizontal();
            }
        }

        // Then colors without materials
        if (colorCounts.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("By Color (no material):", EditorStyles.miniBoldLabel);
            foreach (var kv in colorCounts)
            {
                EditorGUILayout.BeginHorizontal();
                // small color swatch
                Rect r = GUILayoutUtility.GetRect(24, 16, GUILayout.Width(24));
                EditorGUI.DrawRect(r, kv.Key);
                GUILayout.Space(8);
                EditorGUILayout.LabelField($"× {kv.Value}", GUILayout.Width(50));
                EditorGUILayout.EndHorizontal();
            }
        }

        // Nothing case
        if (matCounts.Count == 0 && colorCounts.Count == 0)
        {
            EditorGUILayout.HelpBox("No passenger cells with material or color assigned yet.", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }
}
