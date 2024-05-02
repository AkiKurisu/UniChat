using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Kurisu.UniChat.Memory;
using UnityEngine;
namespace Kurisu.UniChat
{
    /// <summary>
    /// Use llm to generate contents
    /// </summary>
    public class LLMGenerator : IGenerator
    {
        private readonly IChatModel llm;
        private readonly ChatMemory memory;
        public LLMGenerator(IChatModel llm, ChatMemory memory) : base()
        {
            this.memory = memory;
            this.llm = llm;
        }
        public async UniTask<bool> Generate(GenerateContext context, CancellationToken ct)
        {
            try
            {
                var llmData = await llm.GenerateAsync(memory, ct);
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