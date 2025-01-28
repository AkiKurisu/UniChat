using System;
using System.Collections.Generic;
using Unity.Sentis;
namespace UniChat.NLP
{
    public class BertClassifier : IClassifier, IDisposable
    {
        private readonly IWorker _worker;
        
        private readonly BertTokenizer _tokenizer;
        
        public BertClassifier(Model model, BertTokenizer tokenizer, BackendType backendType = BackendType.GPUCompute)
        {
            _tokenizer = tokenizer;
            _worker = WorkerFactory.CreateWorker(backendType, model);
        }
        
        public void Dispose()
        {
            _worker.Dispose();
        }
        
        public TensorFloat Encode(Ops ops, IReadOnlyList<string> input)
        {
            Dictionary<string, Tensor> inputSentencesTokensTensor = _tokenizer.Tokenize(input);
            _worker.Execute(inputSentencesTokensTensor);
            TensorFloat outputTensor = ops.Softmax(_worker.PeekOutput("logits") as TensorFloat);
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
        private readonly IWorker _worker;
        
        public OutputClassifier(Model model, BackendType backendType = BackendType.GPUCompute)
        {
            _worker = WorkerFactory.CreateWorker(backendType, model);
        }
        
        public void Dispose()
        {
            _worker.Dispose();
        }
        
        public TensorFloat Classify(Ops ops, TensorFloat inputTensor)
        {
            _worker.Execute(inputTensor);
            TensorFloat outputTensor = ops.Softmax(_worker.PeekOutput() as TensorFloat);
            return outputTensor;
        }
    }
}