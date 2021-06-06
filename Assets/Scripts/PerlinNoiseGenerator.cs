using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerlinNoiseGenerator : MonoBehaviour
{
    // Most of these varaibles are public to make it easier for me to debug and change their values in the editor
    public int perlinTextureSizeX;
    public int perlinTextureSizeY;

    public float worldHeightScale;

    public int heightLimit;
    public int minSurfaceLevel;

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

    public Dictionary<Vector2Int, GameObject> chunks; // Keeps track of each chunk empty object
                                                       //  private Dictionary<Vector3Int, int> blockTypes; // Keeps track of each block type

    private List<Vector2Int> toGeneratePosition; // the chunks which need to be generated (doing it this way saves on performance)
    private List<int[,,]> toGenerateChunksScript;
    private List<GameObject> toUnloadGameObjects;


    public PlayerMovement playerMovementScript;

    private Mesh mesh;
    private List<Vector3> tempVerticesList;
    private List<int> tempTrianglesList;
    private int loopCount = 0;

    private bool worldLoaded = false; // Has the world loaded for the first time? Once true, the player will spawn

    public Transform player;
    public Vector2Int playerChunkPosition; // Public so I can test and debug in the Unity Editor
    private Vector2Int previousPlayerChunkPosition;

    private float timeSinceLastWorldUpdate = 0f;

    private List<string> currentlyLoadedChunks; // The name (which are ints) of each chunk which we want currently loaded into our game

    private int[] blockIDIndexOffsetLocations =
    {
        1, 1, 1, 1, 0, 2
    };

    // The UV position of each block in the texture atlas. Public because HotbarManager needs it to spawn the textures for the hotbar's blocks
    public Vector2[] blockIDs = {
        // 0 - dirt:
        new Vector2(2, 15), // top
        new Vector2(2, 15), // sides
        new Vector2(2, 15), // bottom
        // 1 - grass:
        new Vector2(0, 15), // grass top
        new Vector2(3, 15), // grass side
        new Vector2(2, 15), // dirt
        // 2 - stone:
        new Vector2(1, 15),
        new Vector2(1, 15),
        new Vector2(1, 15),
        // 3 - bedrock:
        new Vector2(1, 14),
        new Vector2(1, 14),
        new Vector2(1, 14),
        // 4 - netherrack:
        new Vector2(7, 9),
        new Vector2(7, 9),
        new Vector2(7, 9),
        // 5 - redstone:
        new Vector2(10, 14),
        new Vector2(10, 14),
        new Vector2(10, 14),
        // 6 - wooden planks:
        new Vector2(4, 15),
        new Vector2(4, 15),
        new Vector2(4, 15)
    };


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
        toUnloadGameObjects = new List<GameObject>();

        chunkNameCounter = 0;

        //GenerateWorld(true);
        //GenerateWorld(false);

    }

    private void Start()
    {
        previousPlayerChunkPosition = new Vector2Int(999999, 999999);
    }


    // Update is called once per frame
    void Update()
    {
        playerChunkPosition = new Vector2Int(Mathf.CeilToInt(player.position.x / chunkSize), Mathf.CeilToInt(player.position.z / chunkSize)); // Work out which chunk the player is currently on

        // put chunk loading and unloading code in here

        if (playerChunkPosition != previousPlayerChunkPosition) // If the player has moved into another chunk...
        {
            //print("You have moved into chunk " + playerChunkPosition);
            GenerateWorld(false);
            StopCoroutine(DelayBuildChunks());
            StartCoroutine(DelayBuildChunks());

        }

        previousPlayerChunkPosition = playerChunkPosition;

    }
    // Reload the world by only loading in chunks which have not yet been loaded in (for when the player moves chunk)
    private void AskToGenerateMesh(int x, int y, bool instant, int[,,] blockTypes)
    {
        if (instant)
        {
            GenerateMeshForChunk(x, y, blockTypes, false);
        }
        else
        {
            if (!toGeneratePosition.Contains(new Vector2Int(x, y)))
            {
                toGeneratePosition.Add(new Vector2Int(x, y));
                toGenerateChunksScript.Add(blockTypes);
            }
        }
    }


    // Call this function when generating the world for the first time
    private void GenerateWorld(bool instant)
    {
        timeSinceLastWorldUpdate = Time.realtimeSinceStartup;

        currentlyLoadedChunks = new List<string>();

        CreateChunk(playerChunkPosition.x, playerChunkPosition.y, true, instant); // The center chunk, (which the player spawns on)

        // let r=current renderDistance 'outline' to spawn
        for (int r = 1; r < renderDistance; r++) // For every 'outline'...
        {
            for (int i = -(r - 1); i < r + 1; i++) // Basiclly spawns every chunk in the 'outline'
            {
                bool generateMesh = !(r == renderDistance - 1); // true if it's not a chunk in the outer 'ring'

                // For some reason math likes to exclude (negative, negative), so I must manually spawn that chunk ;(
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

        UnloadChunks();
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
        if (!worldLoaded)
        {
            worldLoaded = true;
            playerMovementScript.SpawnPlayer();
        }
        //Debug.Break();
    }

    public void ReloadChunk(GameObject chunkObject, int chunkX, int chunkZ)
    {
        GenerateMeshForChunk(chunkX, chunkZ, chunkObject.GetComponent<Chunks>().GetBlockTypes(), true);
    }


    private void UnloadChunks()
    {

        for (int i = 0; i < worldParent.childCount; i++) // Go through every chunk currently loaded
        {
            if (worldParent.GetChild(i).name != "0") // Do not unload the center chunk
            {
                if (!(currentlyLoadedChunks.Contains(worldParent.GetChild(i).name))) // Check the currentlyLoadedChunks list to check if this chunk should be unloaded (if it's not in the list it should be unloaded)
                {
                    if (worldParent.GetChild(i).childCount > 0) // No need to unload chunks which don't even have a mesh object
                    {
                        // print("Destorying chunk " + worldParent.GetChild(i).name);
                        // chunks.Remove(CalculateChunkPosition(worldParent.GetChild(i)));
                        // Destroy(worldParent.GetChild(i).gameObject);
                        //worldParent.GetChild(i).gameObject.SetActive(false);
                        //worldParent.GetChild(i).GetComponent<Chunks>().meshVisible = false;
                        if (!(toUnloadGameObjects.Contains(worldParent.GetChild(i).gameObject)))
                        {
                            toUnloadGameObjects.Add(worldParent.GetChild(i).gameObject);
                        }
                        
                    }
                }

            }

        }

        StopCoroutine(DelayUnloadChunks());
        StartCoroutine(DelayUnloadChunks());

    }

    // This function generates the meshes for each block in the chunk specified
    private void GenerateMeshForChunk(int chunkX, int chunkZ, int[,,] blockTypes, bool reloading)
    {

        //print("Generating a mesh for chunk " + new Vector2Int(chunkX, chunkZ));
        Vector3Int chunkOffset = new Vector3Int(chunkX * (chunkSize - 0), 0, chunkZ * (chunkSize - 0));

        // Check if this chunk is already loaded. If so, return and therefore don't load it.
        Vector2Int chunkPositionVector = new Vector2Int(chunkX, chunkZ);
        //print("Chunk " + chunks[chunkPositionVector].name + ": active = " + chunks[chunkPositionVector].activeSelf + ", meshVisible = " + chunks[chunkPositionVector].GetComponent<Chunks>().meshVisible);
        if (!reloading)
        {
            if (chunks.ContainsKey(chunkPositionVector))
            {
                if (chunks[chunkPositionVector].transform.childCount > 0)
                {
                    //return;
                    //print("Outside & " + chunks[chunkPositionVector]);
                    // print(chunks[chunkPositionVector].GetComponent<Chunks>().meshVisible);
                    if (!chunks[chunkPositionVector].GetComponent<Chunks>().meshVisible)
                    {
                        // print("Setting chunk " + chunks[chunkPositionVector].name + " active! " + chunks[chunkPositionVector].GetComponent<Chunks>().meshVisible);
                        chunks[chunkPositionVector].SetActive(true);
                        //  print("Is it active?: " + chunks[chunkPositionVector].activeSelf + ", " + chunks[chunkPositionVector].activeInHierarchy);
                        chunks[chunkPositionVector].GetComponent<Chunks>().meshVisible = true;
                    }
                    return;

                }

            }
        }
        else // If we are reloading the chunk
        {

        }

        // There is no chunk located in x,z, therefore it will be generated!

        // Get the Chunks script from all four chunks adjacent to this chunk. This is so later I can call GetSingleBlockType() on these scripts.
        Chunks[] adjacentChunkScripts = new Chunks[4];
        for (int i=0; i<4; i++)
        {
            //print(new Vector2Int(chunkX + adjacentBlocksOffsets[i].x, chunkZ + adjacentBlocksOffsets[i].z));
            adjacentChunkScripts[i] = chunks[new Vector2Int(chunkX + adjacentBlocksOffsets[i].x, chunkZ + adjacentBlocksOffsets[i].z)].GetComponent<Chunks>();
        }

        bool[] chunksToReload = new bool[4];

        mesh = new Mesh();
        tempVerticesList = new List<Vector3>();
        tempTrianglesList = new List<int>();

        List<Vector2> uvsList = new List<Vector2>();
        float tileOffset = 1f / 16f; // 0.0625

        for (int x = 0; x < chunkSize; x++) // For every block x
        {
            for (int z = 0; z < chunkSize; z++) // For every block z
            {
                int xCord = x + (chunkX * chunkSize);
                int zCord = z + (chunkZ * chunkSize);
                int yCord = Mathf.RoundToInt(SampleStepped(xCord, zCord) * worldHeightScale);
                for (int y = 0; y < heightLimit + 0; y++) // change this to y<heightLimit if I'm testing out/using floating blocks (blocks above the surface)
                {
                    if (blockTypes[x, y, z] != -1) // Only attempt to draw meshes on this block if it's actually a block! (not air)
                    {

                        
                        List<int> verticesOrder = GenerateMesh(new Vector3Int(x, y, z), new Vector3Int(chunkX, 0, chunkZ), chunks[new Vector2Int(chunkX, chunkZ)].transform, blockTypes, adjacentChunkScripts); // Generate the mesh for that block


                        // Add to the uvs array:
                        for (int i = 0; i < verticesOrder.Count; i++) // For every block face in the chunk (mesh)
                        {
                            Vector2 uvBlockVector2 = blockIDs[(blockTypes[x, y, z] * 3) + blockIDIndexOffsetLocations[verticesOrder[i]]];
                            float ublock = uvBlockVector2.x;
                            float vblock = uvBlockVector2.y;
                            float umin = tileOffset * ublock;
                            float umax = tileOffset * (ublock + 1f);
                            float vmin = tileOffset * vblock;
                            float vmax = tileOffset * (vblock + 1f);

                            if (verticesOrder[i] == 1)
                            {
                                // POSITIVE Z / DEFAULT:
                                uvsList.Add(new Vector2(umin, vmax)); //top-left
                                uvsList.Add(new Vector2(umax, vmax)); //top-right
                                uvsList.Add(new Vector2(umax, vmin)); //bottom-right
                                uvsList.Add(new Vector2(umin, vmin)); //bottom-left
                            }
                            else if (verticesOrder[i] == 3)
                            {
                                // POSITIVE X:
                                uvsList.Add(new Vector2(umax, vmax)); //top-right
                                uvsList.Add(new Vector2(umax, vmin)); //bottom-right
                                uvsList.Add(new Vector2(umin, vmin)); //bottom-left
                                uvsList.Add(new Vector2(umin, vmax)); //top-left
                            }

                            else if (verticesOrder[i] == 0)
                            {
                                // NEGATIVE Z:
                                uvsList.Add(new Vector2(umin, vmax)); //top-left
                                uvsList.Add(new Vector2(umin, vmin)); //bottom-left
                                uvsList.Add(new Vector2(umax, vmin)); //bottom-right
                                uvsList.Add(new Vector2(umax, vmax)); //top-right
                            }
                            else if (verticesOrder[i] == 2)
                            {
                                // NEGATIVE X:
                                uvsList.Add(new Vector2(umax, vmax)); //top-right
                                uvsList.Add(new Vector2(umin, vmax)); //top-left
                                uvsList.Add(new Vector2(umin, vmin)); //bottom-left
                                uvsList.Add(new Vector2(umax, vmin)); //bottom-right
                            }
                            else
                            {
                                // POSITIVE Z / DEFAULT:
                                uvsList.Add(new Vector2(umin, vmax)); //top-left
                                uvsList.Add(new Vector2(umax, vmax)); //top-right
                                uvsList.Add(new Vector2(umax, vmin)); //bottom-right
                                uvsList.Add(new Vector2(umin, vmin)); //bottom-left
                            }
                            
                        }

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
        uvs = uvsList.ToArray();
        mesh.uv = uvs;


        GameObject meshObject;
        GameObject chunkObject;
        chunkObject = chunks[new Vector2Int(chunkX, chunkZ)];
        if (reloading)
        {
            meshObject = chunkObject.transform.GetChild(0).gameObject;
        }
        else
        {
            meshObject = Instantiate(emtpyMeshPrefab, emtpyMeshPrefab.transform.position + chunkOffset, emtpyMeshPrefab.transform.rotation); // Create a chunk object for the mesh to be applied too
            meshObject.transform.parent = chunkObject.transform;
            meshObject.transform.parent.GetComponent<Chunks>().chunkPosition = new Vector2Int(chunkX, chunkZ);
        }

        meshObject.GetComponent<MeshFilter>().mesh = mesh;
        meshObject.GetComponent<MeshCollider>().sharedMesh = mesh;

        meshObject.transform.parent.gameObject.GetComponent<Chunks>().meshGenerated = true;
        meshObject.transform.parent.gameObject.GetComponent<Chunks>().meshVisible = true;

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
            GameObject chunkObject = chunks[chunkPositionVector];
            if (chunkObject.transform.childCount > 0) // Has a mesh object been created?
            {
                if (chunkObject.activeSelf) // is the mesh object actually visible?
                {
                    if (chunkObject.GetComponent<Chunks>().meshGenerated)
                    {
                        currentlyLoadedChunks.Add(chunkObject.name);
                        return;
                    }

                }
                else
                {
                    currentlyLoadedChunks.Add(chunkObject.name);
                    AskToGenerateMesh(chunkX, chunkZ, instant, chunkObject.GetComponent<Chunks>().GetBlockTypes());
                    return;
                }

            }

        }

        int[,,] blockTypes = new int[chunkSize, heightLimit, chunkSize];

        Transform chunkEmptyObject = Instantiate(chunkParent, chunkParent.position, chunkParent.rotation); // Instantiate the empty chunk object in which this chunk's cubes will be placed in
        chunkEmptyObject.name = chunkNameCounter.ToString(); // Set it's name to make it neat
        currentlyLoadedChunks.Add(chunkNameCounter.ToString());
        chunks[new Vector2Int(chunkX, chunkZ)] = chunkEmptyObject.gameObject; // Add the chunk object to the chunks array for reference later on when adding blocks

        chunkEmptyObject.parent = worldParent;


        // Go through every possible block in this chunk to both spawn them and give them their respective values
        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                int xCord = x + (chunkX * chunkSize);
                int zCord = z + (chunkZ * chunkSize);
                int yCord = Mathf.RoundToInt((SampleStepped(xCord, zCord) * worldHeightScale) + minSurfaceLevel); // yCord = Surface

                // World generation layers:

                blockTypes[x, yCord, z] = 1; // Set every block on the surface to grass (1 = grass)

                for (int y = 0; y < yCord; y++) // Set every block below the surface to stone
                {
                    blockTypes[x, y, z] = 2; // 2 = stone
                }

                int dirtDepth = 5;
                // Set every block 5-6 blocks below the surface to dirt
                for (int y=yCord-dirtDepth; y<yCord; y++)
                {
                    if (ValidYCord(y))
                    {
                        blockTypes[x, y, z] = 0; // 0 = dirt
                    }
                }
                if (Random.value > 0.5f && ValidYCord(yCord - 6))
                {
                    blockTypes[x, yCord - (dirtDepth+1), z] = 0;
                }


                for (int y = yCord + 1; y < heightLimit; y++) // Set every block above the surface to air
                {
                    blockTypes[x, y, z] = -1; // -1 = air
                }


                // Have a 50% chance each of spawning bedrock at y=1 & y=2
                for (int i=1; i<3; i++)
                {
                    if (Random.value > 0.5f)
                    {
                        blockTypes[x, i, z] = 3; // 3 = bedrock
                    }
                    else if (blockTypes[x, i, z] == -1 && Random.value > 0.8f) // If no bedrock is spawned at y=3 && air it is being spawned at the surface (because the ground is super low), then have a 20% chance of spawning netherrack at at either y=1 &&|| y=2
                    {
                        blockTypes[x, i, z] = 4; // 4 = netherrack
                    }
                }
                // Always spawn a layer of bedrock at y=0
                blockTypes[x, 0, z] = 3; // 3 = bedrock

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

    private bool ValidYCord(int y)
    {
        return y > 0 && y < heightLimit; // Boundary test, making sure we don't spawn a block below the world nor above the world's height limit
    }

    // Generate a mesh for the given block
    private List<int> GenerateMesh(Vector3Int blockPosition, Vector3Int chunkPosition, Transform chunkEmptyObject, int[,,] blockTypes, Chunks[] adjacentChunkScripts)
    {
        List<int> verticesOrderList = new List<int>();
        for (int i = 0; i < 6; i++) // For every adjacent block around this block, (six blocks)
        {

            Vector3Int blockToSearch = blockPosition + adjacentBlocksOffsets[i]; // The adjacent block to search
            int finalBlockTypeValue;

            if (blockToSearch.y < heightLimit && blockToSearch.y > 0) // Only spawn a quad if it will be drawn within the height limits
            {
                if (blockToSearch.z > chunkSize - 1)
                {
                    finalBlockTypeValue = adjacentChunkScripts[0].GetSingleBlockType(new Vector3Int(blockToSearch.x, blockToSearch.y, 0));
                }
                else if (blockToSearch.z < 0)
                {
                    finalBlockTypeValue = adjacentChunkScripts[1].GetSingleBlockType(new Vector3Int(blockToSearch.x, blockToSearch.y, chunkSize - 1));
                }
                else if (blockToSearch.x > chunkSize - 1)
                {
                    finalBlockTypeValue = adjacentChunkScripts[2].GetSingleBlockType(new Vector3Int(0, blockToSearch.y, blockToSearch.z));
                }
                else if (blockToSearch.x < 0)
                {
                    finalBlockTypeValue = adjacentChunkScripts[3].GetSingleBlockType(new Vector3Int(chunkSize - 1, blockToSearch.y, blockToSearch.z));
                }
                else
                {
                    //print("blockToSearch = " + blockToSearch);
                    finalBlockTypeValue = blockTypes[blockToSearch.x, blockToSearch.y, blockToSearch.z];
                }
            }
            else if (blockToSearch.y == heightLimit)
            {
                finalBlockTypeValue = -1;
            }
            else
            {
                finalBlockTypeValue = 999999; // 999999 = don't generate a quad since it will most likely be facing outside of the world
            }

            /*
            if (blockToSearch.x < chunkSize - 1 && blockToSearch.y < heightLimit - 1 && blockToSearch.z < chunkSize - 1 && blockToSearch.x >= 0 && blockToSearch.y >= 0 && blockToSearch.z >= 0) // If the block exists in this chunk (has been generated)
            {
                //print(blockToSearch);
                finalBlockTypeValue = blockTypes[blockToSearch.x, blockToSearch.y, blockToSearch.z];
            }
            else
            {
                finalBlockTypeValue = -1; // -1 = don't generate a quad since it will most likely be facing outside of the world
            }
            */

            if (finalBlockTypeValue == -1) // Is there air next to me on i side? If so, I need to generate a quad for that side
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
                verticesOrderList.Add(i);

            }


        }
        return verticesOrderList;

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
            GenerateMeshForChunk(toGeneratePosition[0].x, toGeneratePosition[0].y, toGenerateChunksScript[0], false);

            toGeneratePosition.RemoveAt(0);
            toGenerateChunksScript.RemoveAt(0);

            yield return new WaitForSeconds(chunkSpawnRate);
        }

    }

    IEnumerator DelayUnloadChunks()
    {
        while (toUnloadGameObjects.Count > 0)
        {
            toUnloadGameObjects[0].gameObject.SetActive(false);
            toUnloadGameObjects[0].GetComponent<Chunks>().meshVisible = false;

            toUnloadGameObjects.RemoveAt(0);

            yield return new WaitForSeconds(chunkSpawnRate);
        }
    }



}
