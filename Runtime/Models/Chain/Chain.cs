using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
namespace Kurisu.UniChat.Chains
{
    public abstract class Chain : IChain
    {
        public IChainInputs Inputs { get; set; }
        private const string RunKey = "__run";
        public abstract string ChainType();
        public abstract IReadOnlyList<string> InputKeys { get; }
        public abstract IReadOnlyList<string> OutputKeys { get; }
        public Chain(IChainInputs inputs)
        {
            Inputs = inputs;
        }
        /// <summary>
        /// Run the chain using a simple input/output.
        /// </summary>
        /// <param name="input">The string input to use to execute the chain.</param>
        /// <returns>A text value containing the result of the chain.</returns>
        /// <exception cref="ArgumentException">If the type of chain used expects multiple inputs, this method will throw an ArgumentException.</exception>
        public virtual async UniTask<string> Run(string input)
        {
            var isKeylessInput = InputKeys.Count <= 1;

            if (!isKeylessInput)
            {
                throw new ArgumentException($"Chain {ChainType()} expects multiple inputs, cannot use 'run'");
            }

            var values = InputKeys.Count > 0 ? new ChainValues(InputKeys[0], input) : new ChainValues();
            var returnValues = await CallAsync(values);
            var keys = returnValues.Value.Keys;

            if (keys.Count(p => p != RunKey) == 1)
            {
                var returnValue = returnValues.Value.FirstOrDefault(p => p.Key != RunKey).Value;

                return returnValue?.ToString();
            }

            throw new InvalidOperationException("Return values have multiple keys, 'run' only supported when one key currently");
        }

        /// <summary>
        /// Run the chain using a simple input/output.
        /// </summary>
        /// <param name="input">The dict input to use to execute the chain.</param>
        /// <param name="callbacks">
        /// Callbacks to use for this chain run. These will be called in
        /// addition to callbacks passed to the chain during construction, but only
        /// these runtime callbacks will propagate to calls to other objects.
        /// </param>
        /// <returns>A text value containing the result of the chain.</returns>
        public virtual async UniTask<string> Run(Dictionary<string, object> input, ICallbacks callbacks = null)
        {
            input = input ?? throw new ArgumentNullException(nameof(input));

            var keysLengthDifferent = InputKeys.Count != input.Count;

            if (keysLengthDifferent)
            {
                throw new ArgumentException($"Chain {ChainType()} expects {InputKeys.Count} but, received {input.Count}");
            }

            var returnValues = await CallAsync(new ChainValues(input), callbacks);

            var returnValue = returnValues.Value.FirstOrDefault(kv => kv.Key == OutputKeys[0]).Value;

            return returnValue?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Execute the chain, using the values provided.
        /// </summary>
        /// <param name="values">The <see cref="ChainValues"/> to use.</param>
        /// <param name="callbacks"></param>
        /// <param name="tags"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        public async UniTask<IChainValues> CallAsync(
            IChainValues values,
            ICallbacks callbacks = null,
            IReadOnlyList<string> tags = null,
            IReadOnlyDictionary<string, object> metadata = null
        )
        {
            var callBack = await ChainCallback.Configure(
                callbacks,
                Inputs.Callbacks,
                tags,
                Inputs.Tags,
                metadata,
                Inputs.Metadata
            );

            var runManager = await callBack.HandleChainStart(this, values);

            try
            {
                var result = await CallAsync(values, runManager);

                await runManager.HandleChainEndAsync(values, result);

                return result;
            }
            catch (Exception e)
            {
                await runManager.HandleChainErrorAsync(e, values);
                throw;
            }
        }

        /// <summary>
        /// Execute the chain, using the values provided.
        /// </summary>
        /// <param name="values">The <see cref="ChainValues"/> to use.</param>
        /// <param name="runManager"></param>
        /// <returns></returns>
        protected abstract UniTask<IChainValues> CallAsync(IChainValues values, CallbackManagerForChainRun runManager);

        /// <summary>
        /// Call the chain on all inputs in the list.
        /// </summary>
        public virtual async UniTask<List<IChainValues>> ApplyAsync(IReadOnlyList<ChainValues> inputs)
        {
            var tasks = inputs.Select(input => CallAsync(input));
            var results = await UniTask.WhenAll(tasks);
            return results.ToList();
        }
        /// <summary>
        /// Replaces context and question in the prompt with their values.
        /// </summary>
        /// <param name="template"></param>
        /// <param name="outputKey"></param>
        /// <returns></returns>
        public static PromptChain Template(
            string template,
            string outputKey = "text")
        {
            return new PromptChain(template, outputKey);
        }

        /// <summary>
        /// Sets the value to the chain context.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="outputKey"></param>
        /// <returns></returns>
        public static SetChain Set(
            object value,
            string outputKey = "text")
        {
            return new SetChain(value, outputKey);
        }

        /// <summary>
        /// Sets the value returned by lambda to the chain context.
        /// </summary>
        /// <param name="valueGetter"></param>
        /// <param name="outputKey"></param>
        /// <returns></returns>
        public static SetLambdaChain Set(
            Func<string> valueGetter,
            string outputKey = "text")
        {
            return new SetLambdaChain(valueGetter, outputKey);
        }

        /// <summary>
        /// Executes the lambda function on the chain context.
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public static DoChain Do(
            Action<Dictionary<string, object>> func)
        {
            return new DoChain(func);
        }

        /// <summary>
        /// Executes the LLM model on the chain context.
        /// </summary>
        /// <param name="llm"></param>
        /// <param name="inputKey"></param>
        /// <param name="outputKey"></param>
        /// <returns></returns>
        public static LLMChain LLM(
            ILargeLanguageModel llm,
            string inputKey = "text",
            string outputKey = "text")
        {
            return new LLMChain(llm, inputKey, outputKey);
        }
        public static UpdateHistoryChain UpdateHistory(
            ChatHistory history,
            string requestKey = "text",
            string responseKey = "text")
        {
            return new UpdateHistoryChain(history, requestKey, responseKey);
        }

        public static TTSChain TTS(
            ITextToSpeechModel model,
            TextToSpeechSettings settings = null,
            string inputKey = "text",
            string outputKey = "audio")
        {
            return new TTSChain(model, settings, inputKey, outputKey);
        }
        public static SplitChain Split(
            ISplitter splitter,
            string inputKey = "text",
            string outputKey = "splitted_text")
        {
            return new SplitChain(splitter, inputKey, outputKey);
        }
        public static TranslateChain Translate(
            ITranslator translator,
            string inputKey = "text",
            string outputKey = "translated_text")
        {
            return new TranslateChain(translator, inputKey, outputKey);
        }

        // public static ReActAgentExecutorChain ReActAgentExecutor(
        //     IChatModel model,
        //     string? reActPrompt = null,
        //     int maxActions = 5,
        //     string inputKey = "text",
        //     string outputKey = "text")
        // {
        //     return new ReActAgentExecutorChain(model, reActPrompt, maxActions, inputKey, outputKey);
        // }


        // public static ReActParserChain ReActParser(
        //     string inputKey = "text",
        //     string outputKey = "text")
        // {
        //     return new ReActParserChain(inputKey, outputKey);
        // }

    }

    public class CallbackManagerForChainRun : ParentRunManager, IChainRunner<CallbackManagerForChainRun>
    {
        public CallbackManagerForChainRun()
        {

        }

        public CallbackManagerForChainRun(
            string runId,
            List<CallbackHandler> handlers,
            List<CallbackHandler> inheritableHandlers,
            string parentRunId = null)
            : base(runId, handlers, inheritableHandlers, parentRunId)
        {
        }

        public async UniTask HandleChainEndAsync(IChainValues input, IChainValues output)
        {
            input = input ?? throw new ArgumentNullException(nameof(input));
            output = output ?? throw new ArgumentNullException(nameof(output));
            foreach (var handler in Handlers)
            {
                try
                {
                    await handler.HandleChainEndAsync(
                        input.Value,
                        output.Value,
                        RunId,
                        ParentRunId);
                }
                catch (Exception ex)
                {
                    await Console.Error.WriteLineAsync($"Error in handler {handler.GetType().Name}, HandleChainEnd: {ex}");
                }
            }
        }

        public async UniTask HandleChainErrorAsync(Exception error, IChainValues input)
        {
            input = input ?? throw new ArgumentNullException(nameof(input));

            foreach (var handler in Handlers)
            {
                try
                {
                    await handler.HandleChainErrorAsync(error, RunId, input.Value, ParentRunId);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in handler {handler.GetType().Name}, HandleChainError: {ex}");
                }
            }
        }

        public async UniTask HandleTextAsync(string text)
        {
            foreach (var handler in Handlers)
            {
                try
                {
                    await handler.HandleTextAsync(text, RunId, ParentRunId);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in handler {handler.GetType().Name}, HandleText: {ex}");
                }
            }
        }
    }
}