using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using System.Linq;

public class PerlinNoiseGenerator : MonoBehaviour
{
    public GameObject cubePrefab; // Prefab to spawn
    public RawImage imageToApplyTo;

    public int perlinTextureSizeX;
    public int perlinTextureSizeY;

    public int perlinGridStepSizeX;
    public int perlinGridStepSizeY;
    public float worldHeightScale;

    public int playerChunkPosX;
    public int playerChunkPosZ;

    public int heightLimit;

    public int chunkSize;
    public int renderDistance;

    public int noiseScale;

    public Vector2 perlinOffset; // The offset in which we search the perlin noise at

    private Texture2D perlinNoiseTexture;

    public List<Vector3> chunkVertices;
    //private int[] chunkDataBlockTypes;
    private int[] availableChunkNames;
    private int chunkNameCounter = 0;

   // private arr[][][] chunkData;

    public Transform worldParent;
    public Transform chunkParent;
    public GameObject emtpyMeshPrefab;

    public Transform testingGameObjectParent;

    private Dictionary<Vector2Int, GameObject> chunks;



    public List<Vector3> tempVerticesList;

    private Vector3Int[] adjacentBlocksOffsets =
    {
        new Vector3Int(0, 0, 1),
        new Vector3Int(0, 0, -1),
        new Vector3Int(1, 0, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, -1, 0)
    };


    private int[] baseTriangles =
{
        0, 1, 2,
        2, 3, 0
    };

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


        // Set the size of these arrays
        int chunksAcross = renderDistance + (renderDistance - 1);
        chunks = new Dictionary<Vector2Int, GameObject>();


        chunkVertices.Clear();
        chunkNameCounter = 0;
        DeleteWorld();
        GenerateWorld();

    }


    // Update is called once per frame
    void Update()
    {
        //GenerateNoise();




        
    }

    // Call this function when generating the world for the first time
    private void GenerateWorld()
    {

        GenerateChunk(0, 0); // The center chunk, (which the player spawns on)

        // let r=current renderDistance 'outline' to spawn
        for (int r = 1; r < renderDistance; r++) // For every 'outline'...
        {
            for (int i = -(r-1); i < r+1; i++) // Basiclly spawns every chunk in the 'outline'
            {
                // For some reason math likes to exclude (negitive, negitive), so I must manually spawn that chunk ;(
                if (r==i && r>0)
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

        print("World Generation Complete. Generated " + (renderDistance + (renderDistance - 1)) * (renderDistance + (renderDistance - 1)) + " chunks!");
        Debug.Break();
    }

    private void DeleteWorld()
    {
        //print(worldParent.childCount);
        for (int i = 0; i < worldParent.childCount; i++)
        {
            Destroy(worldParent.GetChild(i).gameObject); // Delete each chunk which is currently in the hierarchy
        }

    }

    /*
    private void GenerateNoise()
    {
        perlinNoiseTexture = new Texture2D(perlinTextureSizeX, perlinTextureSizeY); // Generate the base texture at the correct size

        for (int x = 0; x < perlinTextureSizeX; x++)
        {
            for (int y = 0; y < perlinTextureSizeY; y++)
            {
                perlinNoiseTexture.SetPixel(x, y, SampleNoise(x, y));
            }
        }

        perlinNoiseTexture.Apply(); // Save (apply) our changes
        imageToApplyTo.texture = perlinNoiseTexture;

    }
    */

    // Returns the value of the specified coordinates by searching in Mathf.PerlinNoise()
    Color SampleNoise(int x, int y)
    {
        // Get valid coordinates so Mathf.PerlinNoise() doesn't get mad at us
        float xCoord = (float)x / perlinTextureSizeX * noiseScale + perlinOffset.x;
        float yCoord = (float)y / perlinTextureSizeY * noiseScale + perlinOffset.y;

        float sample = Mathf.PerlinNoise(xCoord, yCoord); // Get that value
        Color perlinColor = new Color(sample, sample, sample); // This causes a greyscale pixel to be formed
        //print("sample = " + sample + ", colour = " + perlinColor);
        return perlinColor;
    }



    private void GenerateChunk(int chunkX, int chunkZ)
    {
        
        //int[] chunkDataBlockTypes = new int[(chunkSize * chunkSize * heightLimit) + 1];
        //GameObject[] blockObjects = new GameObject[(chunkSize * chunkSize * heightLimit) + 1];

        GameObject[,,] blockObjects = new GameObject[chunkSize, heightLimit, chunkSize];
        int[,,] chunkDataBlockTypes = new int[chunkSize, heightLimit, chunkSize];

        // Instantiate the empty chunk object in which this chunk's cubes will be placed in
        Transform chunkEmptyObject = Instantiate(chunkParent, chunkParent.position, chunkParent.rotation);
        chunkEmptyObject.name = chunkNameCounter.ToString(); // Set it's name
        chunks[new Vector2Int(chunkX, chunkZ)] = chunkEmptyObject.gameObject;

        print("Chunk Number " + chunkNameCounter + " - chunkX = " + chunkX + ", chunkZ = " + chunkZ + ", gameObject = " + chunks[new Vector2Int(chunkX, chunkZ)]);


        chunkEmptyObject.parent = worldParent;

        // For every soon-to-be block in this chunk...
        for (int y=0; y<heightLimit; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    // Spawn an empty object in that position, even if it is air
                    GameObject block = Instantiate(emtpyMeshPrefab, new Vector3(x, y, z)+emtpyMeshPrefab.transform.position, emtpyMeshPrefab.transform.rotation);
                    block.transform.parent = chunkEmptyObject;
                    blockObjects[x, y, z] = block;

                    
                }
            }
        }


        // Go through every possible block in this chunk not on the y cord (spawn surface blocks only) (chunkSize*chunkSize)
        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                int xCord = x + (chunkX * chunkSize);
                int zCord = z + (chunkZ * chunkSize);
                int yCord = Mathf.RoundToInt(SampleStepped(xCord, zCord) * worldHeightScale);
                //print(GetIndex(new Vector3Int(x, yCord, z)));
                chunkDataBlockTypes[x, yCord, z] = 1; // Write to the chunkDataBlockTypes array

                //SpawnBlock(xCord, yCord, zCord, chunkEmptyObject);
                // Instantiate(cubePrefab, new Vector3(xCord, yCord, zCord), cubePrefab.transform.rotation).transform.parent = testingGameObjectParent;
                //SpawnBlocksUnder(xCord, yCord, zCord, chunkEmptyObject);
            }
        }
        
        // Go through every block on the surface again, but this time they have been spawned so now I can generate the meshes for them
        for (int x=0; x<chunkSize; x++)
        {
            for (int z=0; z<chunkSize; z++)
            {
                int xCord = x + (chunkX * chunkSize);
                int zCord = z + (chunkZ * chunkSize);
                int yCord = Mathf.RoundToInt(SampleStepped(xCord, zCord) * worldHeightScale);
                GenerateMesh(new Vector3Int(x, yCord, z), blockObjects[x, yCord, z], chunkDataBlockTypes); // Generate the mesh for that block
            }
        }

        chunkEmptyObject.position = new Vector3(chunkX * chunkSize, chunkEmptyObject.position.y, chunkZ * chunkSize);


        // Feed all of this information to the chunk's script since I don't need it here anymore, but we will need it in the future
        Chunk chunkScript = chunks[new Vector2Int(chunkX, chunkZ)].GetComponent<Chunk>();
        chunkScript.WriteChunkDataBlockTypes(chunkDataBlockTypes);
        chunkScript.WriteChunkBlockObjects(blockObjects);

        // Increment this ready for the next chunk to be generated
        chunkNameCounter++;


        //Debug.Break();

    }

    // Each index in this game's arrays are paired to an x,y,z cord. This function works out said index with a Vector3
    private int GetIndex(Vector3Int pos)
    {
        int num = (((pos.x) * chunkSize) + pos.z) + ((chunkSize * chunkSize) * (pos.y)); // math is evil yet very helpful

        if (num < 0)
        {
            Debug.LogError("Returned index is a negitive number. (function GetIndex()) Number = " + num + "\nx= " + pos.x + "\ny= " + pos.y + "\nz= " + pos.z);
            return 0;
        }

        return num;
    }

    private void GenerateMesh(Vector3Int blockPosition, GameObject block, int[,,] blockDataTypes)
    {



        int loopCount = 0;
        tempVerticesList.Clear();

        for (int i=0; i<6; i++) // For every adjacent block around this block, (six blocks)
        {
            //print("x= " + blockPosition.x + ", y= " + blockPosition.y + ", z= " + blockPosition.z);
            Vector3Int blockToSearch = blockPosition + adjacentBlocksOffsets[i];
            if (blockToSearch.x >= 0 && blockToSearch.y >= 0 && blockToSearch.z >= 0 && blockToSearch.x < chunkSize && blockToSearch.y < heightLimit && blockToSearch.z < chunkSize)
            {
                if (blockDataTypes[blockToSearch.x, blockToSearch.y, blockToSearch.z] == 0)
                { // Is there air next to me on i side? If so, I need to generate a mesh for that side

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

        }

        Mesh mesh = new Mesh();
        GameObject meshObject = block; // Get this block object
        meshObject.GetComponent<MeshFilter>().mesh = mesh;

        // Create vertices:
        Vector3[] tempMeshVerticesArray = new Vector3[4 * loopCount]; // This must be used otherwise it won't work

        for (int j = 0; j < tempVerticesList.Count; j++) // Manually add each element from the tempVerticesList<> list to the tempMeshVerticesArray[] array
        {
       
            tempMeshVerticesArray[j] = tempVerticesList[j]; 
        }

        mesh.vertices = tempMeshVerticesArray;


        // Create triangles:
        int[] tempTriangles = new int[6 * loopCount];
        int indexCount = 0;
        // all vertices use the same triangles, (baseTriangles), I just need to increment the values
        for (int i=1; i<loopCount+1; i++)
        {
            for (int k=0; k<baseTriangles.Length; k++)
            {
                tempTriangles[indexCount] = baseTriangles[k] + (4 * (i - 1));
                indexCount++;
            }
        }

        mesh.triangles = tempTriangles;


        mesh.RecalculateNormals(); // Fixes the weird lighting that the mesh will have



    }

    private float SampleStepped(int x, int y)
    {
        /*
        int gridStepSizeX = perlinTextureSizeX / perlinGridStepSizeX;
        int gridStepSizeY = perlinTextureSizeY / perlinGridStepSizeY;

        float sampleFloat = perlinNoiseTexture.GetPixel((Mathf.FloorToInt(x * gridStepSizeX)), (Mathf.FloorToInt(y * gridStepSizeX))).grayscale;
        */

        // Get valid coordinates so Mathf.PerlinNoise() doesn't get mad at us
        float xCoord = (float)x / perlinTextureSizeX * noiseScale + perlinOffset.x;
        float yCoord = (float)y / perlinTextureSizeY * noiseScale + perlinOffset.y;

        float sample = Mathf.PerlinNoise(xCoord, yCoord); // Get that value

        //print(sample);
        return Mathf.Clamp(sample, 0, 1); // Sometimes perlin noise can return a value outside of the [0, 1] range. Weird right? This fixes that

    }
    /*
    // Spawn blocks from bedrock to the surface
    private void SpawnBlocksUnder(int xCord, int maxY, int zCord, Transform chunk)
    {
        for (int y = 0; y < maxY; y++)
        {
            SpawnBlock(xCord, y, zCord, chunk);
        }
    }
    
    private void SpawnBlock(int x, int y, int z, Transform chunk)
    {
        //Instantiate(cubePrefab, new Vector3(x, y, z), cubePrefab.transform.rotation).transform.parent = chunk;
        //GameObject block = Instantiate(emtpyMeshPrefab, new Vector3(x, y, z), emtpyMeshPrefab.transform.rotation);
        //block.transform.parent = chunk;

        WriteBlockToChunkData(x, y, z, 1);
        //chunkVertices.Add(new Vector3(x, y, z));
    }

    private void WriteBlockToChunkData(int x, int y, int z, int blockType)
    {
        chunkVertices.Add(new Vector3(x, y, z)); // Write the block's vertices to this list, (so we can convert them to meshes later on). 
        //x++;
        //z++;
        chunkDataBlockTypes[(((x) * chunkSize) + z)+((chunkSize*chunkSize)*y)] = blockType; // Update this array so we know what type of block there is, (dirt, air etc).

    }
    */
    


}
