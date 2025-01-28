using System;
using System.Collections.Generic;
using Unity.Sentis;
namespace UniChat.NLP
{
    public class BertEncoder : IEncoder, IDisposable
    {
        public enum PoolingType
        {
            MeanPooling,
            CLSPooling
        }
        
        private readonly BertTokenizer _tokenizer;
        
        private readonly IWorker _worker;
        
        private const string LastHiddenState = "last_hidden_state";
        
        private const string AttentionMask = "attention_mask";
        
        public PoolingType Pooling { get; set; } = PoolingType.MeanPooling;
        
        public BertEncoder(Model model, BertTokenizer tokenizer, BackendType backendType = BackendType.GPUCompute)
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
            if (Pooling == PoolingType.MeanPooling)
                return Encode_Mean_Pooling(ops, input, true);
            return Encode_CLS_Pooling(ops, input, true);
        }
        
        public TensorFloat Encode_Mean_Pooling(Ops ops, IReadOnlyList<string> input, bool normalized)
        {
            var inputSentencesTokensTensor = _tokenizer.Tokenize(input);
            var outputTensor = PeekOutput_Imp(inputSentencesTokensTensor);
            var meanPooledTensor = ops.MeanPooling(inputSentencesTokensTensor[AttentionMask], outputTensor);
            return normalized ? ops.L2Norm(meanPooledTensor) : meanPooledTensor;
        }
        
        private readonly int[] _start = { 0, 0, 0 };
        
        private readonly int[] _end = { 1, 1, 1 };
        
        public TensorFloat Encode_CLS_Pooling(Ops ops, IReadOnlyList<string> input, bool normalized)
        {
            Dictionary<string, Tensor> inputSentencesTokensTensor = _tokenizer.Tokenize(input);
            TensorFloat outputTensor = PeekOutput_Imp(inputSentencesTokensTensor);
            _end[2] = outputTensor.shape[2];
            // Perform pooling. In this case, cls pooling.
            // (batch,sequence_length,embedding_length) => (batch,1,embedding_length)
            var clsPooledTensor = ops.Slice(outputTensor, new ReadOnlySpan<int>(_start), new ReadOnlySpan<int>(_end), null, null) ?? throw new ArgumentNullException("ops.Slice(outputTensor, new ReadOnlySpan<int>(_start), new ReadOnlySpan<int>(_end), null, null)");
            clsPooledTensor = ops.Reshape(clsPooledTensor, new TensorShape(outputTensor.shape[0], outputTensor.shape[2]));
            return normalized ? ops.L2Norm(clsPooledTensor) : clsPooledTensor;
        }
        
        public TensorFloat PeekOutput(IReadOnlyList<string> input)
        {
            return PeekOutput_Imp( _tokenizer.Tokenize(input));
        }
        
        private TensorFloat PeekOutput_Imp(Dictionary<string, Tensor> inputSentencesTokensTensor)
        {
            _worker.Execute(inputSentencesTokensTensor);
            return _worker.PeekOutput(LastHiddenState) as TensorFloat;
        }
    }
}