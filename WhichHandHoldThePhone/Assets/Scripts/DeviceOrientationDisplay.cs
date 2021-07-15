using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class DeviceOrientationDisplay : MonoBehaviour
{
    public Text _deviceOrientationText;

    // Update is called once per frame
    void Update()
    {
        _deviceOrientationText.text = Regex.Replace(Input.deviceOrientation.ToString(), "([a-z])([A-Z])", "$1 $2");
    }
}
