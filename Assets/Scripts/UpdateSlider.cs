using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpdateSlider : MonoBehaviour
{

    public string beginningText;
    public string unit;
    public int multiplier;

    public Text textComponent;

    private void Start()
    {
        //gameObject.GetComponent<Slider>().value = defultValue;
        SliderValueChange(gameObject.GetComponent<Slider>().value); // Initialise the text
    }

    // Called by the slider component when the value is changed. This function updates the text
    public void SliderValueChange(float value)
    {
        textComponent.text = beginningText + " " + Mathf.Round(value * multiplier) + unit;
    }
}
