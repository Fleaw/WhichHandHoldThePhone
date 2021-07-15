using UnityEngine;
using UnityEngine.UI;

public class GyroscopeDisplay : MonoBehaviour
{
    public Text _text;

    private void Start()
    {
        if (!Input.gyro.enabled)
            Input.gyro.enabled = true;

        Input.gyro.updateInterval = 0.0167f;
    }

    void Update()
    {
        RotateGizmo();
        DebugGyroValues();
    }

    private void RotateGizmo()
    {
        transform.rotation = GyroToQuaternion(Input.gyro.attitude);
    }

    private void DebugGyroValues()
    {
        var text = $"Attitude: {Input.gyro.attitude}\n";
        text += $"Gravity: {Input.gyro.gravity}\n";
        text += $"Rotation Rate: {Input.gyro.rotationRate}\n";
        text += $"User Acceleration: {Input.gyro.userAcceleration}\n";
        text += $"Last Acceleration: {Input.acceleration}";

        _text.text = text;
    }

    private Quaternion GyroToQuaternion(Quaternion q)
    {
        return new Quaternion(q.x, q.y, -q.z, -q.w);
    }
}
