using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public float mouseSensitivity = 100f;

    public Transform playerBody;

    float xRotation = 0f;

    public PerlinNoiseGenerator perlinNoiseGeneratorScript;
    public HotbarManager hotbarManagerScript;

    public int rayDistance; // Public so I can edit it in the editor

    private int heightLimit;


    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        heightLimit = perlinNoiseGeneratorScript.heightLimit;
    }

    // Update is called once per frame
    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Ensure the player doesn't look up or down beyond 90º

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);

        // Temp code:
        if (Input.GetKeyDown(KeyCode.Z))
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        //print(Input.GetAxis("Left Trigger"));
        // Mouse presses: (breaking and placing blocks)
        if (Input.GetMouseButtonDown(0) || Input.GetAxis("Left Trigger") > 0)
        {
            ShootRayCast(true); // break
        }
        if (Input.GetMouseButtonDown(1))
        {
            ShootRayCast(false); // place
        }
    }

    private void ShootRayCast(bool breakBlock)
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0f));

        int layerMask = 1 << 2;
        layerMask = ~layerMask;


        if (Physics.Raycast(ray, out hit, rayDistance, layerMask)) // Send a raycast, and output the mesh info it hits throught the hit variable. Only returns through if it hits a block
        {
            //print(hit.point);

            int chunkSize = perlinNoiseGeneratorScript.chunkSize;
            Vector3Int blockWorldPosition = new Vector3Int(Mathf.FloorToInt(hit.point.x), Mathf.FloorToInt(hit.point.y), Mathf.FloorToInt(hit.point.z));
            Transform chunkObject = hit.transform;
           // print(chunkObject.gameObject.name);
            Vector2Int chunkPosition = chunkObject.parent.GetComponent<Chunks>().chunkPosition;
            Vector3Int blockLocalPosition = new Vector3Int(Mathf.Clamp(Mathf.FloorToInt(chunkObject.InverseTransformPoint(blockWorldPosition).x), 0, chunkSize - 1), Mathf.Clamp(Mathf.FloorToInt(chunkObject.InverseTransformPoint(blockWorldPosition).y), 0, heightLimit - 1), Mathf.Clamp(Mathf.FloorToInt(chunkObject.InverseTransformPoint(blockWorldPosition).z), 0, chunkSize - 1));

            //print("Block position = " + blockLocalPosition);
            //print(hit.normal);
            // print("Chunk position = " + chunkPosition);
            int[,,] blockTypes = chunkObject.parent.GetComponent<Chunks>().GetBlockTypes();

            Vector3Int normal = Vector3Int.FloorToInt(hit.normal);

            if (breakBlock && blockTypes[blockLocalPosition.x, blockLocalPosition.y, blockLocalPosition.z] == -1)
            {
                blockLocalPosition = blockLocalPosition - normal;
            }
            else if (!breakBlock && blockTypes[blockLocalPosition.x, blockLocalPosition.y, blockLocalPosition.z] != -1)
            {
                blockLocalPosition = blockLocalPosition + normal;
                if (blockLocalPosition.x >= chunkSize || blockLocalPosition.x < 0 || blockLocalPosition.z >= chunkSize || blockLocalPosition.z < 0)
                {
                    Vector2Int orignalChunkPosition = chunkPosition;
                    chunkPosition = new Vector2Int(chunkPosition.x + normal.x, chunkPosition.y + normal.z);
                    chunkObject = perlinNoiseGeneratorScript.chunks[chunkPosition].transform.GetChild(0);
                    Vector3Int offset = new Vector3Int();
                    if (normal.x == -1)
                    {
                        perlinNoiseGeneratorScript.ReloadChunk(hit.transform.gameObject, orignalChunkPosition.x, orignalChunkPosition.y);
                    }
                    else if (normal.x == 1)
                    {
                        offset = new Vector3Int(-chunkSize, 0, 0);
                    }
                    else if (normal.z == -1)
                    {
                        offset = new Vector3Int(0, 0, chunkSize);
                    }
                    else if (normal.z == 1)
                    {
                        offset = new Vector3Int(0, 0, -chunkSize);
                    }
                    blockLocalPosition += offset;
                }
            }

            // Make sure we don't place a block higher than the height limit, nor place a block under the world
            if (blockLocalPosition.y >= heightLimit || blockLocalPosition.y < 0) // Don't try to edit blockData which doesn't exist
            {
                return;
            }

            Chunks chunkScript = chunkObject.parent.GetComponent<Chunks>();
            int newBlockType = -1; // The new value to set to the block the player clicked (-1 = break block, but could be changed later on)

            if (breakBlock) // If the player is trying to break a block...
            {
                int blockToBreakBlockType = chunkScript.GetSingleBlockType(blockLocalPosition); // The type of block they are trying to break
        
                // Don't let them break it if it's bedrock
                if (blockToBreakBlockType == 3) // 3 = bedrock
                {
                    return;
                }
                if (!hotbarManagerScript.GivePlayerBlock(blockToBreakBlockType)) // Give the player the block. Returns false if they can't fit it in their inventory, therefore don't break the block
                {
                    return;
                }
            }
            else // If we are placing a block...
            {
                newBlockType = hotbarManagerScript.TakePlayerBlock(); // Returns -1 if there is no block to place, otherwise returns the blockType to place
                if (newBlockType == -1)
                {
                    return;
                }     
            }
            chunkScript.EditBlockTypes(blockLocalPosition, newBlockType);


            perlinNoiseGeneratorScript.ReloadChunk(chunkObject.parent.gameObject, chunkPosition.x, chunkPosition.y); // Reload the chunk which the block update occured in

            // Need to update any adjacent chunks based on the blockLocalPosition:
            if (breakBlock) // If we are breaking a block...
            {
                if (blockLocalPosition.z == chunkSize - 1)
                {
                    CallReloadChunk(chunkPosition + new Vector2Int(0, 1));
                }
                else if (blockLocalPosition.z == 0)
                {
                    CallReloadChunk(chunkPosition + new Vector2Int(0, -1));
                }
                if (blockLocalPosition.x == chunkSize - 1)
                {
                    CallReloadChunk(chunkPosition + new Vector2Int(1, 0));
                }
                else if (blockLocalPosition.x == 0)
                {
                    CallReloadChunk(chunkPosition + new Vector2Int(-1, 0));
                }
            }

            
        }
    }

    private void CallReloadChunk(Vector2Int chunkPosition)
    {
        perlinNoiseGeneratorScript.ReloadChunk(perlinNoiseGeneratorScript.chunks[chunkPosition], chunkPosition.x, chunkPosition.y);
    }
}
