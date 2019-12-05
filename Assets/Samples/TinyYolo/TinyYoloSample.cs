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
        [SerializeField] Text framePrefab = null;
        [SerializeField, Range(0f, 1f)] float scoreThreshold = 0.2f;
        [SerializeField] TextAsset labelsText;

        TinyYolo tinyYolo;
        WebCamTexture webcamTexture;
        Text[] frames;
        string[] labels;

        bool isProcessing = false;
        void Start()
        {
            tinyYolo = new TinyYolo(nnModel, workerType);
            string cameraName = WebCamUtil.FindName();
            webcamTexture = new WebCamTexture(cameraName, 640, 480, 30);
            webcamTexture.Play();
            cameraView.texture = webcamTexture;

            frames = new Text[25];
            for (int i = 0; i < frames.Length; i++)
            {
                frames[i] = Instantiate<Text>(framePrefab, Vector3.zero, Quaternion.identity, cameraView.transform);
                frames[i].gameObject.SetActive(false);
            }
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
            cameraView.texture = tinyYolo.texture;

            var size = ((RectTransform)cameraView.transform).rect.size;
            var results = tinyYolo.GetResults();
            for (int i = 0; i < results.Length; i++)
            {
                UpdateFrame(frames[i], results[i], size);
            }
            isProcessing = false;
        }


        void UpdateFrame(Text frame, TinyYolo.Result result, Vector2 size)
        {
            if (result.confidence < scoreThreshold)
            {
                frame.gameObject.SetActive(false);
                return;
            }
            else
            {
                frame.gameObject.SetActive(true);
            }

            // Debug.Log(result.rect);

            frame.text = $" {(int)(result.confidence * 100)}%";
            var rt = frame.transform as RectTransform;
            rt.anchoredPosition = result.rect.position * size - size * 0.5f;
            rt.sizeDelta = result.rect.size * size;
        }


    }
}