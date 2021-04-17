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
    private int[] chunkData;
    private int[] availableChunkNames;
    private int chunkNameCounter = 0;

   // private arr[][][] chunkData;

    public Transform worldParent;
    public Transform chunkParent;
    public GameObject emtpyMeshPrefab;

    public Transform testingGameObjectParent;


    public GameObject[] blockObjects;

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

    private Vector3[] verticesUp =
    {
        new Vector3(0, 1, 0),
        new Vector3(0, 1, 1),
        new Vector3(1, 1, 1),
        new Vector3(1, 1, 0)
    };
    private int[] triangles1 =
    {
        0, 1, 2,
        2, 3, 0
    };
    private int[] triangles2 =
    {
        0, 1, 2,
        2, 3, 0,
        4, 5, 6,
        6, 7, 4
    };
    private int[] triangles3 =
    {
        0, 1, 2,
        2, 3, 0,
        4, 5, 6,
        6, 7, 4,
        8, 9, 10,
        10, 11, 8
    };
    private int[] triangles4 =
    {
        0, 1, 2,
        2, 3, 0,
        4, 5, 6,
        6, 7, 4,
        8, 9, 10,
        10, 11, 8,
        12, 13, 14,
        14, 15, 12
    };
    private int[] triangles5 =
    {
        0, 1, 2,
        2, 3, 0,
        4, 5, 6,
        6, 7, 4,
        8, 9, 10,
        10, 11, 8,
        12, 13, 14,
        14, 15, 12,
        16, 17, 18,
        18, 19, 16
    };
    private int[] triangles6 =
    {
        0, 1, 2,
        2, 3, 0,
        4, 5, 6,
        6, 7, 4,
        8, 9, 10,
        10, 11, 8,
        12, 13, 14,
        14, 15, 12,
        16, 17, 18,
        18, 19, 16,
        20, 21, 22,
        22, 23, 20
    };

    private Vector3[] verticesDown =
    {
        new Vector3(0, 0, 0),
        new Vector3(0, 0, 1),
        new Vector3(1, 0, 1),
        new Vector3(1, 0, 0)
    };
    private int[] trianglesDown =
    {
        0, 1, 2,
        2, 3, 0
    };

    private Vector3[] verticesFront =
    {
        new Vector3(0, 1, 0),
        new Vector3(1, 1, 0), 
        new Vector3(1, 0, 0),
        new Vector3(0, 0, 0)
    };
    private int[] trianglesFront =
    {
        0, 1, 2,
        2, 3, 0
    };

    private Vector3[] verticesBack =
    {
        new Vector3(0, 1, 1),
        new Vector3(0, 0, 1),
        new Vector3(1, 0, 1),
        new Vector3(1, 1, 1)
    };
    private int[] trianglesBack =
    {
        0, 1, 2,
        2, 3, 0
    };

    private Vector3[] verticesLeft =
    {
        new Vector3(0, 1, 0),
        new Vector3(0, 0, 0),
        new Vector3(0, 0, 1),
        new Vector3(0, 1, 1)
    };
    private int[] trianglesLeft =
    {
        0, 1, 2,
        2, 3, 0
    };

    private Vector3[] verticesRight =
    {
        new Vector3(1, 1, 0),
        new Vector3(1, 1, 1),
        new Vector3(1, 0, 1),
        new Vector3(1, 0, 0)
    };
    private int[] trianglesRight =
    {
        0, 1, 2,
        2, 3, 0
    };



    private void Start()
    {


        chunkData = new int[(chunkSize * chunkSize * heightLimit)+1]; // Set's the size of the array
        blockObjects = new GameObject[(chunkSize * chunkSize * heightLimit) + 1];


        chunkVertices.Clear();
        chunkNameCounter = 0;
        DeleteWorld();
        GenerateWorld();
        //print(chunkData.Length);

    }


    // Update is called once per frame
    void Update()
    {
        //GenerateNoise();




        if (Input.GetKeyDown(KeyCode.A))
        {
            int randomIndex = Random.Range(0, chunkData.Length);
            print("Index " + randomIndex + " = " + chunkData[randomIndex]);

        }
        
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
        // Instantiate the empty chunk object in which this chunk's cubes will be placed in
        Transform chunkEmptyObject = Instantiate(chunkParent, chunkParent.position, chunkParent.rotation);
        chunkEmptyObject.name = chunkNameCounter.ToString();

        chunkNameCounter++;

        chunkEmptyObject.parent = worldParent;
        
        for (int y=0; y<heightLimit; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    // Spawn an empty object in that position, even if it is air
                    GameObject block = Instantiate(emtpyMeshPrefab, new Vector3(x, y, z)+emtpyMeshPrefab.transform.position, emtpyMeshPrefab.transform.rotation);
                    block.transform.parent = chunkEmptyObject;
                    SetBlockObject(new Vector3Int(x, y, z), block);
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
                SpawnBlock(xCord, yCord, zCord, chunkEmptyObject);

                Instantiate(cubePrefab, new Vector3(xCord, yCord, zCord), cubePrefab.transform.rotation).transform.parent = testingGameObjectParent;
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
                GenerateMesh(new Vector3Int(xCord, yCord, zCord), chunkEmptyObject); // Generate the mesh for that block
            }
        }

        

    }

    private int ReadChunkData(Vector3Int blockPos)
    {
        return chunkData[(((blockPos.x) * chunkSize) + blockPos.z) + ((chunkSize * chunkSize) * blockPos.y)];
    }

    private GameObject GetBlockObject(Vector3Int blockPos)
    {
        return blockObjects[(((blockPos.x) * chunkSize) + blockPos.z) + ((chunkSize * chunkSize) * blockPos.y)];
    }

    private void SetBlockObject(Vector3Int blockPos, GameObject blockGameObject)
    {
        //print(((((blockPos.x) * chunkSize) + blockPos.z) + ((chunkSize * chunkSize) * blockPos.y)));
        blockObjects[(((blockPos.x) * chunkSize) + blockPos.z) + ((chunkSize * chunkSize) * blockPos.y)] = blockGameObject;
    }
    private void GenerateMesh(Vector3Int blockPosition, Transform chunk)
    {

        int loopCount = 1;
        tempVerticesList.Clear();

        for (int i=0; i<6; i++) // For every adjacent block around this block, (four blocks)
        {
            if (ReadChunkData(blockPosition+adjacentBlocksOffsets[i]+new Vector3Int(0, 1, 0)) == 0) { // Is there air next to me on i side? If so, I need to generate a mesh for that side



                /* COPY FROM TOP OF SCRIPT:
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

               // print(i);
                

                if (i==0)
                {
                    //mesh.vertices = verticesBack;
                    //mesh.vertices = mesh.vertices.Concat(verticesBack).ToArray();
                    tempVerticesList.AddRange(verticesBack);
                    
                }
                else if (i==1)
                {
                    //mesh.vertices = verticesFront;
                    //mesh.vertices = mesh.vertices.Concat(verticesFront).ToArray();
                    tempVerticesList.AddRange(verticesFront);
                }
                else if (i==2)
                {
                    //mesh.vertices = verticesRight;
                    //mesh.vertices = mesh.vertices.Concat(verticesRight).ToArray();
                    tempVerticesList.AddRange(verticesRight);
                }
                else if (i==3)
                {
                    //mesh.vertices = verticesLeft;
                    //mesh.vertices = mesh.vertices.Concat(verticesLeft).ToArray();
                    tempVerticesList.AddRange(verticesLeft);
                }
                else if (i==4)
                {
                    //mesh.vertices = verticesUp;
                    //mesh.vertices = mesh.vertices.Concat(verticesUp).ToArray();
                    tempVerticesList.AddRange(verticesUp);
                }
                else if (i==5)
                {
                    //mesh.vertices = verticesDown;
                    //mesh.vertices = mesh.vertices.Concat(verticesDown).ToArray();
                    tempVerticesList.AddRange(verticesDown);
                }

                loopCount++;

            }

        }

        Mesh mesh = new Mesh();
        GameObject meshObject = GetBlockObject(blockPosition); // Get this block object
        meshObject.GetComponent<MeshFilter>().mesh = mesh;

        //mesh.vertices = new Vector3[4 * loopCount];
        Vector3[] tempMeshVerticesArray = new Vector3[4 * loopCount];

        for (int j = 0; j < tempVerticesList.Count; j++)
        {
       
            tempMeshVerticesArray[j] = tempVerticesList[j];
            //print(tempMeshVerticesArray[j]);
        }

        mesh.vertices = tempMeshVerticesArray;
        /*
        int[] tempTriangles = new int[6 * loopCount];

        for (int j=0; j<loopCount; j++)
        {
            for (int k=0; k<triangles.Length; k++)
            {
                tempTriangles[(loopCount*j)+k] = triangles[k]+(3*(loopCount-1));
            }
        }

        mesh.triangles = tempTriangles;
        */

        if (loopCount == 1)
        {
            mesh.triangles = triangles1;
        }
        else if (loopCount == 2)
        {
            mesh.triangles = triangles2;
        }
        else if (loopCount == 3)
        {
            mesh.triangles = triangles3;
        }
        else if (loopCount == 4)
        {
            mesh.triangles = triangles4;
        }
        else if (loopCount == 5)
        {
            mesh.triangles = triangles5;
        }
        else if (loopCount == 6)
        {
            mesh.triangles = triangles6;
        }
        else
        {
            Debug.LogError("Yeah something went wrong");
            Debug.Break();
        }

        //int[] tempTrianglesArray = triangles;

        //mesh.triangles = mesh.triangles.Concat(tempTrianglesArray).ToArray();

        mesh.RecalculateNormals(); // Fixes the weird lighting that the mesh will have

        

       // print("--------------------------------");
        for (int j = 0; j < mesh.vertices.Length; j++)
        {
            //print(mesh.vertices[j]);
        }


        /*
    for (int x=0; x<chunkSize; x++) // For every x cord in the chunk
    {
        for (int i=x; i<chunkSize; i++) // For every block in the x cord which has not yet been searched
        {
            // Search how many more blocks are in thsi cord which can all geneerate a mesh together
        }

        Mesh mesh = new Mesh();
        GameObject meshObject = Instantiate(emtpyMeshPrefab, emtpyMeshPrefab.transform.position, emtpyMeshPrefab.transform.rotation); // Instantiate an empty object to turn into a mesh
        meshObject.GetComponent<MeshFilter>().mesh = mesh;
        meshObject.transform.parent = chunk;



        Vector3[] vertices;
        int[] triangles;

        vertices = new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(0, 0, 1),
            new Vector3(1, 0, 0),
            new Vector3(1, 0, 1)
        };

        triangles = new int[]
        {
            0, 1, 2,
            1, 3, 2 
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals(); // Fixes the weird lighting that the mesh will have
    }
    */






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

        //return sampleFloat;
        return sample;

    }
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
        chunkData[(((x) * chunkSize) + z)+((chunkSize*chunkSize)*y)] = blockType; // Update this array so we know what type of block there is, (dirt, air etc).

    }


}
