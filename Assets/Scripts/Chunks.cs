using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunks : MonoBehaviour
{
    private int[,,] blockTypes;

    public bool meshGenerated;
    public bool meshVisible;

    // Start is called before the first frame update
    void Awake()
    {
        meshGenerated = false;
        meshVisible = false;
    }

    private void Start()
    {
       
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

    public void EditBlockTypes(Vector3Int blockToEdit, int idToSet)
    {
        blockTypes[blockToEdit.x, blockToEdit.y, blockToEdit.z] = idToSet;
    }


}

