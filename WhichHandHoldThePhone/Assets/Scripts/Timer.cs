using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    [SerializeField] private int _initialValue = 30;
    [SerializeField] private Text _timerText = null;

    private int _currentTimerValue;

    public delegate void TimerEvent();

    public TimerEvent OnTimerEnd;

    public void StartTimer()
    {
        _currentTimerValue = _initialValue;

        StartCoroutine(Countdown());
    }

    IEnumerator Countdown()
    {
        while (_currentTimerValue >= 1)
        {
            yield return new WaitForSeconds(1);

            _currentTimerValue--;
            _timerText.text = _currentTimerValue.ToString();
        }

        OnTimerEnd.Invoke();
    }

    public void StopTimer()
    {
        StopAllCoroutines();

        _timerText.text = _initialValue.ToString();
    }
}
