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
    private SerializedProperty vehicleCountProp;
    private SerializedProperty vehiclesProp;
    private SerializedProperty cellColorsProp;
    private SerializedProperty cellMaterialsProp;
    private SerializedProperty pipeDataProp;
    private SerializedProperty countdownProp;
    private int totalPassengers;

    private void OnEnable()
    {
        level = (LevelData)target;
        widthProp = serializedObject.FindProperty("width");
        heightProp = serializedObject.FindProperty("height");
        cellSizeProp = serializedObject.FindProperty("cellSize");
        cellsProp = serializedObject.FindProperty("cells");
        vehicleCountProp = serializedObject.FindProperty("vehicleCount");
        vehiclesProp = serializedObject.FindProperty("vehicles");
        cellColorsProp = serializedObject.FindProperty("cellColors");
        cellMaterialsProp = serializedObject.FindProperty("cellMaterials");
        pipeDataProp = serializedObject.FindProperty("pipeData");
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
        //EditorGUILayout.PropertyField(vehicleCountProp);
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
        SetVehicleList();
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
                SerializedProperty pdProp = pipeDataProp.GetArrayElementAtIndex(idx);

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
                else if (cellType == CellType.Pipe)
                {
                    // Show small pipe controls: pool size and a foldout to set materials
                    EditorGUILayout.BeginVertical(GUILayout.Width(120));
                    SerializedProperty poolSizeProp = pdProp.FindPropertyRelative("poolSize");
                    SerializedProperty matsProp = pdProp.FindPropertyRelative("materials");
                    SerializedProperty rotProp = pdProp.FindPropertyRelative("rotationY"); 

                    EditorGUI.BeginChangeCheck();
                    int newPool = EditorGUILayout.IntField(GUIContent.none, poolSizeProp.intValue, GUILayout.Width(56), GUILayout.Height(16));
                    if (newPool < 0) newPool = 0;
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(level, "Change Pipe Pool Size");
                        poolSizeProp.intValue = newPool;
                        // ensure materials array matches new size
                        while (matsProp.arraySize < newPool)
                            matsProp.InsertArrayElementAtIndex(matsProp.arraySize);
                        while (matsProp.arraySize > newPool)
                            matsProp.DeleteArrayElementAtIndex(matsProp.arraySize - 1);
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(level);
                    }

                    // edit pipe rotation
                    EditorGUI.BeginChangeCheck();
                    float newRot = 0f;
                    if (rotProp != null) newRot = rotProp.floatValue;
                    newRot = EditorGUILayout.FloatField(GUIContent.none, newRot, GUILayout.Width(56), GUILayout.Height(16));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(level, "Change Pipe Rotation");
                        if (rotProp != null)
                            rotProp.floatValue = newRot;
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(level);
                    }

                    // small foldout style (inline material fields are too wide in grid; show a tiny button to expand full editor)
                    if (GUILayout.Button("Edit Pipe", GUILayout.Width(56), GUILayout.Height(16)))
                    {
                        // open a small popup window — to keep things simple here, we just expand a larger inspector area below the grid
                        // We will draw the full pipe data below the grid after the main grid loop by storing the last clicked pipe index.
                        // Simpler: open inspector selection for LevelData and let user scroll — but we provide inline small material slots in expanded area below.
                        // For now we just show nothing inline, user can edit pipe materials from the "Pipe Details" section below.
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

        DrawFullPipeDetails();
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

        int pipeColorlessCount = 0; // number of pipe pool entries with no material assigned
        totalPassengers = 0;

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

        // Count pipe pool passengers: include poolSize in totalPassengers and include their materials in matCounts
        if (pipeDataProp != null)
        {
            // iterate pipeDataProp entries; check the corresponding cell is Pipe
            int pipeEntries = pipeDataProp.arraySize;
            for (int idx = 0; idx < pipeEntries; idx++)
            {
                if (idx >= cellsProp.arraySize) break;
                var cellType = (CellType)cellsProp.GetArrayElementAtIndex(idx).enumValueIndex;
                if (cellType != CellType.Pipe) continue;

                SerializedProperty pdProp = pipeDataProp.GetArrayElementAtIndex(idx);
                SerializedProperty poolSizeProp = pdProp.FindPropertyRelative("poolSize");
                SerializedProperty matsProp = pdProp.FindPropertyRelative("materials");

                int poolSize = Mathf.Max(0, poolSizeProp != null ? poolSizeProp.intValue : 0);
                totalPassengers += poolSize;

                // Count materials listed for this pipe. We iterate the materials array (it should be sized to poolSize)
                if (matsProp != null)
                {
                    for (int m = 0; m < matsProp.arraySize; m++)
                    {
                        SerializedProperty el = matsProp.GetArrayElementAtIndex(m);
                        Material mat = el.objectReferenceValue as Material;
                        if (mat != null)
                        {
                            if (!matCounts.ContainsKey(mat)) matCounts[mat] = 0;
                            matCounts[mat]++;
                        }
                        else
                        {
                            // material not assigned for this pool slot
                            pipeColorlessCount++;
                        }
                    }
                }
                else
                {
                    // No materials array found; treat all pool entries as colorless
                    pipeColorlessCount += poolSize;
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

        // Pipe entries that had no material assigned
        if (pipeColorlessCount > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Pipe pool (no material):", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField($"× {pipeColorlessCount}");
        }

        // Nothing case
        if (matCounts.Count == 0 && colorCounts.Count == 0)
        {
            EditorGUILayout.HelpBox("No passenger cells with material or color assigned yet.", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }

    // SetVehicleList function will have (totalPassengers / 3) vehicles
    private void SetVehicleList()
    {
        int desiredCount = Mathf.Max(1, totalPassengers / 3);
        if (level.vehicleCount != desiredCount)
        {
            level.vehicleCount = desiredCount;
            EditorUtility.SetDirty(level);
        }
        if (level.vehicles == null)
            level.vehicles = new List<GameObject>();
        //level.vehicles = new List<Vehicle>();
        while (level.vehicles.Count < desiredCount)
            level.vehicles.Add(null);
        while (level.vehicles.Count > desiredCount)
            level.vehicles.RemoveAt(level.vehicles.Count - 1);
        // Draw the vehicle list property
        EditorGUILayout.PropertyField(vehiclesProp, new GUIContent("Vehicles"), true);
    }

    void DrawFullPipeDetails()
    {
        // After drawing the grid, draw full pipe details (for all pipe cells) to allow editing materials lists
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Pipe Details (per Pipe cell)", EditorStyles.boldLabel);
        for (int idx = 0; idx < pipeDataProp.arraySize; idx++)
        {
            if (idx >= cellsProp.arraySize) break;
            var t = (CellType)cellsProp.GetArrayElementAtIndex(idx).enumValueIndex;
            if (t != CellType.Pipe) continue;

            int px = idx % level.width;
            int py = idx / level.width;
            SerializedProperty pd = pipeDataProp.GetArrayElementAtIndex(idx);
            SerializedProperty poolSizeProp = pd.FindPropertyRelative("poolSize");
            SerializedProperty matsProp = pd.FindPropertyRelative("materials");

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField($"Pipe at ({px},{py})", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            int newPool = EditorGUILayout.IntField("Pool Size", poolSizeProp.intValue);
            //Debug.Log($"Editing pipe at ({px},{py}) with current pool size {poolSizeProp.intValue}");
            if (newPool < 0) newPool = 0;
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(level, "Change Pipe Pool Size (Details)");
                poolSizeProp.intValue = newPool;
                // resize materials array accordingly
                while (matsProp.arraySize < newPool)
                    matsProp.InsertArrayElementAtIndex(matsProp.arraySize);
                while (matsProp.arraySize > newPool && matsProp.arraySize > 0)
                    matsProp.DeleteArrayElementAtIndex(matsProp.arraySize - 1);
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(level);
            }
            SerializedProperty rotProp = pd.FindPropertyRelative("rotationY");

            // rotation Y field - only if property exists
            if (rotProp != null)
            {
                EditorGUI.BeginChangeCheck();
                float newRotation = EditorGUILayout.FloatField("Rotation Y (deg)", rotProp.floatValue);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(level, "Change Pipe Rotation (Details)");
                    rotProp.floatValue = newRotation;
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(level);
                }
            }
            else
            {
                EditorGUILayout.LabelField("Rotation Y (deg): (not available)");
            }


            // Draw materials list with indices
            EditorGUILayout.LabelField("Passenger Materials:");
            for (int i = 0; i < matsProp.arraySize; i++)
            {
                SerializedProperty el = matsProp.GetArrayElementAtIndex(i);
                EditorGUI.BeginChangeCheck();
                Object newMat = EditorGUILayout.ObjectField($"#{i}", el.objectReferenceValue, typeof(Material), false);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(level, "Change Pipe Passenger Material");
                    el.objectReferenceValue = (Material)newMat;
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(level);
                }
            }

            EditorGUILayout.EndVertical();
        }
    }
}
