// Assets/Editor/LevelDataEditor.cs
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
    private SerializedProperty countdownProp;

    private void OnEnable()
    {
        level = (LevelData)target;
        widthProp = serializedObject.FindProperty("width");
        heightProp = serializedObject.FindProperty("height");
        cellSizeProp = serializedObject.FindProperty("cellSize");
        cellsProp = serializedObject.FindProperty("cells");
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
            for (int i = 0; i < level.cells.Count; i++) level.cells[i] = CellType.Empty;
            serializedObject.Update();
            EditorUtility.SetDirty(level);
        }
        if (GUILayout.Button("Fill All with Passenger"))
        {
            Undo.RecordObject(level, "Fill Grid");
            for (int i = 0; i < level.cells.Count; i++) level.cells[i] = CellType.ColorPassenger;
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
                if (idx < 0 || idx >= cellsProp.arraySize) continue;

                SerializedProperty cellProp = cellsProp.GetArrayElementAtIndex(idx);
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
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
