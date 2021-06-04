using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public float mouseSensitivity = 100f;

    public Transform playerBody;

    float xRotation = 0f;

    public PerlinNoiseGenerator perlinNoiseGeneratorScript;

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
            ShootRayCast(-1, true); // break
        }
        if (Input.GetMouseButtonDown(1))
        {
            ShootRayCast(6, false); // place
        }
    }

    private void ShootRayCast(int blockChangeValue, bool findNormal)
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

            if (findNormal && blockTypes[blockLocalPosition.x, blockLocalPosition.y, blockLocalPosition.z] == -1)
            {
                blockLocalPosition = blockLocalPosition - normal;
            }
            else if (!findNormal && blockTypes[blockLocalPosition.x, blockLocalPosition.y, blockLocalPosition.z] != -1)
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

            if (blockLocalPosition.y >= heightLimit || blockLocalPosition.y < 0) // Don't try to edit blockData which doesn't exist
            {
                return;
            }

            Chunks chunkScript = chunkObject.parent.GetComponent<Chunks>();
            // Check to make sure the player isn't trying to break bedrock:
            if (blockChangeValue == -1)
            {
                if (chunkScript.GetSingleBlockType(blockLocalPosition) == 3) // 3 = bedrock
                {
                    return;
                } 
            }
            chunkScript.EditBlockTypes(blockLocalPosition, blockChangeValue);

            perlinNoiseGeneratorScript.ReloadChunk(chunkObject.parent.gameObject, chunkPosition.x, chunkPosition.y); // Reload the chunk which the block update occured in

            // Need to update any adjacent chunks based on the blockLocalPosition:
            if (blockChangeValue == -1) // If we are breaking a block...
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
