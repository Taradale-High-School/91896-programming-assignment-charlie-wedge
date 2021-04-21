using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{

    private int[,,] blockTypes; // This array holds every block type in this chunk, (which blocks are air, dirt etc)
    private GameObject[,,] blockObjects; // This array holds the object of each block in the chunk (even air)

    public void WriteChunkDataBlockTypes(int[,,] data)
    {
        blockTypes = data;
    }

    public void WriteChunkBlockObjects(GameObject[,,] data)
    {
        blockObjects = data;
    }

    public int GetChunkDataBlockType(Vector3Int index)
    {
        return blockTypes[index.x, index.y, index.z];
    }

}
