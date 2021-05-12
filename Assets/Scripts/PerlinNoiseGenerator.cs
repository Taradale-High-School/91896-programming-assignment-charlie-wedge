using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class PerlinNoiseGenerator : MonoBehaviour
{
    // Most of these varaibles are public to make it easier for me to debug and change their values in the editor
    public int perlinTextureSizeX;
    public int perlinTextureSizeY;

    public float worldHeightScale;

    public int heightLimit;

    public int chunkSize;
    public int renderDistance;
    public int emptyChunkDistance; // The amount of values in the renderDistance variable which will not have their mesh generated, just their data

    public float chunkSpawnRate; // The greater the number, the slower chunks spawn. (chunks spawned x number of seconds). This results in better performance, but slower world generation when exploring.

    public int noiseScale;

    public Vector2Int perlinOffset; // The offset in which we search the perlin noise at (seed)
    public bool randomSeed;

    private int chunkNameCounter = 0;

    public Transform worldParent;
    public Transform chunkParent;
    public GameObject emtpyMeshPrefab;

    private Dictionary<Vector2Int, GameObject> chunks; // Keeps track of each chunk empty object
  //  private Dictionary<Vector3Int, int> blockTypes; // Keeps track of each block type

    private List<Vector2Int> toGeneratePosition; // the chunks which need to be generated (doing it this way saves on performance)
    private List<int [,,]> toGenerateChunksScript;

    private Mesh mesh;
    private List<Vector3> tempVerticesList;
    private List<int> tempTrianglesList;
    private int loopCount = 0;

    public Transform player;
    public Vector2Int playerChunkPosition; // Public so I can test and debug in the Unity Editor
    private Vector2Int previousPlayerChunkPosition;

    private float timeSinceLastWorldUpdate = 0f;

    // The offsets which make up all the blocks around a block, (for generating quads)
    private Vector3Int[] adjacentBlocksOffsets =
    {
        new Vector3Int(0, 0, 1),
        new Vector3Int(0, 0, -1),
        new Vector3Int(1, 0, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, -1, 0)
    };

    // The two triangles which all the vertices use:
    private int[] baseTriangles =
    {
        0, 1, 2,
        2, 3, 0
    };
    // Vertices which make up each side of a block:
    private Vector3[] verticesUp =
    {
        new Vector3(0, 1, 0),
        new Vector3(0, 1, 1),
        new Vector3(1, 1, 1),
        new Vector3(1, 1, 0)
    };

    private Vector3[] verticesDown =
    {
        new Vector3(0, 0, 0),
        new Vector3(1, 0, 0),
        new Vector3(1, 0, 1),
        new Vector3(0, 0, 1)
    };

    private Vector3[] verticesFront =
    {
        new Vector3(0, 1, 0),
        new Vector3(1, 1, 0),
        new Vector3(1, 0, 0),
        new Vector3(0, 0, 0)
    };

    private Vector3[] verticesBack =
    {
        new Vector3(0, 1, 1),
        new Vector3(0, 0, 1),
        new Vector3(1, 0, 1),
        new Vector3(1, 1, 1)
    };

    private Vector3[] verticesLeft =
    {
        new Vector3(0, 1, 0),
        new Vector3(0, 0, 0),
        new Vector3(0, 0, 1),
        new Vector3(0, 1, 1)
    };

    private Vector3[] verticesRight =
    {
        new Vector3(1, 1, 0),
        new Vector3(1, 1, 1),
        new Vector3(1, 0, 1),
        new Vector3(1, 0, 0)
    };



    private void Awake()
    {


        if (renderDistance - emptyChunkDistance <= 0)
        {
            Debug.LogWarning("emptyChunkDistance cancels out renderDistance. This may result in weird behaviour with chunk meshes being generated.");
        }

        if (randomSeed)
        {
            perlinOffset = new Vector2Int(Random.Range(-25000, 25000), Random.Range(-25000, 25000));
            noiseScale = Random.Range(3, 6);
        }

        chunks = new Dictionary<Vector2Int, GameObject>();
        //blockTypes = new Dictionary<Vector3Int, int>();
        toGeneratePosition = new List<Vector2Int>();
        toGenerateChunksScript = new List<int[,,]>();

        chunkNameCounter = 0;

        GenerateWorld(true);

    }

    private void Start()
    {
        previousPlayerChunkPosition = new Vector2Int(0, 0);
    }


    // Update is called once per frame
    void Update()
    {
        playerChunkPosition = new Vector2Int(Mathf.FloorToInt(player.position.x / chunkSize), Mathf.FloorToInt(player.position.z / chunkSize)); // Work out which chunk the player is currently on

        // put chunk loading and unloading code in here

        if (playerChunkPosition != previousPlayerChunkPosition) // If the player has moved into another chunk...
        {
            print("You have moved into chunk " + playerChunkPosition);
            GenerateWorld(false);
            StopAllCoroutines();
            StartCoroutine(DelayBuildChunks());

        }

        previousPlayerChunkPosition = playerChunkPosition;

    }
    // Reload the world by only loading in chunks which have not yet been loaded in (for when the player moves chunk)
    private void AskToGenerateMesh(int x, int y, bool instant, int[,,] blockTypes)
    {
        if (instant)
        {
            GenerateMeshForChunk(x, y, blockTypes);
        }
        else
        {
            toGeneratePosition.Add(new Vector2Int(x, y));
            toGenerateChunksScript.Add(blockTypes);
        }
    }


    // Call this function when generating the world for the first time
    private void GenerateWorld(bool instant)
    {
        timeSinceLastWorldUpdate = Time.realtimeSinceStartup;

        CreateChunk(playerChunkPosition.x, playerChunkPosition.y, true, instant); // The center chunk, (which the player spawns on)

        // let r=current renderDistance 'outline' to spawn
        for (int r = 1; r < renderDistance; r++) // For every 'outline'...
        {
            for (int i = -(r - 1); i < r + 1; i++) // Basiclly spawns every chunk in the 'outline'
            {
                bool generateMesh = !(r == renderDistance-1);
               
                // For some reason math likes to exclude (negitive, negitive), so I must manually spawn that chunk ;(
                if (r == i && r > 0)
                {
                    CreateChunk(-i + playerChunkPosition.x, -r + playerChunkPosition.y, generateMesh, instant);
                }
                else
                {
                    CreateChunk(i + playerChunkPosition.x, r + playerChunkPosition.y, generateMesh, instant);
                }

                CreateChunk(r + playerChunkPosition.x, i + playerChunkPosition.y, generateMesh, instant);
                CreateChunk(-r + playerChunkPosition.x, i + playerChunkPosition.y, generateMesh, instant);
                CreateChunk(i + playerChunkPosition.x, -r + playerChunkPosition.y, generateMesh, instant);
            }
        }
        /*
        // Later on in this game's development, this code will be upgraded to only spawn meshes in chunks which are visible to the player:
        AskToGenerateMesh(0, 0, instant); // The center chunk, (which the player spawns on)

        // let r=current renderDistance 'outline' to spawn
        for (int r = 1; r < renderDistance - emptyChunkDistance; r++) // For every 'outline'...
        {
            for (int i = -(r - 1); i < r + 1; i++) // Basiclly spawns every chunk in the 'outline'
            {
                // For some reason math likes to exclude (negitive, negitive), so I must manually spawn that chunk ;(
                if (r == i && r > 0)
                {
                    AskToGenerateMesh(-i + playerChunkPosition.x, -r + playerChunkPosition.y, instant);
                }
                else
                {
                    AskToGenerateMesh(i + playerChunkPosition.x, r + playerChunkPosition.y, instant);
                }

                AskToGenerateMesh(r + playerChunkPosition.x, i + playerChunkPosition.y, instant);
                AskToGenerateMesh(-r + playerChunkPosition.x, i + playerChunkPosition.y, instant);
                AskToGenerateMesh(i + playerChunkPosition.x, -r + playerChunkPosition.y, instant);
            }
        }
        */

        //timeSinceLastWorldUpdate = Time.realtimeSinceStartup - timeSinceLastWorldUpdate;
        if (instant)
        {
            timeSinceLastWorldUpdate = Time.realtimeSinceStartup;
            print("World Generation Complete. Generated " + (renderDistance + (renderDistance - 1)) * (renderDistance + (renderDistance - 1)) + " chunk" + ((renderDistance + (renderDistance - 1)) * (renderDistance + (renderDistance - 1)) == 1 ? "" : "s") + " in " + timeSinceLastWorldUpdate + " seconds!\n(" + timeSinceLastWorldUpdate / ((renderDistance + (renderDistance - 1)) * (renderDistance + (renderDistance - 1))) + " seconds per chunk!)");
        }

        //Debug.Break();
    }

    // This function generates the meshes for each block in the chunk specified
    private void GenerateMeshForChunk(int chunkX, int chunkZ, int[,,] blockTypes)
    {
        Vector3Int chunkOffset = new Vector3Int(chunkX * (chunkSize-1), 0, chunkZ * (chunkSize-1));

        // Check if this chunk is already loaded. If so, return and therefore don't load it.
        Vector2Int chunkPositionVector = new Vector2Int(chunkX, chunkZ);
        if (chunks.ContainsKey(chunkPositionVector))
        {
            if (chunks[chunkPositionVector].transform.childCount > 0)
            {
                if (chunks[chunkPositionVector].transform.GetChild(0).gameObject.activeSelf)
                {
                    return;
                }
            }

        }
        // There is no chunk located in x,z, therefore it will be generated!


        mesh = new Mesh();
        tempVerticesList = new List<Vector3>();
        tempTrianglesList = new List<int>();

        for (int x = 0; x < chunkSize; x++) // For every block x
        {
            for (int z = 0; z < chunkSize; z++) // For every block z
            {
                int xCord = x + (chunkX * chunkSize);
                int zCord = z + (chunkZ * chunkSize);
                int yCord = Mathf.RoundToInt(SampleStepped(xCord, zCord) * worldHeightScale);
                for (int y = 0; y < heightLimit; y++) // change this back to y<yCord+1 once I'm done testing out floating blocks! ---------------------------------------------------------------------------------------------------------------------
                {
                    if (blockTypes[x, y, z] != 0) // Only attempt to draw meshes on this block if it's actually a block! (not air)
                    {
                        GenerateMesh(new Vector3Int(x, y, z), new Vector3Int(chunkX, 0, chunkZ), chunks[new Vector2Int(chunkX, chunkZ)].transform, blockTypes); // Generate the mesh for that block
                    }
                }
            }
        }


        mesh.vertices = tempVerticesList.ToArray();

        for (int i = 1; i < loopCount + 1; i++)
        {
            for (int k = 0; k < baseTriangles.Length; k++)
            {
                tempTrianglesList.Add(baseTriangles[k] + (4 * (i - 1)));
            }
        }

        mesh.triangles = tempTrianglesList.ToArray();


        Vector2[] uvs = new Vector2[mesh.vertices.Length];
        for (int i = 0; i < uvs.Length; i = i + 4)
        {
            uvs[0 + i] = new Vector2(0 + i, 1 + i); //top-left
            uvs[1 + i] = new Vector2(1 + i, 1 + i); //top-right
            uvs[2 + i] = new Vector2(1 + i, 0 + i); //bottom-left
            uvs[3 + i] = new Vector2(0 + i, 0 + i); //bottom-right
        }

        mesh.uv = uvs;


        GameObject meshObject = Instantiate(emtpyMeshPrefab, emtpyMeshPrefab.transform.position + chunkOffset, emtpyMeshPrefab.transform.rotation); // Create a block object
        meshObject.transform.parent = chunks[new Vector2Int(chunkX, chunkZ)].transform;

        meshObject.GetComponent<MeshFilter>().mesh = mesh;
        meshObject.GetComponent<MeshCollider>().sharedMesh = mesh;

        mesh.RecalculateNormals();

        loopCount = 0;
    }

    private void DeleteWorld()
    {
        for (int i = 0; i < worldParent.childCount; i++)
        {
            Destroy(worldParent.GetChild(i).gameObject); // Delete each chunk which is currently in the hierarchy
        }

    }

    // Do everything required to generate a chunk at x,z based on the player's current position
    private void CreateChunk(int chunkX, int chunkZ, bool generateMesh, bool instant)
    {


        // Check if this chunk is already loaded. If so, return and therefore don't load it.
        Vector2Int chunkPositionVector = new Vector2Int(chunkX, chunkZ);
        if (chunks.ContainsKey(chunkPositionVector))
        {
            if (chunks[chunkPositionVector].transform.childCount > 0)
            {
                if (chunks[chunkPositionVector].transform.GetChild(0).gameObject.activeSelf)
                {
                    return;
                }
            }

        }

        int[,,] blockTypes = new int[chunkSize, heightLimit, chunkSize];

        Transform chunkEmptyObject = Instantiate(chunkParent, chunkParent.position, chunkParent.rotation); // Instantiate the empty chunk object in which this chunk's cubes will be placed in
        chunkEmptyObject.name = chunkNameCounter.ToString(); // Set it's name to make it neat
        chunks[new Vector2Int(chunkX, chunkZ)] = chunkEmptyObject.gameObject; // Add the chunk object to the chunks array for reference later on when adding blocks

        chunkEmptyObject.parent = worldParent;


        // Go through every possible block in this chunk to both spawn them and give them their respective values
        for (int x = 0; x < chunkSize-1; x++)
        {
            for (int z = 0; z < chunkSize-1; z++)
            {
                int xCord = x + (chunkX * chunkSize);
                int zCord = z + (chunkZ * chunkSize);
                int yCord = Mathf.RoundToInt(SampleStepped(xCord, zCord) * worldHeightScale); // yCord = Surface
                //print(new Vector3Int(x, yCord, z));
                blockTypes[x, yCord, z] = 1; // Set every block on the surface to grass (1 = grass)

                for (int y = 0; y < yCord; y++) // Set every block below the surface to stone
                {
                    blockTypes[x, y, z] = 2; // 2 = stone
                }

                for (int y = yCord + 1; y < heightLimit; y++) // Set every block above the surface to air
                {
                    blockTypes[x, y, z] = 0; // 0 = air
                }



            }
        }


        Chunks chunkScriptTemp = chunkEmptyObject.GetComponent<Chunks>();
        chunkScriptTemp.StoreBlockTypes(blockTypes);

       // blockTypes[playerChunkPosition.x, GetSurfaceHeight(new Vector2Int(0, 0), blockTypes) + 2, playerChunkPosition.y] = 1; // The single block above the player's head when they spawn - delete this once the game is finished

        // Increment this ready for the next chunk to be generated
        chunkNameCounter++;

        if (generateMesh)
        {
            AskToGenerateMesh(chunkX, chunkZ, instant, blockTypes);
        }

    }

    // Generate a mesh for the given block
    private void GenerateMesh(Vector3Int blockPosition, Vector3Int chunkPosition, Transform chunkEmptyObject, int[,,] blockTypes)
    {

        for (int i = 0; i < 6; i++) // For every adjacent block around this block, (six blocks)
        {

            Vector3Int blockToSearch = blockPosition + adjacentBlocksOffsets[i]; // The adjacent block to search
            int finalBlockTypeValue;

            if (blockToSearch.x < chunkSize-1 && blockToSearch.y < heightLimit-1 && blockToSearch.z < chunkSize-1 && blockToSearch.x >= 0 && blockToSearch.y >=0 && blockToSearch.z >= 0) // If the block exists in this chunk (has been generated)
            {
                //print(blockToSearch);
                finalBlockTypeValue = blockTypes[blockToSearch.x, blockToSearch.y, blockToSearch.z];
            }
            else
            {
                finalBlockTypeValue = -1; // -1 = don't generate a quad since it will most likely be facing outside of the world
            }

            if (finalBlockTypeValue == 0) // Is there air next to me on i side? If so, I need to generate a quad for that side
            {

                /* COPYED FROM TOP OF SCRIPT:
                private Vector3Int[] adjacentBlocksOffsets =
                {
                    new Vector3Int(0, 0, 1),
                    new Vector3Int(0, 0, -1),
                    new Vector3Int(1, 0, 0),
                    new Vector3Int(-1, 0, 0),
                    new Vector3Int(0, 1, 0),
                    new Vector3Int(0, -1, 0)
                };      
                */
                Vector3[] veryTempVertices = new Vector3[verticesBack.Length];

                if (i == 0) // verticesBack
                {
                    for (int u = 0; u < verticesBack.Length; u++)
                    {
                        veryTempVertices[u] = verticesBack[u] + blockPosition;
                    }

                    tempVerticesList.AddRange(veryTempVertices);
                }
                else if (i == 1) // verticesFront
                {
                    for (int u = 0; u < verticesFront.Length; u++)
                    {
                        veryTempVertices[u] = verticesFront[u] + blockPosition;
                    }

                    tempVerticesList.AddRange(veryTempVertices);
                }
                else if (i == 2) // verticesRight
                {
                    for (int u = 0; u < verticesRight.Length; u++)
                    {
                        veryTempVertices[u] = verticesRight[u] + blockPosition;
                    }
                    

                    tempVerticesList.AddRange(veryTempVertices);
                }
                else if (i == 3) // verticesLeft
                {
                    for (int u = 0; u < verticesLeft.Length; u++)
                    {
                        veryTempVertices[u] = verticesLeft[u] + blockPosition;
                    }

                    tempVerticesList.AddRange(veryTempVertices);
                }
                else if (i == 4) // verticesUp
                {
                    for (int u = 0; u < verticesUp.Length; u++)
                    {
                        veryTempVertices[u] = verticesUp[u] + blockPosition;
                    }

                    tempVerticesList.AddRange(veryTempVertices);
                }
                else if (i == 5) // verticesDown
                {
                    for (int u = 0; u < verticesDown.Length; u++)
                    {
                        veryTempVertices[u] = verticesDown[u] + blockPosition;
                    }

                    tempVerticesList.AddRange(veryTempVertices);
                }


                loopCount++;

            }


        }

    }

    // Get the height of the surface for x,z based on perlin noise
    private float SampleStepped(int x, int z)
    {
        // Get valid coordinates for Mathf.PerlinNoise() to use
        float xCord = (float)x / perlinTextureSizeX * noiseScale + perlinOffset.x;
        float zCord = (float)z / perlinTextureSizeY * noiseScale + perlinOffset.y;

        float sample = Mathf.PerlinNoise(xCord, zCord);

        return Mathf.Clamp(sample, 0, 1); // Sometimes perlin noise can return a value outside of the [0, 1] range. Weird right? Mathf.Clamp() fixes that

    }

    // Find the y cord of the surface at x,z (for spawning entites on the surface)
    public int GetSurfaceHeight(Vector2Int positionToGet, int[,,] blockTypes)
    {
        // Search through each y block at x,z until we find the surface
        for (int i = 0; i < heightLimit; i++)
        {
            if (blockTypes[positionToGet.x, i, positionToGet.y] == 0) // If it is air
            {
                return i;
            }
        }
        return heightLimit;
    }

    IEnumerator DelayBuildChunks()
    {
        while (toGeneratePosition.Count > 0)
        {
            GenerateMeshForChunk(toGeneratePosition[0].x, toGeneratePosition[0].y, toGenerateChunksScript[0]);
            toGeneratePosition.RemoveAt(0);
            toGenerateChunksScript.RemoveAt(0);

            yield return new WaitForSeconds(chunkSpawnRate);

        }

    }



}
