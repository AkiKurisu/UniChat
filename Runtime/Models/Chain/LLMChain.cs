using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
namespace Kurisu.UniChat.Chains
{
    public class LLMChain : StackableChain
    {
        private readonly ILargeLanguageModel _llm;
        private bool verbose;
        public LLMChain(
            ILargeLanguageModel llm,
            string inputKey = "prompt",
            string outputKey = "text"
            )
        {
            InputKeys = new[] { inputKey };
            OutputKeys = new[] { outputKey };
            _llm = llm;
        }

        protected override async UniTask<IChainValues> InternalCall(IChainValues values)
        {
            values = values ?? throw new ArgumentNullException(nameof(values));

            var promptObject = values.Value[InputKeys[0]];
            ILLMResponse response;
            if (promptObject is string prompt)
            {
                if (verbose) Debug.Log($"LLM request: {prompt}");
                response = await _llm.GenerateAsync(prompt, default);
                values.Value[OutputKeys[0]] = response.Response;
            }
            else
            {
                throw new ArgumentException(nameof(promptObject));
            }

            if (verbose) Debug.Log($"LLM response: {response.Response}");
            return values;
        }
        public LLMChain Verbose(bool verbose)
        {
            this.verbose = verbose;
            return this;
        }
    }
}