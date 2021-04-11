using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    private int[] chunkData;
    private int[] availableChunkNames;
    private int chunkNameCounter = 0;

   // private arr[][][] chunkData;

    public Transform worldParent;
    public Transform chunkParent;

    private void Start()
    {

        

        chunkData = new int[(chunkSize * chunkSize * heightLimit)+1]; // Set's the size of the array
        print(chunkData.Length);

    }


    // Update is called once per frame
    void Update()
    {
        //GenerateNoise();

        chunkNameCounter = 0;
        DeleteWorld();
        GenerateWorld();


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



    private void GenerateChunk(int chunkX, int chunkY)
    {
        // Instantiate the empty chunk object in which this chunk's cubes will be placed in
        Transform chunkEmptyObject = Instantiate(chunkParent, chunkParent.position, chunkParent.rotation);
        chunkEmptyObject.name = chunkNameCounter.ToString();

        chunkNameCounter++;

        chunkEmptyObject.parent = worldParent;

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                int xCord = x + (chunkX * chunkSize);
                int zCord = y + (chunkY * chunkSize);
                int yCord = Mathf.RoundToInt(SampleStepped(xCord, zCord) * worldHeightScale);
                SpawnBlock(xCord, yCord, zCord, chunkEmptyObject);
                //SpawnBlocksUnder(xCord, yCord, zCord, chunkEmptyObject);
            }
        }

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
        Instantiate(cubePrefab, new Vector3(x, y, z), cubePrefab.transform.rotation).transform.parent = chunk;
        WriteToChunkData(x, y, z);
    }

    private void WriteToChunkData(int x, int y, int z)
    {
        x++;
        z++;
        chunkData[(((x - 1) * chunkSize) + z)+((chunkSize*chunkSize)*y)] = 1;
    }
}
