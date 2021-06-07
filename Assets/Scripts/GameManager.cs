using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{

    public Dictionary<Vector2Int, int[,,]> previousSaveWorldData;
    public Vector3 previousSavePlayerPosition;
    public bool previousSaveLoaded;

    public int seed;

    private static bool alreadyCopy;
    public SoundManager soundManagerScript;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        if (!alreadyCopy)
        {
            alreadyCopy = true; // Get's called the first time the game is loaded
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
