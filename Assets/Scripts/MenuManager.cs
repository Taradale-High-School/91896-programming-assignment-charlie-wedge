using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    // Create World Menu Stuff:
    public GameObject errorImage;
    public Text errorText;

    public Text worldNameInputText;
    public Text seedInputText;

    public Button generateWorldButton;


    // Main Menu Stuff:
    public GameObject mainMenuCanvas;
    public GameObject createWorldCanvas;

    public Button loadWorldButton;

    // Other General Stuff:
    private GameManager gameManagerScript;

    // Start is called before the first frame update
    void Start()
    {
        gameManagerScript = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();

        if (gameManagerScript.previousSaveLoaded)
        {
            loadWorldButton.interactable = true;
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
        if (!CheckWorldName(worldNameInputText.text)) // Usually this isn't needed as the WorldNameInputExited() function catches a bad name before this button is pressed, but this is here just in case.
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
        gameManagerScript.seed = seedFinal;

        gameManagerScript.ChangeScene(1); // Change to the game scene

    }
}
