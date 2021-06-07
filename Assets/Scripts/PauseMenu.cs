using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuCanvas;
    public bool gamePaused;

    // Start is called before the first frame update
    void Start()
    {
        
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

}
