using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Kurisu.UniChat.LLMs;
namespace Kurisu.UniChat.Chains
{
    public class PromptChain : StackableChain
    {
        private readonly PromptTemplate promptTemplate;
        public PromptChain(string template, string outputKey = "prompt")
        {
            OutputKeys = new[] { outputKey };
            promptTemplate = new(template);
            InputKeys = promptTemplate.GetVariables().ToArray();
        }

        protected override UniTask<IChainValues> InternalCall(IChainValues values)
        {
            values = values ?? throw new ArgumentNullException(nameof(values));

            // validate that input keys containing all variables
            var valueKeys = values.Value.Keys;
            var missing = InputKeys.Except(valueKeys);
            if (missing.Any())
            {
                throw new InvalidOperationException($"Input keys must contain all variables in template. Missing: {string.Join(",", missing)}");
            }

            var formattedPrompt = promptTemplate.Format(values.Value);

            values.Value[OutputKeys[0]] = formattedPrompt;

            return UniTask.FromResult(values);
        }
    }
}