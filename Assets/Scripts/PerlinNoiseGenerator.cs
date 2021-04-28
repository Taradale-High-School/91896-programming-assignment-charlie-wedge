﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerlinNoiseGenerator : MonoBehaviour
{
    // Most of these varaibles are public to make it easier for me to debug and change their values in the editor
    public int perlinTextureSizeX;
    public int perlinTextureSizeY;

    public float worldHeightScale;

    public int playerChunkPosX;
    public int playerChunkPosZ;

    public int heightLimit;

    public int chunkSize;
    public int renderDistance;

    public int noiseScale;

    public Vector2 perlinOffset; // The offset in which we search the perlin noise at (seed)

    private int chunkNameCounter = 0;

    public Transform worldParent;
    public Transform chunkParent;
    public GameObject emtpyMeshPrefab;

    private Dictionary<Vector2Int, GameObject> chunks; // Keeps track of each chunk empty object
    private Dictionary<Vector3Int, int> blockTypes; // Keeps track of each block type



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



    private void Start()
    {
        chunks = new Dictionary<Vector2Int, GameObject>();
        blockTypes = new Dictionary<Vector3Int, int>();

        chunkNameCounter = 0;

        GenerateWorld();

    }


    // Update is called once per frame
    void Update()
    {


    }

    // Call this function when generating the world for the first time
    private void GenerateWorld()
    {

        GenerateChunk(0, 0); // The center chunk, (which the player spawns on)

        // let r=current renderDistance 'outline' to spawn
        for (int r = 1; r < renderDistance; r++) // For every 'outline'...
        {
            for (int i = -(r - 1); i < r + 1; i++) // Basiclly spawns every chunk in the 'outline'
            {
                // For some reason math likes to exclude (negitive, negitive), so I must manually spawn that chunk ;(
                if (r == i && r > 0)
                {
                    GenerateChunk(-i, -r);
                }
                else
                {
                    GenerateChunk(i, r);
                }

                GenerateChunk(r, i);
                GenerateChunk(-r, i);
                GenerateChunk(i, -r);
            }
        }

        // Later on in this game's development, this code will be upgraded to only spawn meshes in chunks which are visible to the player:
        GenerateMeshForChunk(0, 0); // The center chunk, (which the player spawns on)

        // let r=current renderDistance 'outline' to spawn
        for (int r = 1; r < renderDistance; r++) // For every 'outline'...
        {
            for (int i = -(r - 1); i < r + 1; i++) // Basiclly spawns every chunk in the 'outline'
            {
                // For some reason math likes to exclude (negitive, negitive), so I must manually spawn that chunk ;(
                if (r == i && r > 0)
                {
                    GenerateMeshForChunk(-i, -r);
                }
                else
                {
                    GenerateMeshForChunk(i, r);
                }

                GenerateMeshForChunk(r, i);
                GenerateMeshForChunk(-r, i);
                GenerateMeshForChunk(i, -r);
            }
        }

        print("World Generation Complete. Generated " + (renderDistance + (renderDistance - 1)) * (renderDistance + (renderDistance - 1)) + " chunks in " + Time.realtimeSinceStartup + " seconds!");
       // Debug.Break();
    }

    // This function generates the meshes for each block in the chunk specified
    private void GenerateMeshForChunk(int chunkX, int chunkZ)
    {
        for (int x = 0; x < chunkSize; x++) // For every block x
        {
            for (int z = 0; z < chunkSize; z++) // For every block z
            {
                int xCord = x + (chunkX * chunkSize);
                int zCord = z + (chunkZ * chunkSize);
                int yCord = Mathf.RoundToInt(SampleStepped(xCord, zCord) * worldHeightScale);
                for (int y=0; y<yCord+1; y++)
                {
                    if (blockTypes[new Vector3Int(xCord, y, zCord)] != 0) // Only attempt to draw meshes on this block if it's actually a block! (not air)
                    {
                        GenerateMesh(new Vector3Int(xCord, y, zCord), new Vector3Int(chunkX, 0, chunkZ), chunks[new Vector2Int(chunkX, chunkZ)].transform); // Generate the mesh for that block
                    }
                }
            }
        }
    }

    private void DeleteWorld()
    {
        for (int i = 0; i < worldParent.childCount; i++)
        {
            Destroy(worldParent.GetChild(i).gameObject); // Delete each chunk which is currently in the hierarchy
        }

    }

    // Do everything required to generate a chunk at x,z
    private void GenerateChunk(int chunkX, int chunkZ)
    {
        Transform chunkEmptyObject = Instantiate(chunkParent, chunkParent.position, chunkParent.rotation); // Instantiate the empty chunk object in which this chunk's cubes will be placed in
        chunkEmptyObject.name = chunkNameCounter.ToString(); // Set it's name to make it neat
        chunks[new Vector2Int(chunkX, chunkZ)] = chunkEmptyObject.gameObject; // Add the chunk object to the chunks array for reference later on when adding blocks

        chunkEmptyObject.parent = worldParent;


        // Go through every possible block in this chunk to both spawn them and give them their respective values
        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                int xCord = x + (chunkX * chunkSize);
                int zCord = z + (chunkZ * chunkSize);
                int yCord = Mathf.RoundToInt(SampleStepped(xCord, zCord) * worldHeightScale); // yCord = Surface

                blockTypes[new Vector3Int(xCord, yCord, zCord)] = 1; // Set every block on the surface to grass (1 = grass)

                for (int y = 0; y < yCord; y++) // Set every block below the surface to stone
                {
                    blockTypes[new Vector3Int(xCord, y, zCord)] = 2; // 2 = stone
                }

                for (int y=yCord+1; y<heightLimit; y++) // Set every block above the surface to air
                {
                    blockTypes[new Vector3Int(xCord, y, zCord)] = 0; // 0 = air
                }



            }
        }

        //chunkEmptyObject.position = new Vector3(chunkX * chunkSize, chunkEmptyObject.position.y, chunkZ * chunkSize);

        // Increment this ready for the next chunk to be generated
        chunkNameCounter++;

    }

    // Generate a mesh for the given block
    private void GenerateMesh(Vector3Int blockPosition, Vector3Int chunkPosition, Transform chunkEmptyObject)
    {


        int loopCount = 0;
        List<Vector3> tempVerticesList = new List<Vector3>();
        bool quadAdded = false; // Has there been anything added to the mesh?

        for (int i = 0; i < 6; i++) // For every adjacent block around this block, (six blocks)
        {

            Vector3Int blockToSearch = blockPosition + adjacentBlocksOffsets[i]; // The adjacent block to search
            int finalBlockTypeValue;

            if (blockTypes.ContainsKey(blockToSearch)) // If the block exists (has been generated)
            {
                finalBlockTypeValue = blockTypes[blockToSearch];
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
                quadAdded = true;

                if (i == 0)
                {
                    tempVerticesList.AddRange(verticesBack);
                }
                else if (i == 1)
                {
                    tempVerticesList.AddRange(verticesFront);
                }
                else if (i == 2)
                {
                    tempVerticesList.AddRange(verticesRight);
                }
                else if (i == 3)
                {
                    tempVerticesList.AddRange(verticesLeft);
                }
                else if (i == 4)
                {
                    tempVerticesList.AddRange(verticesUp);
                }
                else if (i == 5)
                {
                    tempVerticesList.AddRange(verticesDown);
                }

                loopCount++;

            }


        }
        if (quadAdded)
        {

            Mesh mesh = new Mesh();
            GameObject meshObject = Instantiate(emtpyMeshPrefab, blockPosition+emtpyMeshPrefab.transform.position, emtpyMeshPrefab.transform.rotation); // Create a block object
            meshObject.transform.parent = chunkEmptyObject;
            meshObject.GetComponent<MeshFilter>().mesh = mesh;

            // Create vertices:
            Vector3[] tempMeshVerticesArray = new Vector3[4 * loopCount];

            for (int j = 0; j < tempVerticesList.Count; j++) // Manually add each element from the tempVerticesList<> list to the tempMeshVerticesArray[] array (it's stupid but it must be done manually otherwise C# will have a fit)
            {
                tempMeshVerticesArray[j] = tempVerticesList[j];
            }

            mesh.vertices = tempMeshVerticesArray;


            // Create triangles:
            int[] tempTriangles = new int[6 * loopCount];
            int indexCount = 0;
            // (all vertices use the same triangles, (baseTriangles), I just need to increment the values)
            for (int i = 1; i < loopCount + 1; i++)
            {
                for (int k = 0; k < baseTriangles.Length; k++)
                {
                    tempTriangles[indexCount] = baseTriangles[k] + (4 * (i - 1));
                    indexCount++;
                }
            }

            mesh.triangles = tempTriangles;


            mesh.RecalculateNormals(); // Fixes the weird lighting that the mesh will have
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


}
