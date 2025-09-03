using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GridCell
{
    public int x { get; }
    public int y { get; }
    public Vector3 worldPos { get; }
    public GameObject OccupyingObject { get; private set; }

    public GridCell (int x, int y, Vector3 worldPos)
    {
        this.x = x;
        this.y = y;
        this.worldPos = worldPos;
        OccupyingObject = null;
    }

    public void SetOccupyingObject(GameObject obj)
    {
        OccupyingObject = obj;
    }

    public void ClearOccupyingObject()
    {
        OccupyingObject = null;
    }

    public bool IsOccupied()
    {
        return OccupyingObject != null;
    }
}
