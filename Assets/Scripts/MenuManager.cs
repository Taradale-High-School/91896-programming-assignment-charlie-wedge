// Managers the menu buttons and loads / creates the game when those buttons are pressed

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    // Create World Menu Stuff:
    public GameObject errorImage;
    public Text errorText;

    public InputField worldNameInputText;
    public InputField seedInputText;

    public Button generateWorldButton;

    // Options menu stuff:
    public Slider volumeSlider;
    public Slider renderDistanceSlider;
    public Slider sensitivitySlider;
    public Text invertMouseButtonText;

    // Main Menu Stuff:
    public GameObject mainMenuCanvas;
    public GameObject createWorldCanvas;

    public Button loadWorldButton;

    // Other General Stuff:
    private GameManager gameManagerScript;
    private AudioSource soundManagerAudioSource;

    // Start is called before the first frame update
    void Start()
    {
        gameManagerScript = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        soundManagerAudioSource = GameObject.FindGameObjectWithTag("SoundManager").GetComponent<AudioSource>();

        // Make sure the sliders and buttons in the Options menu are up-to-date, since they defult to their defult value when the scene is loaded
        volumeSlider.value = soundManagerAudioSource.volume;
        renderDistanceSlider.value = PerlinNoiseGenerator.renderDistance;
        sensitivitySlider.value = MouseLook.mouseSensitivity;
        ChangeInvertMouseText(MouseLook.invertMouse);

        if (GameManager.storedDataPresent)
        {
            loadWorldButton.interactable = true;
            loadWorldButton.gameObject.transform.GetChild(0).GetComponent<Text>().text = "Load Save: " + GameManager.storedWorldName;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    // Called when the player has finished editing the 'World Name' input box (OnEndEdit())
    public void WorldNameInputExited(string name)
    {
        CheckWorldName(name);
    }
    private bool CheckWorldName(string name)
    {
        if (name.Length == 1) // The Input Field Component takes care of the character limit of 23. I just need to check for the character minimum. (A true boundry test could be found in the ValidYCord() function in the PerlinNoiseGenerator script)
        {
            DisplayErrorMessage("Please enter more than one character");
        }
        else if (name.Length == 0) // Reset the input box if everything has been deleted
        {
            ChangeErrorVisibility(false);
            generateWorldButton.interactable = false;
        }
        else if (float.TryParse(name, out float result)) // Check if it's just numbers
        {
            DisplayErrorMessage("Please enter letters");
        }
        else // The world name is vaild
        {
            ChangeErrorVisibility(false);
            return true;
        }
        return false;
    }
    
    private void DisplayErrorMessage(string message)
    {
        errorText.text = message;
        ChangeErrorVisibility(true);
    }
    private void ChangeErrorVisibility(bool visible)
    {
        errorImage.SetActive(visible);
        generateWorldButton.interactable = !visible;
        if (!visible)
        {
            errorText.text = "";
        }
    }


    public void GenerateWorldButtonPressed()
    {
        string worldName = worldNameInputText.text;
        if (!CheckWorldName(worldName)) // Usually this isn't needed as the WorldNameInputExited() function catches a bad name before this button is pressed, but this is here just in case.
        {
            return;
        }

        // Seed stuff:
        int seedFinal;
        string rawSeedInput = seedInputText.text; // What the player entered
        // If the player has entered a seed, then convert it to an int, (since they can enter letters) through ASCII
        if (rawSeedInput.Length > 0) // If the player has entered a seed, use it or convert it if it's not vaild
        {

            // "If the seed contains characters other than numbers or is greater than or equal to 20 characters in length, the hash code is used to generate a number seed"
            if (!int.TryParse(rawSeedInput, out int result) || rawSeedInput.Length >= 20)
            {
                seedFinal = rawSeedInput.GetHashCode();
            }
            else // If it's vaild, use it
            {
                seedFinal = result;
            }
        }
        else // The player has not given us a seed, so we'll pick a random one for them
        {
            Random.InitState((int)System.DateTime.Now.Ticks); // Reset the C# seed for the world seed, otherwise the same world would be generated from the world seed used in the previous world
            seedFinal = Random.Range(-2147483647, 2147483647); // Generate a random number within the int32 range
        }
        print("The user has entered the seed [" + rawSeedInput + "] which has been converted to [" + seedFinal + "].");
        GameManager.currentSeed = seedFinal;
        GameManager.currentWorldName = worldName;

        gameManagerScript.ChangeScene(1); // Change to the game scene

    }

    public void LoadPreviousWorld()
    {
        GameManager.loadPreviousSave = true;

        GameManager.currentSeed = GameManager.storedSeed;
        GameManager.currentWorldName = GameManager.storedWorldName;

        gameManagerScript.ChangeScene(1); // Change to the game scene
    }

    // Hopefully they never feel the need to press this button
    public void ExitGame()
    {
        print("Quitting game!");
        Application.Quit();
    }

    // Called whenever the volume slider is changed
    public void ChangeMusicVolume()
    {
        soundManagerAudioSource.volume = Mathf.Round(volumeSlider.value*100)/100;
    }
    public void ChangeRenderDistance()
    {
        PerlinNoiseGenerator.renderDistance = Mathf.FloorToInt(renderDistanceSlider.value);
    }
    public void ChangeSensitivity()
    {
        MouseLook.mouseSensitivity = sensitivitySlider.value;
    }
    public void InvertMouseButtonPressed()
    {
        bool newState = !MouseLook.invertMouse;
        MouseLook.invertMouse = newState;

        ChangeInvertMouseText(newState);
    }
    private void ChangeInvertMouseText(bool newState)
    {
        invertMouseButtonText.text = "Invert Mouse: " + (newState ? "ON" : "OFF");
    }


}
