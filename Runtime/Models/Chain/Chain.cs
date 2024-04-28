using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Kurisu.UniChat.Memory;
namespace Kurisu.UniChat.Chains
{
    public abstract class Chain : IChain
    {
        public IChainInputs Inputs { get; set; }
        private const string RunKey = "__run";
        public abstract string ChainType();
        public abstract IReadOnlyList<string> InputKeys { get; }
        public abstract IReadOnlyList<string> OutputKeys { get; }
        private bool stackTrace;
        private bool applyToContext;
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
            values = values ?? throw new ArgumentNullException(nameof(values));
            var runContext = RunContext.GetContext(values);
            if (applyToContext) runContext.StackTrace |= stackTrace;

            var callBack = await ChainCallback.Configure(
                runContext.RunId,
                callbacks,
                Inputs.Callbacks,
                tags,
                Inputs.Tags,
                metadata,
                Inputs.Metadata,
                stackTrace: stackTrace || runContext.StackTrace
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
        /// Trace this chain to debug status
        /// </summary>
        /// <param name="stackTrace">Enable stack track</param>
        /// <param name="applyToContext">Trace all child chains when run this chain</param>
        /// <returns></returns>
        public Chain Trace(bool stackTrace, bool applyToContext = false)
        {
            this.stackTrace = stackTrace;
            this.applyToContext = applyToContext;
            return this;
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
        /// <summary>
        /// Updates chat history.
        /// </summary>
        /// <param name="history"></param>
        /// <param name="requestKey">The user's request</param>
        /// <param name="responseKey">The model's response</param>
        /// <returns></returns>
        public static UpdateHistoryChain UpdateHistory(
            ChatHistory history,
            string requestKey = "text",
            string responseKey = "text")
        {
            return new UpdateHistoryChain(history, requestKey, responseKey);
        }
        /// <summary>
        /// Converts text to speech using the specified TTS model, will be batched when input is IReadOnlyList<string>
        /// </summary>
        /// <param name="model"></param>
        /// <param name="settings"></param>
        /// <param name="inputKey"></param>
        /// <param name="outputKey"></param>
        /// <returns></returns>
        public static TTSChain TTS(
            ITextToSpeechModel model,
            TextToSpeechSettings settings = null,
            string inputKey = "text",
            string outputKey = "audio")
        {
            return new TTSChain(model, settings, inputKey, outputKey);
        }
        /// <summary>
        /// Converts speech to text using the specified STT model.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="settings"></param>
        /// <param name="inputKey"></param>
        /// <param name="outputKey"></param>
        /// <returns></returns>
        public static STTChain STT(
            ISpeechToTextModel model,
            SpeechToTextSettings settings = null,
            string inputKey = "audio",
            string outputKey = "text")
        {
            return new STTChain(model, settings, inputKey, outputKey);
        }
        /// <summary>
        /// Split text
        /// </summary>
        /// <param name="splitter"></param>
        /// <param name="inputKey"></param>
        /// <param name="outputKey"></param>
        /// <returns></returns>
        public static SplitChain Split(
            ISplitter splitter,
            string inputKey = "text",
            string outputKey = "text")
        {
            return new SplitChain(splitter, inputKey, outputKey);
        }
        /// <summary>
        /// Translate text, will be batched when input is IReadOnlyList<string>
        /// </summary>
        /// <param name="translator"></param>
        /// <param name="inputKey"></param>
        /// <param name="outputKey"></param>
        /// <returns></returns>
        public static TranslateChain Translate(
            ITranslator translator,
            string inputKey = "text",
            string outputKey = "text")
        {
            return new TranslateChain(translator, inputKey, outputKey);
        }
        /// <summary>
        /// Loads chat memory.
        /// Usually used before a model to get the context of the conversation.
        /// </summary>
        /// <param name="memory"></param>
        /// <param name="outputKey"></param>
        public static LoadMemoryChain LoadMemory(
            ChatMemory memory,
            string outputKey = "text")
        {
            return new LoadMemoryChain(memory, outputKey);
        }
        /// <summary>
        /// Uses ReAct technique to allow LLM to execute functions.
        /// <see cref="ReActAgentExecutorChain.UseTool"/> to add tools.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="reActPrompt"></param>
        /// <param name="maxActions"></param>
        /// <param name="inputKey"></param>
        /// <param name="outputKey"></param>
        public static ReActAgentExecutorChain ReActAgentExecutor(
            ILargeLanguageModel model,
            string reactPrompt = null,
            int maxActions = 5,
            string inputKey = "text",
            string outputKey = "text")
        {
            return new ReActAgentExecutorChain(model, reactPrompt, maxActions, inputKey, outputKey);
        }
        /// <summary>
        /// Parses the output of LLM model as it would be a ReAct output.
        /// Can be used with custom ReAct prompts.
        /// </summary>
        /// <param name="inputKey"></param>
        /// <param name="outputKey"></param>
        /// <returns></returns>
        public static ReActParserChain ReActParser(
            string inputKey = "text",
            string outputKey = "text")
        {
            return new ReActParserChain(inputKey, outputKey);
        }
    }
}