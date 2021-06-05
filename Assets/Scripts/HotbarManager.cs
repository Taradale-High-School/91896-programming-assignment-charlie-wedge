using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HotbarManager : MonoBehaviour
{
    public RectTransform itemBorder;

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

    private int currentlySelectedSlot = 0;
    private float scrollRate = 0.01f; // The time in seconds to wait until we check the scroll wheel again

    // Start is called before the first frame update
    void Start()
    {
        SelectedSlotChanged(); // Initialise

        StartCoroutine(CheckScrollWheel());
    }

    // Update is called once per frame
    void Update()
    {
        // Search to check if they player has pressed a neumeric key within the numberKeyCodes range
        for (int i=0; i<numberKeyCodes.Length; i++)
        {
            if (Input.GetKeyDown(numberKeyCodes[i]))
            {
                currentlySelectedSlot = i;
                SelectedSlotChanged();
            }
        }

    }

    // Using an IEnumerator here allows me to customise the scroll rate. Update() would create an inconsistent scroll rate, and FixedUpdate() has too fast of a scroll rate.
    IEnumerator CheckScrollWheel()
    {
        while (true) // I never thought I would ever do this, but it works.
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
        return ((itemNum * 40)-(40*4));
    }


}
