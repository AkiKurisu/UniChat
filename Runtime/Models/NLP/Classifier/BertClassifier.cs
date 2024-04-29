using System;
using System.Collections.Generic;
using Unity.Sentis;
namespace Kurisu.UniChat.NLP
{
    public class BertClassifier : IClassifier, IDisposable
    {
        private readonly IWorker worker;
        private readonly BertTokenizer tokenizer;
        public BertClassifier(Model model, BertTokenizer tokenizer, BackendType backendType = BackendType.GPUCompute)
        {
            this.tokenizer = tokenizer;
            worker = WorkerFactory.CreateWorker(backendType, model);
        }
        public void Dispose()
        {
            worker.Dispose();
        }
        public TensorFloat Encode(Ops ops, IReadOnlyList<string> input)
        {
            Dictionary<string, Tensor> inputSentencesTokensTensor = tokenizer.Tokenize(input);
            worker.Execute(inputSentencesTokensTensor);
            TensorFloat outputTensor = ops.Softmax(worker.PeekOutput("logits") as TensorFloat);
            return outputTensor;
        }
        public (TensorFloat, TensorInt) Classify(Ops ops, IReadOnlyList<string> inputs)
        {
            var inputTensor = Encode(ops, inputs);
            TensorInt ids = ops.ArgMax(inputTensor, 1, true);
            return (inputTensor, ids);
        }
    }
    public class OutputClassifier : IDisposable
    {
        private readonly IWorker worker;
        public OutputClassifier(Model model, BackendType backendType = BackendType.GPUCompute)
        {
            worker = WorkerFactory.CreateWorker(backendType, model);
        }
        public void Dispose()
        {
            worker.Dispose();
        }
        public TensorFloat Classify(Ops ops, TensorFloat inputTensor)
        {
            worker.Execute(inputTensor);
            TensorFloat outputTensor = ops.Softmax(worker.PeekOutput() as TensorFloat);
            return outputTensor;
        }
    }
}