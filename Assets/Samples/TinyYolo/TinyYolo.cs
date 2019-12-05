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
        TensorShape inputShape;

        static readonly float[] ANCHORS = {
            1.08f, 1.19f, 3.42f, 4.41f, 6.63f, 11.38f, 9.42f, 5.11f, 16.62f, 10.52f
        };

        public Texture texture => textureToTensor.resizeTexture;

        public TinyYolo(NNModel nnModel, BarracudaWorkerFactory.Type type)
        {
            bool verbose = false;
            model = ModelLoader.Load(nnModel, verbose);
            Debug.Log(model);
            Debug.Log(model.inputs[0].name);
            worker = BarracudaWorkerFactory.CreateWorker(type, model, verbose);

            inputShape = new TensorShape(model.inputs[0].shape);
            textureToTensor = new TextureToTensor();
            resizeOptions = new TextureToTensor.ResizeOptions()
            {
                width = inputShape.width,
                height = inputShape.height,
                rotationDegree = 0,
                flipX = false,
                flipY = false,
                aspectMode = TextureToTensor.AspectMode.Fill,
            };
            inputAspect = (float)inputShape.width / (float)inputShape.height;

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
            var input = new Tensor(resizedTex, 3, "image");
            worker.AddInput(input);
            yield return worker.ExecuteAsync();
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
            Debug.AssertFormat(output.width == 13, "output width is assumed {0} but {1}", 13, output.width);
            Debug.AssertFormat(output.height == 13, "output height is assumed {0} but {1}", 13, output.height);
            Debug.AssertFormat(output.channels == 125, "output width is assumed {0} but {1}", 125, output.channels);

            const int GRIDS = 13;
            const int STRIDE = 416 / GRIDS;

            for (int y = 0; y < GRIDS; y++)
            {
                for (int x = 0; x < GRIDS; x++)
                {
                    for (int z = 0; z < 5; z++)
                    {
                        int ch = z * (25 + 5);
                        float tx = output[0, y, x, ch];
                        float ty = output[0, y, x, ch + 1];
                        float tw = output[0, y, x, ch + 2];
                        float th = output[0, y, x, ch + 3];
                        float to = output[0, y, x, ch + 4];
                        results[z] = new Result()
                        {
                            confidence = Sigmoid(to),
                            rect = MakeRect(
                                centerX: (x + Sigmoid(tx)) * STRIDE / inputShape.width,
                                centerY: (y + Sigmoid(ty)) * STRIDE / inputShape.height,
                                width: Mathf.Exp(tw) * ANCHORS[2 * z] * STRIDE / inputShape.width,
                                height: Mathf.Exp(th) * ANCHORS[2 * z + 1] * STRIDE / inputShape.height
                            ),
                        };
                    }
                }
            }
        }

        static Rect MakeRect(float centerX, float centerY, float width, float height)
        {
            return new Rect(centerX - width * 0.5f, centerY - height * 0.5f, width, height);
        }
    }
}