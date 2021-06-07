using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    private static bool alreadyCopy;
    private bool invoking;

    public bool instantlyPlay; // Should be true when in menus, false when playing the game. Public so the game manager script can change it once we load into a game

    public AudioSource audioSource;

    public AudioClip[] music; // An array of the music that can play in the game. Public so I can reference the music from the project window via the Unity Editor

    // Make sure the music still plays, despite scene changes occuring
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        if (!alreadyCopy)
        {
            alreadyCopy = true; // Get's called the first time the game is loaded
        }
        else
        {
            Destroy(gameObject); // The game has already loaded, therefore destory this sound manager since another copy of it already exists
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        instantlyPlay = true; // When the application first loads, the first scene will be the menu scene, therefore instantlyPlay should initially be set to true.
        PlayRandomSong(); // Play a random song as soon as the player loads up our game
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!audioSource.isPlaying && !invoking)
        {
            if (instantlyPlay)
            {
                PlayRandomSong();
            }
            else
            {
                Invoke("PlayRandomSong", Random.Range(30f, 300f));
                invoking = true;
            }
        }
    }

    public void PlayRandomSong()
    {
        CancelInvoke();
        invoking = false;

        //print("Playing a random song");
        audioSource.Stop();
        audioSource.clip = music[Random.Range(0, music.Length)];
        audioSource.Play();
    }
}
