// Keeps track of the previous saved game, and also converts this to and from int[] and dictionaries

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class GameManager : MonoBehaviour
{
    // These two variables can only be written to/read by the WriteChunkData() and ReadChunkData() functions below.
    public int[] storedChunkData; // The block data in the saved game
    public int[] storedChunkPositions; // The chunk positions in the saved game

    public static Vector3 storedPlayerPosition;
    public static Quaternion storedPlayerRotation;
    public static string storedWorldName;
    public static bool storedDataPresent;
    public static int storedSeed;

    public static int[] storedHotbarBlockTypes;
    public static int[] storedHotbarBlockCount;

    public static bool loadPreviousSave; // Load the current saved game or create a new world? This boolean is checked by the PerlinNoiseGenerator script whenever the GameScene is loaded

    public static int currentSeed;
    public static string currentWorldName;

    private static bool alreadyCopy;
    public SoundManager soundManagerScript;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        if (!alreadyCopy)
        {
            alreadyCopy = true; // Get's called the first time the game is loaded
            //storedWorldData = new Dictionary<Vector2Int, int[,,]>();
        }
        else
        {
            Destroy(gameObject); // The game has already loaded, therefore destory this game manager since another copy of it already exists
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Write dataToWrite to the storedChunkData array in this GameManager, therefore saving the game to memory
    // This function basiclly converts a Dictionary<Vector2Int, GameObject> to two int[] for saving to memory. Saving the Dictionary/other forms of a dictionary would cause weird problems since these dictionaries are reference types, meaning an 'exit without saving' function would be impossible
    public void WriteChunkData(Dictionary<Vector2Int, GameObject> dataToWrite)
    {
        int dataIndexCount = 0;
        int posIndexCount = 0;

        int keyLength = dataToWrite.Keys.ToArray<Vector2Int>().Length;
        storedChunkData = new int[16 * 16 * 128 * keyLength];
        storedChunkPositions = new int[keyLength * 2];


        foreach (KeyValuePair<Vector2Int, GameObject> chunk in dataToWrite)
        {
            Chunks chunkScript = chunk.Value.GetComponent<Chunks>();
            if (chunkScript.meshGenerated)
            {

                storedChunkPositions[posIndexCount] = chunk.Key.x;
                storedChunkPositions[posIndexCount+1] = chunk.Key.y;
                posIndexCount += 2;

                int[,,] blockTypes = chunkScript.GetBlockTypes();
                for (int x=0; x<16; x++)
                {
                    for (int y=0; y<128; y++)
                    {
                        for (int z=0; z<16; z++)
                        {
                            storedChunkData[dataIndexCount] = blockTypes[x, y, z];


                            dataIndexCount++;
                        }
                    }
                }
            }
        }
        print("Writing chunk data");
    }

    // Read the saved game, created in the above function. This function basiclly converts the two int[]s created in the above function into a Dictionary<Vector2Int, int[,,]>, which ultimately is redable by the PerlinNoiseGenerator script.
    public Dictionary<Vector2Int, int[,,]> ReadChunkData()
    {
        Dictionary<Vector2Int, int[,,]> tempDictionary = new Dictionary<Vector2Int, int[,,]>();

        int indexCount = 0;
        for (int i=0; i<storedChunkPositions.Length; i += 2)
        {
            int[,,] tempArray = new int[16,128,16];

            
            for (int x=0; x<16; x++)
            {
                for (int y=0; y<128; y++)
                {
                    for (int z=0; z<16; z++)
                    {
                        tempArray[x, y, z] = storedChunkData[indexCount];
                        indexCount++;
                    }
                }
            }

            
            tempDictionary[new Vector2Int(storedChunkPositions[i], storedChunkPositions[i + 1])] = tempArray;
        }
        print("Reading chunk data");
        return tempDictionary;
    }

    // Update is called once per frame
    void Update()
    {
  
    }

    public void ChangeScene(int buildIndex) // buildIndex 0 = menu scene, 1 = game scene
    {
        soundManagerScript.instantlyPlay = buildIndex == 0 ? true : false; // If the current scene is the menu scene, (buildIndex = 0 ), then set instantlyPlay to true. Otherwise set it to false, since we'll most likely be in the game scene.
        SceneManager.LoadScene(buildIndex);
    }



}
