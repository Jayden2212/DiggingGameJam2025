using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements.Experimental;

public class SliderController : MonoBehaviour
{
    public float maxSliderAmount;
    public void SliderChange(float value)
    {
        float localValue = value * maxSliderAmount;

        if (gameObject.name.Equals("VolumeSlider"))
        {
            AudioSource audio = GameObject.Find("AudioHolder").GetComponent<AudioSource>();
            audio.volume = localValue;
        }
    }
}
