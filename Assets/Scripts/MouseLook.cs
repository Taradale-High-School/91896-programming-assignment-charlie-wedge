﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public float mouseSensitivity = 100f;

    public Transform playerBody;

    float xRotation = 0f;

    public PerlinNoiseGenerator perlinNoiseGeneratorScript;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Ensure the player doesn't look up or down beyond 90º

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);

        // Temp code:
        if (Input.GetKeyDown(KeyCode.Z))
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        // Mouse presses: (breaking and placing blocks)
        if (Input.GetMouseButtonDown(0))
        {
           // print("Mouse pressed!");

            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(0, 0, 0));

            if (Physics.Raycast(ray, out hit)) // Send a raycast, and output the mesh info it hits throught the hit variable. Only returns through if it hits a block
            {
                //print(hit.point);

                int chunkSize = perlinNoiseGeneratorScript.chunkSize;
                Vector3Int blockWorldPosition = new Vector3Int(Mathf.RoundToInt(hit.point.x), Mathf.FloorToInt(hit.point.y), Mathf.RoundToInt(hit.point.z));
                Vector2Int chunkPosition = new Vector2Int(Mathf.RoundToInt(blockWorldPosition.x / chunkSize), Mathf.RoundToInt(blockWorldPosition.z / chunkSize));
                Vector3Int blockLocalPosition = new Vector3Int(Mathf.Abs(blockWorldPosition.x % chunkSize), blockWorldPosition.y, Mathf.Abs(blockWorldPosition.z % chunkSize));

                print("Block position = " + blockLocalPosition);
                print("Chunk position = " + chunkPosition);

                GameObject chunkObject = perlinNoiseGeneratorScript.chunks[chunkPosition];
                chunkObject.GetComponent<Chunks>().EditBlockTypes(blockLocalPosition, -1);

                perlinNoiseGeneratorScript.ReloadChunk(chunkObject, chunkPosition.x, chunkPosition.y);
            }
        }
    }
}
