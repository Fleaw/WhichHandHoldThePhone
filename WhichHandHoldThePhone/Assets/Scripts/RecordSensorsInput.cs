using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Globalization;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System;

[RequireComponent(typeof(SelectHand), typeof(Timer), typeof(HoldingHandPrediction))]
public class RecordSensorsInput : MonoBehaviour
{
    [SerializeField] private Button _startRecordingButton = null;
    //[SerializeField] private Button _stopRecordingButton = null;
    [SerializeField] private Button _deletePreviousRecordButton = null;
    [SerializeField] private Button _deleteAllRecordsButton = null;
    [SerializeField] private Button _sendFileButton = null;
    [SerializeField] private Canvas _interactionCanvas = null;

    public bool IsRecording { get; private set; } = false;

    public string SensorInput { get; private set; }

    //Components
    private SelectHand _selectHand;
    private Timer _timer;
    private HoldingHandPrediction _holdingHandPrediction;

    private NumberFormatInfo _nfi = new NumberFormatInfo();
    private string _filePath = string.Empty;

    private static readonly string LAST_FILE_SENT_CHECKSUM = "LAST_FILE_SENT_CHECKSUM";
    private static readonly string GOFILE_EMAIL = "youremail@mail.com";

    private void Start()
    {
        _selectHand = GetComponent<SelectHand>();
        _timer = GetComponent<Timer>();
        _holdingHandPrediction = GetComponent<HoldingHandPrediction>();

        var basePath = Application.persistentDataPath;
        _filePath = basePath + $"/SensorInputs.txt";

        if (!Input.gyro.enabled) Input.gyro.enabled = true;
        Input.gyro.updateInterval = 0.0167f;

        _nfi.NumberDecimalSeparator = ".";

        _timer.OnTimerEnd += Stop;

        if (File.Exists(_filePath) && File.ReadAllText(_filePath).Length > 0)
            _deleteAllRecordsButton.interactable = true;

        if (FileExistAndNotSent())
        {
            _sendFileButton.interactable = true;
        }
    }

    public void Record()
    {
        if (IsRecording) return;

        SensorInput = string.Empty;

        IsRecording = true;

        _interactionCanvas.gameObject.SetActive(true);
        _timer.StartTimer();

        _startRecordingButton.interactable = false;
        //_stopRecordingButton.interactable = true;
        _deletePreviousRecordButton.interactable = false;
        _deleteAllRecordsButton.interactable = false;
        _sendFileButton.interactable = false;
        _selectHand.SetAllButtonsInteractable(false);

        _holdingHandPrediction.StopPrediction();

        Debug.Log("Recording...");
    }

    public void Stop()
    {
        if (!IsRecording) return;

        _timer.StopTimer();
        _interactionCanvas.gameObject.SetActive(false);

        _startRecordingButton.interactable = true;
        //_stopRecordingButton.interactable = false;
        _selectHand.SetAllButtonsInteractable(true);

        SaveToFile(_filePath, SensorInput);

        _sendFileButton.interactable = true;
        _deletePreviousRecordButton.interactable = true;
        _deleteAllRecordsButton.interactable = true;

        _holdingHandPrediction.StartPrediction();

        IsRecording = false;
    }

    public void SendFile()
    {
        StartCoroutine(SendFileCoroutine());
    }

    private void ShowAndroidToastMessage(string message)
    {
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        if (unityActivity != null)
        {
            AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
            unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                AndroidJavaObject toastObject = toastClass.CallStatic<AndroidJavaObject>("makeText", unityActivity, message, 1);

                toastObject.Call("setGravity", 48, 0, 0);
                toastObject.Call("show");
            }));
        }
    }

    private IEnumerator SendFileCoroutine()
    {
        _sendFileButton.interactable = false;

        if (!IsInternetReachable())
        {
            Debug.Log("Error. Check internet connection!");

#if UNITY_ANDROID && !UNITY_EDITOR
            ShowAndroidToastMessage("Check internet connection!");
#endif
            _sendFileButton.interactable = true;

            yield break;
        }

        //Get File data
        var fileData = File.ReadAllBytes(_filePath);

        if(fileData.Length == 0)
        {
            Debug.Log("File is empty. No need to send.");

#if UNITY_ANDROID && !UNITY_EDITOR
            ShowAndroidToastMessage("File is empty.");
#endif

            yield break;
        }

        var checksum = CalculateChecksum(fileData);

        if (CompareChecksum(checksum))
        {
            Debug.Log("File already sent.");
            yield break;
        }

        /*
        var name = SystemInfo.deviceName;
        if (name.Equals("<unknown>")) name = SystemInfo.deviceModel;
        */

        //Http Post form
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormFileSection("file", fileData, "SensorInputs.txt", "multipart/form-data"));
        formData.Add(new MultipartFormDataSection("email", GOFILE_EMAIL));
        formData.Add(new MultipartFormDataSection("description", SystemInfo.deviceModel));

        //Get GoFile server
        var server = GoFileApi.Api.GetServer;

        var url = $"https://{server}.gofile.io/uploadFile";

        using (UnityWebRequest www = UnityWebRequest.Post(url, formData))
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log($"Error: {www.error}; Url: {url}");

#if UNITY_ANDROID && !UNITY_EDITOR
                ShowAndroidToastMessage($"Error. Please try again later");
#endif
                _sendFileButton.interactable = true;
            }
            else
            {
                PlayerPrefs.SetString(LAST_FILE_SENT_CHECKSUM, checksum);
                PlayerPrefs.Save();

                Debug.Log("File sent.");

#if UNITY_ANDROID && !UNITY_EDITOR
                ShowAndroidToastMessage("File sent.");
#endif

                _sendFileButton.interactable = false;
            }
        }
    }

    private bool IsInternetReachable()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            return false;
        }

        return true;
    }

    private bool FileExistAndNotSent()
    {
        if (File.Exists(_filePath))
        {
            var fileData = File.ReadAllBytes(_filePath);

            if (fileData.Length == 0) return false;

            var checksum = CalculateChecksum(fileData);

            if (CompareChecksum(checksum))
            {
                return false;
            }

            return true;
        }

        return false;
    }

    private bool CompareChecksum(string cheksum)
    {
        return PlayerPrefs.GetString(LAST_FILE_SENT_CHECKSUM, "").Equals(cheksum);
    }

    private string CalculateChecksum(byte[] fileData)
    {
        return Convert.ToBase64String(MD5.Create().ComputeHash(fileData));
    }

    private void Update()
    {
        if(!IsRecording && _selectHand.SelectedHand != SelectHand.Hand.None)
        {
            _startRecordingButton.interactable = true;
        }
        else
        {
            _startRecordingButton.interactable = false;
        }

        if (!IsRecording) return;

        var gravity = $"{(int)Input.deviceOrientation},{Input.gyro.gravity.x.ToString("F3", _nfi)},{Input.gyro.gravity.y.ToString("F3", _nfi)},{Input.gyro.gravity.z.ToString("F3", _nfi)},{_selectHand.SelectedHand}";

        if (!string.IsNullOrEmpty(SensorInput))
            SensorInput += "\n" + gravity;
        else
            SensorInput = gravity;
    }

    private void SaveToFile(string filePath, string data)
    {
        Debug.Log("Writing to File...");

        if (!File.Exists(filePath))
        {
            using (StreamWriter sw = File.CreateText(filePath))
            {
                sw.WriteLine(data);
            }
        }
        else
        {
            using (StreamWriter sw = File.AppendText(filePath))
            {
                sw.WriteLine(data);
            }
        }

        Debug.Log("Done.");
    }

    public void DeletePreviousRecord()
    {
        var newFileLength = RemoveFromEndOfFile(_filePath, SensorInput);

        _deletePreviousRecordButton.interactable = false;

        if (newFileLength == 0)
        {
            _deleteAllRecordsButton.interactable = false;
            _sendFileButton.interactable = false;
        }
    }

    public void DeleteAllRecords()
    {
        if (File.Exists(_filePath))
            File.Delete(_filePath);

        _deleteAllRecordsButton.interactable = false;
        _sendFileButton.interactable = false;
    }

    /// <summary>
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="previousRecord"></param>
    /// <returns>New content length</returns>
    private int RemoveFromEndOfFile(string filePath, string contentToRemove)
    {
        var fileContent = File.ReadAllText(filePath);

        var newContent = fileContent.Remove((fileContent.Length - 1) - (contentToRemove.Length + 1));

        File.WriteAllText(filePath, newContent);

        return newContent.Length;
    }
}
