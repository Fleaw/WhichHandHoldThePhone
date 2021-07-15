using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Barracuda;
using UnityEngine;

public class HoldingHandPrediction : MonoBehaviour
{
    public NNModel _modelAsset;

    public GameObject _leftHandArrow;
    public GameObject _rightHandArrow;
    public GameObject _bothHandArrow;
    
    private Model _runtimeModel;
    private IWorker worker;

    private Dictionary<SelectHand.Hand, GameObject> _mapHandsToArrows = new Dictionary<SelectHand.Hand, GameObject>();

    private bool _isPredictionRunning = false;

    void Start()
    {
        _runtimeModel = ModelLoader.Load(_modelAsset);
        
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharp, _runtimeModel);

        _mapHandsToArrows.Add(SelectHand.Hand.Left, _leftHandArrow);
        _mapHandsToArrows.Add(SelectHand.Hand.Right, _rightHandArrow);
        _mapHandsToArrows.Add(SelectHand.Hand.Both, _bothHandArrow);

        HideAllArrows();

        if(!_isPredictionRunning)
            StartCoroutine(MakePrediction());
    }

    public void StartPrediction()
    {
        if(!_isPredictionRunning)
            StartCoroutine(MakePrediction());
    }

    public void StopPrediction()
    {
        StopAllCoroutines();
        HideAllArrows();

        _isPredictionRunning = false;
    }

    private IEnumerator MakePrediction()
    {
        _isPredictionRunning = true;

        while (true)
        {
            var predictions = new int[10];

            for(int i = 0; i < 10; i++)
            {
                var gravity = Input.gyro.gravity;

                var input = new Tensor(new int[] { 1, 1, 4 }, new float[] { (float)Input.deviceOrientation, gravity.x, gravity.y, gravity.z });

                worker.Execute(input);
                Tensor output = worker.PeekOutput();

                input.Dispose();

                var outputArray = output.ToReadOnlyArray();
                var max = outputArray.Max();
                var index = outputArray.ToList().IndexOf(max);

                predictions[i] = index;

                yield return new WaitForSeconds(0.1f);
            }

            var hand = predictions.GroupBy(v => v).OrderByDescending(x => x.Count()).FirstOrDefault();

            HideAllArrows();

            if (hand != null)
            {
                Debug.Log($"Hand holding the phone: {(SelectHand.Hand)hand.Key}");

                _mapHandsToArrows[(SelectHand.Hand)hand.Key].SetActive(true);
            }
        }
    }

    private void HideAllArrows()
    {
        _leftHandArrow.SetActive(false);
        _rightHandArrow.SetActive(false);
        _bothHandArrow.SetActive(false);
    }
}
