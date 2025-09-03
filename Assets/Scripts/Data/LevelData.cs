using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Level/Simple Level Data")]
public class LevelData : ScriptableObject
{
    public int width = 5;      // columns
    public int height = 3;     // rows
    public float cellSize = 1f;// size of one cell in world units
}
