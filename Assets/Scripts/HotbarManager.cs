// Keeps track of the player's inventory and also allows the player to use the Hotbar. It also gives the player blocks and takes blocks away when they are placed/blocking

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HotbarManager : MonoBehaviour
{
    public RectTransform itemBorder;
    public GameObject blockPrefab;
    public GameObject textPrefab;

    public PerlinNoiseGenerator perlinNoiseGeneratorScript;
    public PauseMenu pauseMenuScript;

    // I hate this, but I'm not sure there's any other way to get all neumeric keys
    private KeyCode[] numberKeyCodes =
    {
        KeyCode.Alpha1,
        KeyCode.Alpha2,
        KeyCode.Alpha3,
        KeyCode.Alpha4,
        KeyCode.Alpha5,
        KeyCode.Alpha6,
        KeyCode.Alpha7,
        KeyCode.Alpha8,
        KeyCode.Alpha9
    };


    private MeshFilter[] slotObjects = new MeshFilter[9]; // A reference to each slot object
    private Text[] slotTexts = new Text[9];
    public int[] inventoryBlockTypes = new int[9]; // The type of block in each slot
    public int[] inventoryBlockCount = new int[9]; // The quantity of the item in each slot

    private int currentlySelectedSlot = 0;
    private float scrollRate = 0.01f; // The time in seconds to wait until we check the scroll wheel again
    public int maximumStackCount;

    private Mesh masterMeshForBlocks;


    private void Awake()
    {
        if (GameManager.loadPreviousSave)
        {
            inventoryBlockTypes = GameManager.storedHotbarBlockTypes;
            inventoryBlockCount = GameManager.storedHotbarBlockCount;
        }
        else
        {
            for (int i = 0; i < 9; i++)
            {
                inventoryBlockTypes[i] = -1;
                inventoryBlockCount[i] = 0;
            }
        }
    }

    // Start is called before the first frame update. This Start function just initialises stuff.
    void Start()
    {
        // Initialise stuff:


        CreateHotbarItemGameObjects();
        SelectedSlotChanged();

        StartCoroutine(CheckScrollWheel());

        GenerateItemMesh();

        for (int i=0; i<9; i++) // Check if the hotbar already has items in it (if loaded from a previous save). If so, generate the UV for that item!
        {
            if (inventoryBlockTypes[i] != -1)
            {
                GenerateUVs(i, inventoryBlockTypes[i]);
                UpdateSlotText(i);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!pauseMenuScript.gamePaused)
        {
            // Search to check if they player has pressed a neumeric key within the numberKeyCodes range
            for (int i = 0; i < numberKeyCodes.Length; i++)
            {
                if (Input.GetKeyDown(numberKeyCodes[i]))
                {
                    currentlySelectedSlot = i;
                    SelectedSlotChanged();
                }
            }
        }
    }

    // Using an IEnumerator here allows me to customise the scroll rate. Update() would create an inconsistent scroll rate, and FixedUpdate() has too fast of a scroll rate.
    IEnumerator CheckScrollWheel()
    {
        while (true) // I never thought I would ever do this, but it works.
        {
            if (!pauseMenuScript.gamePaused)
            {
                // Check the scroll wheel:
                float mouseScroll = Input.mouseScrollDelta.y;
                if (mouseScroll > 0)
                {
                    currentlySelectedSlot++;
                    SelectedSlotChanged();
                }
                else if (mouseScroll < 0)
                {
                    currentlySelectedSlot--;
                    SelectedSlotChanged();
                }
            }

            yield return new WaitForSeconds(scrollRate);
        }
    }


    // Call whenever the currentlySelectedSlot variable is change.
    private void SelectedSlotChanged()
    {
        if (currentlySelectedSlot > 8)
        {
            currentlySelectedSlot = 0;
        }
        else if (currentlySelectedSlot < 0)
        {
            currentlySelectedSlot = 8;
        }

        // Change the position of the hotbar border to visually show the player which block they have selected
        itemBorder.localPosition = new Vector3(GetBorderPosition(currentlySelectedSlot), itemBorder.localPosition.y, itemBorder.localPosition.z);

    }


    // Return the x position of the item border based on the itemNum they have selected. EG: item 0 = -160, item 4 = 0, item 8 = 160
    private int GetBorderPosition(int itemNum) // itemNum should have a range of 0-8. If my code works, a boundary test is unnecessary
    {
        return ((itemNum * 40) - (40 * 4));
    }

    // Adds a block to the player's inventory. Public so the mouseLook script can call it when a block is broken
    public bool GivePlayerBlock(int blockType) // Returns false if the block can't be broken due to no space in the inventory
    {
        int slotToWriteTo = FindAvailableBlockTypeSlotInInventory(blockType);
        if (slotToWriteTo == -1) // If the block is not yet present in the inventory (or at least a stack with free space in it), then find a new slot to write to
        {
            if (inventoryBlockTypes[currentlySelectedSlot] == -1) // If the slot currently selected is blank, write to that slot
            {
                slotToWriteTo = currentlySelectedSlot;
            }
            else // Otherwise write to the first available slot
            {
                int firstAvailableSlot = FindFirstAvailableSlot();
                if (firstAvailableSlot == -1) // Don't break the block if their inventory is full (-1 = no slot found)
                {
                    return false;
                }
                slotToWriteTo = firstAvailableSlot;
            }

            GenerateUVs(slotToWriteTo, blockType);
            inventoryBlockTypes[slotToWriteTo] = blockType;
        }
        else if (inventoryBlockTypes[currentlySelectedSlot] == blockType && inventoryBlockCount[currentlySelectedSlot] < maximumStackCount) // Or if the player is holding the block type that their breaking, (rare, but for if it's not the most left position)
        {
            slotToWriteTo = currentlySelectedSlot;
        }

        ChangeSlotValue(slotToWriteTo, true);
        

        return true;
    }

    public int TakePlayerBlock()
    {
        if (inventoryBlockTypes[currentlySelectedSlot] == -1) // If there is no block in the currently selected slot
        {
            return -1; // -1 means don't place a block
        }
        ChangeSlotValue(currentlySelectedSlot, false);
        int blockType = inventoryBlockTypes[currentlySelectedSlot];
        if (inventoryBlockCount[currentlySelectedSlot] == 0) // If there are now no blocks in that slot after placing one, remove the block texture from the hotbar
        {
            slotObjects[currentlySelectedSlot].mesh.Clear();
            inventoryBlockTypes[currentlySelectedSlot] = -1;
        }

        return blockType;
    }

    // Increase or decrease the number of blocks in slot slotNum
    private void ChangeSlotValue(int slotNum, bool increase)
    {
        inventoryBlockCount[slotNum] += (increase ? 1 : -1);
        UpdateSlotText(slotNum);
    }

    private void UpdateSlotText(int slotNum)
    {
        slotTexts[slotNum].text = inventoryBlockCount[slotNum].ToString();
    }

    private int FindFirstAvailableSlot()
    {
        for (int i=0; i<9; i++)
        {
            if (inventoryBlockTypes[i] == -1)
            {
                return i;
            }
        }
        return -1;
    }

    private int FindAvailableBlockTypeSlotInInventory(int blockType)
    {
        for (int i=0; i<9; i++)
        {
            if (inventoryBlockTypes[i] == blockType && inventoryBlockCount[i] < maximumStackCount)
            {
                return i;
            }
        }
        return -1;
    }

    // Instantiate the nine gameObjects in the hotbar which could show a block
    private void CreateHotbarItemGameObjects()
    {
        for (int i = 0; i < 9; i++)
        {
            GameObject instantiatedObject = Instantiate(blockPrefab, blockPrefab.transform.position, blockPrefab.transform.rotation, transform);
            instantiatedObject.name = "Item Slot Object " + i;
            instantiatedObject.transform.localScale = blockPrefab.transform.localScale + new Vector3(i < 4 ? 4-i : 0, 0, i > 4 ? i-4 : 0);
            instantiatedObject.transform.localPosition = new Vector3(GetBorderPosition(i) + ((i-3) * -1), 0, 0) + blockPrefab.transform.position;

            GameObject instantiantedTextObject = Instantiate(textPrefab, textPrefab.transform.position, textPrefab.transform.rotation, instantiatedObject.transform);
            instantiantedTextObject.name = "Item Text " + i;
            instantiantedTextObject.transform.localPosition = textPrefab.transform.localPosition;
            instantiantedTextObject.transform.rotation = Quaternion.identity;

            slotTexts[i] = instantiantedTextObject.GetComponent<Text>();
            slotObjects[i] = instantiatedObject.GetComponent<MeshFilter>();
        }
        
    }

    private void GenerateItemMesh()
    {
        // The vertices required to create a mesh for a block in the hotbar
        Vector3[] vertices =
        {
        // Up:
        new Vector3(0, 1, 0),
        new Vector3(0, 1, 1),
        new Vector3(1, 1, 1),
        new Vector3(1, 1, 0),
        // Right:
        new Vector3(1, 1, 0),
        new Vector3(1, 1, 1),
        new Vector3(1, 0, 1),
        new Vector3(1, 0, 0),
        // Front:
        new Vector3(0, 1, 0),
        new Vector3(1, 1, 0),
        new Vector3(1, 0, 0),
        new Vector3(0, 0, 0)
        };
        // The triangles required to create a mesh for a block in the hotbar
        int[] trianlges =
        {
            0, 1, 2,
            2, 3, 0,

            4, 5, 6,
            6, 7, 4,

            8, 9, 10,
            10, 11, 8
        };

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = trianlges;

        masterMeshForBlocks = mesh;

    }

    // Generate the UVs (textures) to the hotbar block
    private void GenerateUVs(int slotNum, int blockType)
    {
        MeshFilter blockFilter = slotObjects[slotNum];

        blockFilter.mesh = masterMeshForBlocks;

        List<Vector2> uvsList = new List<Vector2>();
        float tileOffset = 1f / 16f; // 0.0625

        for (int i=0; i<3; i++)
        {
            Vector2 uvBlockVector2 = perlinNoiseGeneratorScript.blockIDs[(blockType * 3) + (i == 0 ? 0 : 1)]; // Get the UV position
            float ublock = uvBlockVector2.x;
            float vblock = uvBlockVector2.y;
            float umin = tileOffset * ublock;
            float umax = tileOffset * (ublock + 1f);
            float vmin = tileOffset * vblock;
            float vmax = tileOffset * (vblock + 1f);

            // I am very lucky that these four lines generate all three UV quads the right way round, and there's no need for any if() statements
            uvsList.Add(new Vector2(umin, vmax));
            uvsList.Add(new Vector2(umax, vmax));
            uvsList.Add(new Vector2(umax, vmin));
            uvsList.Add(new Vector2(umin, vmin));
        }

        blockFilter.mesh.uv = uvsList.ToArray();
        blockFilter.mesh.RecalculateNormals();
    }


}
