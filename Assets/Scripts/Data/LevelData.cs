using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum CellType
{
    Empty,
    ColorPassenger,
    HiddenColorPassenger,
    Obstacle,
    Pipe
}

[Serializable]
public class PipeData
{
    public int poolSize = 3;
    public List<Material> materials = new List<Material>();
    public float rotationY = 0f;
}

[CreateAssetMenu(fileName = "LevelData", menuName = "Level/Simple Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Grid Settings")]
    public int width = 5;      // columns
    public int height = 3;     // rows
    public float cellSize = 2f;// size of one cell in world units

    [Header("Countdown for Losing")]
    [SerializeField] float countdown = 90f; // seconds for the level

    // flattened row-major list: index = y * width + x
    public List<CellType> cells = new List<CellType>();

    // parallel per-cell colors (used only when the cell is a passenger type)
    public List<Color> cellColors = new List<Color>();

    // per-cell pipe data (pool size + materials list)
    public List<PipeData> pipeData = new List<PipeData>();

    // to set the passenger's material at runtime (use GetCellMaterial in spawner).
    public List<Material> cellMaterials = new List<Material>();

    public List<GameObject> vehicles = new List<GameObject>();
    public int vehicleCount;

    // Unity callback in the editor when values change — keeps the list in sync
    private void OnValidate()
    {
        ResizeCells();
    }

    // Ensure the cells list size matches width*height
    public void ResizeCells()
    {
        int target = Mathf.Max(0, width * height);
        if (cells == null) cells = new List<CellType>();
        if (cellColors == null) cellColors = new List<Color>();
        if (cellMaterials == null) cellMaterials = new List<Material>();
        if (pipeData == null) pipeData = new List<PipeData>();

        if (cells.Count > target)
        {
            cells.RemoveRange(target, cells.Count - target);
        }
        else
        {
            while (cells.Count < target)
                cells.Add(CellType.Empty);
        }

        // Keep colors in sync with cells list length
        if (cellColors.Count > target)
        {
            cellColors.RemoveRange(target, cellColors.Count - target);
        }
        else
        {
            while (cellColors.Count < target)
                cellColors.Add(Color.white); // default color
        }

        // Keep materials in sync with cells list length
        if (cellMaterials.Count > target)
        {
            cellMaterials.RemoveRange(target, cellMaterials.Count - target);
        }
        else
        {
            while (cellMaterials.Count < target)
                cellMaterials.Add(null); // default no material
        }

        // Resize pipeData (each element holds pool size + list of materials)
        if (pipeData.Count > target)
            pipeData.RemoveRange(target, pipeData.Count - target);
        else
        {
            while (pipeData.Count < target)
            {
                var pd = new PipeData();
                pd.poolSize = 3;
                pd.materials = new List<Material>();
                pipeData.Add(pd);
            }
        }
    }

    // Safe accessor
    public CellType GetCell(int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) 
            return CellType.Empty;
        int idx = y * width + x;
        if (idx < 0 || idx >= cells.Count) 
            return CellType.Empty;
        return cells[idx];
    }

    // Safe setter
    public void SetCell(int x, int y, CellType t)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) 
            return;
        int idx = y * width + x;
        if (idx < 0 || idx >= cells.Count) 
            return;
        cells[idx] = t;
    }

    // Safe color accessor (returns white if out-of-range)
    public Color GetCellColor(int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return Color.white;
        int idx = y * width + x;
        if (idx < 0 || idx >= cellColors.Count) return Color.white;
        return cellColors[idx];
    }

    // Safe color setter
    public void SetCellColor(int x, int y, Color c)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return;
        int idx = y * width + x;
        if (idx < 0 || idx >= cellColors.Count) return;
        cellColors[idx] = c;
    }

    // Safe material accessor (returns null if out-of-range)
    public Material GetCellMaterial(int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return null;
        int idx = y * width + x;
        if (idx < 0 || idx >= cellMaterials.Count) return null;
        return cellMaterials[idx];
    }

    // Safe material setter
    public void SetCellMaterial(int x, int y, Material mat)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return;
        int idx = y * width + x;
        if (idx < 0 || idx >= cellMaterials.Count) return;
        cellMaterials[idx] = mat;
    }

    // Get pipe data for a cell (may return default PipeData)
    public PipeData GetPipeData(int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return null;
        int idx = y * width + x;
        if (idx < 0 || idx >= pipeData.Count) return null;
        return pipeData[idx];
    }

    public float GetRemaniningTime()
    {
        return countdown;
    }
}
