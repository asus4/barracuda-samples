using System;
using System.Collections;
using UnityEngine;
using Barracuda;

using static MathExtension;

namespace BarracudaSample
{
    public class TinyYolo : IDisposable
    {
        [Serializable]
        public struct Result
        {
            public float confidence;
            public Rect rect;
        }

        Model model;
        IWorker worker;
        TextureToTensor textureToTensor;
        TextureToTensor.ResizeOptions resizeOptions;
        float inputAspect;
        Result[] results;

        public TinyYolo(NNModel nnModel, BarracudaWorkerFactory.Type type)
        {
            bool verbose = false;
            model = ModelLoader.Load(nnModel, verbose);
            worker = BarracudaWorkerFactory.CreateWorker(type, model, verbose);

            var shape = new TensorShape(model.inputs[0].shape);
            textureToTensor = new TextureToTensor();
            resizeOptions = new TextureToTensor.ResizeOptions()
            {
                width = shape.width,
                height = shape.height,
                rotationDegree = 0,
                flipX = false,
                flipY = false,
                aspectMode = TextureToTensor.AspectMode.Fill,
            };
            inputAspect = (float)shape.width / (float)shape.height;

            results = new Result[25];
        }

        public void Dispose()
        {
            worker?.Dispose();
            model = null;
            textureToTensor?.Dispose();
        }

        public IEnumerator ExecuteAsync(Texture inputTex)
        {
            var resizedTex = textureToTensor.Resize(inputTex, resizeOptions);
            var input = new Tensor(resizedTex, 3);
            yield return worker.ExecuteAsync(input);
            var output = worker.Peek();

            GetResults(output, results);

            input.Dispose();
            output.Dispose();
        }

        public Rect GetUVRect(float srcAspect)
        {
            return TextureToTensor.GetUVRect(srcAspect, inputAspect, resizeOptions.aspectMode);
        }

        public Result[] GetResults()
        {
            return results;
        }

        void GetResults(Tensor output, Result[] results)
        {
            int rows = output.height; // 13
            int cols = output.width; // 13
            int labels = output.channels / 5; // 125 / 5 = 25 labels
            const int stride = 32; // 416 / 13

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    for (int label = 0; label < labels; label++)
                    {
                        float tx = output[0, y, x, label * 5];
                        float ty = output[0, y, x, label * 5 + 1];
                        float tw = output[0, y, x, label * 5 + 2];
                        float th = output[0, y, x, label * 5 + 3];
                        float tc = output[0, y, x, label * 5 + 4];
                        results[label] = new Result()
                        {
                            confidence = Sigmoid(tc),
                            rect = new Rect(
                                x: (x + Sigmoid(tx)) * stride,
                                y: (y + Sigmoid(ty)) * stride,
                                width: Mathf.Exp(tw) * stride,
                                height: Mathf.Exp(th) * stride
                            ),
                        };
                    }
                }
            }
        }
    }
}