using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunks : MonoBehaviour
{
    private int[,,] blockTypes;
    public bool meshGenerated;

    // Start is called before the first frame update
    void Awake()
    {
        meshGenerated = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StoreBlockTypes(int [,,] tempBlockTypes)
    {
        blockTypes = tempBlockTypes;

    }

    public int[,,] GetBlockTypes()
    {
        return blockTypes;
    }


}

