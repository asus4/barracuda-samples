using System;
using System.Collections;
using UnityEngine;
using Barracuda;

namespace BarracudaSample
{
    public class Mnist : IDisposable
    {
        IWorker worker;
        Model model;
        float[] results;
        PrecompiledComputeOps ops;


        public Mnist(NNModel nnModel, BarracudaWorkerFactory.Type type)
        {
            bool verbose = true;
            model = ModelLoader.Load(nnModel, verbose);
            worker = BarracudaWorkerFactory.CreateWorker(type, model, verbose);

            var kernels = ComputeShaderSingleton.Instance.kernels;
            ops = new PrecompiledComputeOps(kernels, kernels[0]);
        }

        public void Dispose()
        {
            worker?.Dispose();
            model = null;
            ops = null;
        }

        public IEnumerator ExecuteAsync(Texture inputTex)
        {
            Tensor input = new Tensor(inputTex, 1);
            yield return worker.ExecuteAsync(input);
            Tensor output1 = worker.Peek();
            Tensor output2 = ops.Softmax(output1);
            results = output2.data.Download(10);

            input.Dispose();
            output1.Dispose();
            output2.Dispose();
        }

        public float[] GetResult()
        {
            return results;
        }

        public TensorShape GetInputShape(int index)
        {
            return new TensorShape(model.inputs[index].shape);
        }
    }
}
