// Pauses the game and saves the game when that button is pressed

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuCanvas;
    public bool gamePaused;

    public Transform player;

    private GameManager gameManagerScript;
    public PerlinNoiseGenerator perlinNoiseGeneratorScript;
    public HotbarManager hotbarManagerScript;

    // Start is called before the first frame update
    void Start()
    {
        gameManagerScript = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("escape"))
        {
            ChangePauseState(!gamePaused);
        }
    }

    public void ChangePauseState(bool pause)
    {
        gamePaused = pause;
        pauseMenuCanvas.SetActive(pause);
        Time.timeScale = pause ? 0 : 1;
        if (pause)
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void ExitGame()
    {
        Time.timeScale = 1;
        gameManagerScript.ChangeScene(0); // Change to the menu scene

    }

    public void SaveGame(bool exit)
    {
 

        //GameManager.storedWorldData.Clear();

        /*
        Vector2Int[] allKeys = perlinNoiseGeneratorScript.chunks.Keys.ToArray<Vector2Int>();
        for (int i=0; i<allKeys.Length; i++)
        {
            Chunks chunkScript = perlinNoiseGeneratorScript.chunks[allKeys[i]].GetComponent<Chunks>();
            if (chunkScript.meshGenerated)
            {
                GameManager.storedWorldData[allKeys[i]] = chunkScript.GetBlockTypes();
            }
        }
        */

        Dictionary<Vector2Int, GameObject> tempD = new Dictionary<Vector2Int, GameObject>();
     foreach (KeyValuePair<Vector2Int, GameObject> chunk in perlinNoiseGeneratorScript.chunks)
     {
         Chunks chunkScript = chunk.Value.GetComponent<Chunks>();
         if (chunkScript.meshGenerated)
         {
             tempD[chunk.Key] = chunk.Value;

             //GameManager.storedWorldData[chunk.Key] = chunkScript.GetBlockTypes();
         }
     }
     
        /*
        for (int x=-50; x<50; x++)
        {
            for (int y=-50; y<50; y++)
            {
                if (perlinNoiseGeneratorScript.chunks.ContainsKey(new Vector2Int(x, y)))
                {
                    GameManager.storedWorldData[new Vector2Int(x, y)] = perlinNoiseGeneratorScript.chunks[new Vector2Int(x, y)].GetComponent<Chunks>().GetBlockTypes();
                }
            }
        }
        */

        gameManagerScript.WriteChunkData(tempD);

        // Override the current saved game with this game
        GameManager.storedPlayerPosition = player.position;
        GameManager.storedPlayerRotation = player.rotation;
        GameManager.storedWorldName = GameManager.currentWorldName;
        GameManager.storedSeed = GameManager.currentSeed;
        GameManager.storedDataPresent = true;
        GameManager.storedHotbarBlockTypes = hotbarManagerScript.inventoryBlockTypes;
        GameManager.storedHotbarBlockCount = hotbarManagerScript.inventoryBlockCount;

        if (exit)
        {
            ExitGame();
        }
    }

}
