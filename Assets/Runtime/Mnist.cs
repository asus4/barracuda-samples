using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Mnist : MonoBehaviour
{
    [SerializeField] string mnistPath = "classify_mnist_graph_def.nn";
    [SerializeField] RawImage inputImageView = null;
    [SerializeField] Text outputTextView = null;
    [SerializeField] Barracuda.BarracudaWorkerFactory.Type workerType = Barracuda.BarracudaWorkerFactory.Type.ComputePrecompiled;

    Barracuda.IWorker worker;
    RenderTexture inputTex;

    Mesh lineMesh;
    Material lineMaterial;
    Texture2D clearTexture;
    System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

    bool isProcessing = false;


    #region Life Cycle
    void Start()
    {
        // Init model
        var model = Barracuda.ModelLoader.LoadFromStreamingAssets(mnistPath, true);
        Debug.Log(model);

        worker = Barracuda.BarracudaWorkerFactory.CreateWorker(workerType, model, false);

        // Init texture 28 x 28, float
        var shape = new Barracuda.TensorShape(model.inputs[0].shape);
        inputTex = new RenderTexture(shape.width, shape.height, 0, RenderTextureFormat.R8);
        inputTex.enableRandomWrite = true;
        inputTex.filterMode = FilterMode.Bilinear;

        inputImageView.texture = inputTex;
        clearTexture = Texture2D.blackTexture;

        // Mesh
        lineMesh = new Mesh();
        lineMesh.MarkDynamic();
        lineMesh.vertices = new Vector3[2];
        lineMesh.SetIndices(new[] { 0, 1 }, MeshTopology.Lines, 0);

        lineMaterial = new Material(Shader.Find("Hidden/LineShader"));
        lineMaterial.SetColor("_Color", Color.white);

        ClearInput();
    }

    void OnDestroy()
    {
        worker?.Dispose();
        inputTex?.Release();

        Destroy(lineMesh);
        Destroy(lineMaterial);
    }

    #endregion // Life Cycle

    #region UI Events

    public void Execute()
    {
        StartCoroutine(ExecuteAsync());
    }

    public void ClearInput()
    {
        Graphics.Blit(clearTexture, inputTex);
    }

    public void OnDrag(BaseEventData baseData)
    {
        var data = (PointerEventData)baseData;
        data.Use();

        var area = data.pointerDrag.GetComponent<RectTransform>();
        var p0 = area.InverseTransformPoint(data.position - data.delta);
        var p1 = area.InverseTransformPoint(data.position);

        var scale = new Vector3(2 / area.rect.width, -2 / area.rect.height, 0);
        p0 = Vector3.Scale(p0, scale);
        p1 = Vector3.Scale(p1, scale);

        DrawLine(p0, p1);

        if (!isProcessing)
        {
            StartCoroutine(ExecuteAsync());
        }
    }

    #endregion // UI Events

    #region Private

    void DrawLine(Vector3 p0, Vector3 p1)
    {
        var prevRT = RenderTexture.active;
        RenderTexture.active = inputTex;

        lineMesh.SetVertices(new List<Vector3>() { p0, p1 });
        lineMaterial.SetPass(0);
        Graphics.DrawMeshNow(lineMesh, Matrix4x4.identity);

        RenderTexture.active = prevRT;
    }

    IEnumerator ExecuteAsync()
    {
        if (isProcessing)
        {
            Debug.LogWarning("now processing");
            yield break;
        }

        isProcessing = true;

        stopwatch.Restart();
        var tensor = new Barracuda.Tensor(inputTex, 1);
        yield return worker.ExecuteAsync(tensor);
        stopwatch.Stop();

        var output = worker.Peek();
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < 10; i++)
        {
            sb.Append($"{i}: {output[i]:0.00}\n");
        }
        sb.AppendLine();
        sb.AppendLine($"execute time: {stopwatch.ElapsedMilliseconds} msec");

        outputTextView.text = sb.ToString();

        output.Dispose();

        isProcessing = false;
    }
    #endregion // Private

}

