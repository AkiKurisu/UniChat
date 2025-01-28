using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniChat.Memory;
using UnityEngine;

namespace UniChat
{
    /// <summary>
    /// Use llm to generate contents
    /// </summary>
    public class LLMGenerator : IGenerator
    {
        private readonly IChatModel _llm;
        
        private readonly ChatMemory _memory;
        
        public LLMGenerator(IChatModel llm, ChatMemory memory)
        {
            _memory = memory;
            _llm = llm;
        }
        
        public async UniTask<bool> Generate(GenerateContext context, CancellationToken ct)
        {
            try
            {
                var llmData = await _llm.GenerateAsync(_memory, ct);
                context.generatedContent = llmData.Response;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
                return false;
            }
        }
    }
}