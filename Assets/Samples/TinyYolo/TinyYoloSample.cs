using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BarracudaSample
{
    public class TinyYoloSample : MonoBehaviour
    {
        [SerializeField] Barracuda.NNModel nnModel;
        [SerializeField] Barracuda.BarracudaWorkerFactory.Type workerType = Barracuda.BarracudaWorkerFactory.Type.ComputePrecompiled;
        [SerializeField] RawImage cameraView;
        [SerializeField] TinyYolo.Result[] results;
        TinyYolo tinyYolo;
        WebCamTexture webcamTexture;

        bool isProcessing = false;
        void Start()
        {
            tinyYolo = new TinyYolo(nnModel, workerType);
            string cameraName = WebCamUtil.FindName();
            webcamTexture = new WebCamTexture(cameraName, 640, 480, 30);
            webcamTexture.Play();
            cameraView.texture = webcamTexture;
        }

        void OnDestroy()
        {
            tinyYolo?.Dispose();
        }

        void Update()
        {
            if (!isProcessing)
            {
                StartCoroutine(ExecuteAsync());
            }
        }

        IEnumerator ExecuteAsync()
        {
            isProcessing = true;
            yield return tinyYolo.ExecuteAsync(webcamTexture);
            results = tinyYolo.GetResults();
            cameraView.uvRect = tinyYolo.GetUVRect((float)webcamTexture.width / (float)webcamTexture.height);
            isProcessing = false;
        }
    }
}