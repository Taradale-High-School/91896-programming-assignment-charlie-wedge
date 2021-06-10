using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    public CharacterController controller;

    public float speed = 12f;
    public float gravity = -9.81f;
    public float jumpHeight = 2f;

    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    Vector3 velocity;
    bool isGrounded;

    bool playerSpawned;

    public PerlinNoiseGenerator perlinNoiseGeneratorScript;


    // LateStart is called after the other Start() methods in other scripts, and therefore after the world has been generated
    void Start()
    {
        //transform.position = new Vector3(0, perlinNoiseGeneratorScript.GetSurfaceHeight(new Vector2Int(0, 0)), 0);'
    }

    public void SpawnPlayer(bool useStoredPosition) // Public as it's called by the PerlinNoiseGenerator script once the world has loaded for the first ime
    {
        if (useStoredPosition)
        {
            Vector3 playerPosition = GameManager.storedPlayerPosition;
            transform.position = new Vector3(playerPosition.x, playerPosition.y + 1, playerPosition.z); // Add +1 to the y-cord to prevent cliping into the block below the player
            transform.rotation = GameManager.storedPlayerRotation;
        }
        else
        {
            int surfaceLevelAtSpawnPoint = perlinNoiseGeneratorScript.GetSurfaceHeight(new Vector2Int(0, 0), perlinNoiseGeneratorScript.chunks[new Vector2Int(0, 0)].GetComponent<Chunks>().GetBlockTypes());
            transform.position = new Vector3(0, surfaceLevelAtSpawnPoint + 1, 0); // Should be (0, 50, 0), however it's not due to me quickly testing something
        }

        Invoke("PlayerHasSpawned", 0.1f);

    }

    private void PlayerHasSpawned()
    {
        playerSpawned = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (playerSpawned)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }

            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");

            Vector3 move = transform.right * x + transform.forward * z;

            controller.Move(move * speed * Time.deltaTime);

            // jumping velocity: v=sqrt(h*-2*g)
            if (Input.GetButton("Jump") && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            // Gravity: (note - change in y=(1/2)g*t*t)
            velocity.y += gravity * Time.deltaTime;

            controller.Move(velocity * Time.deltaTime);

            if (transform.position.y < -1) // Spawn them back at the surface if they clip through the ground (it does happen)
            {
                transform.position = new Vector3(transform.position.x, perlinNoiseGeneratorScript.heightLimit + 3, transform.position.z);
            }
        }

    }
}
