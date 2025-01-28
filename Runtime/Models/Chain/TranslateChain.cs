using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
namespace UniChat.Chains
{
    public class TranslateChain : StackableChain
    {
        private readonly ITranslator translator;
        private bool useCache;
        private TextCache textCache;
        public TranslateChain(
            ITranslator translator,
            string inputKey = "text",
            string outputKey = "translated_text"
            )
        {
            InputKeys = new[] { inputKey };
            OutputKeys = new[] { outputKey };
            this.translator = translator;
        }

        protected override async UniTask<IChainValues> InternalCall(IChainValues values)
        {
            values = values ?? throw new ArgumentNullException(nameof(values));
            var input = values.Value[InputKeys[0]];
            if (input is string stringValue)
            {
                var hash = XXHash.CalculateHash(stringValue);
                string response;
                if (useCache && textCache.Contains(hash))
                {
                    response = (await textCache.Load(hash))[0];
                }
                else
                {
                    response = await translator.TranslateAsync(stringValue, default);
                    if (useCache)
                    {
                        textCache.Save(hash, response);
                    }
                }
                values.Value[OutputKeys[0]] = response;
                return values;
            }
            if (input is IReadOnlyList<string> segments)
            {
                var hash = XXHash.CalculateHash(segments[0]);
                string[] responses;
                //Batch
                if (useCache && textCache.Contains(hash))
                {
                    responses = await textCache.Load(hash);
                }
                else
                {
                    responses = await UniTask.WhenAll(segments.Select(x => translator.TranslateAsync(x, default)));
                    if (useCache)
                    {
                        textCache.Save(hash, responses);
                    }
                }
                values.Value[OutputKeys[0]] = responses;
                return values;
            }
            throw new ArgumentException(nameof(input));
        }
        public TranslateChain UseCache(TextCache textCache)
        {
            this.textCache = textCache;
            useCache = textCache != null;
            return this;
        }
    }
}