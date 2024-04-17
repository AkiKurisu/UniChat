using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;
namespace Kurisu.UniChat.Chains
{
    public abstract class StackableChain : IChain
    {
        public string Name { get; set; } = string.Empty;
        public virtual IReadOnlyList<string> InputKeys { get; protected set; } = Array.Empty<string>();
        public virtual IReadOnlyList<string> OutputKeys { get; protected set; } = Array.Empty<string>();
        protected StackableChain() { }
        private bool stackTrack;
        protected StackableChain(StackableChain lastChild)
        {
            lastChild = lastChild ?? throw new ArgumentNullException(nameof(lastChild));

            Name = lastChild.Name;
            InputKeys = lastChild.InputKeys;
            OutputKeys = lastChild.OutputKeys;
        }
        protected string GenerateName()
        {
            return GetType().Name;
        }
        public string GetInputs()
        {
            return string.Join(",", InputKeys);
        }
        public string GetOutputs()
        {
            return string.Join(",", OutputKeys);
        }
        private string FormatInputValues(IChainValues values)
        {
            List<string> res = new();
            foreach (var key in InputKeys)
            {
                if (!values.Value.ContainsKey(key))
                {
                    res.Add($"{key} is expected but missing");
                    continue;
                };
                res.Add($"{key}={values.Value[key]}");
            }
            return string.Join(",\n", res);
        }

        public async UniTask<IChainValues> CallAsync(IChainValues values, ICallbacks callbacks = null,
        IReadOnlyList<string> tags = null, IReadOnlyDictionary<string, object> metadata = null)
        {
            values = values ?? throw new ArgumentNullException(nameof(values));

            CallbackManagerForChainRun runManager = null;
            if (stackTrack)
            {
                var callBack = await ChainCallback.Configure(
                    callbacks,
                        null,
                    tags,
                    null,
                    metadata,
                    null
                );
                runManager = await callBack.HandleChainStart(this, values);
            }
            try
            {
                var result = await InternalCall(values);
                _hook?.Invoke(values);
                if (runManager != null) await runManager.HandleChainEndAsync(values, result);
                return result;
            }
            catch (StackableChainException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var name = string.IsNullOrWhiteSpace(Name)
                    ? GenerateName()
                    : Name;
                var inputValues = FormatInputValues(values);
                var message = $"Error occured in {name} with inputs \n{inputValues}\n.";
                if (runManager != null) await runManager.HandleChainErrorAsync(ex, values);
                throw new StackableChainException(message, ex);
            }

        }

        protected abstract UniTask<IChainValues> InternalCall(IChainValues values);

        public static StackChain operator |(StackableChain a, StackableChain b)
        {
            return new StackChain(a, b);
        }

        public static StackChain BitwiseOr(StackableChain left, StackableChain right)
        {
            return left | right;
        }

        public async UniTask<IChainValues> Run(StackableChainHook hook = null)
        {
            var values = new StackableChainValues() { Hook = hook };
            hook?.ChainStart(values);
            var res = await CallAsync(values);
            return res;
        }

        public async UniTask<string> Run(
            string resultKey,
            StackableChainHook hook = null)
        {
            var values = await CallAsync(new StackableChainValues
            {
                Hook = hook,
            });

            return values.Value[resultKey].ToString();
        }

        public async UniTask<T> Run<T>(
            string resultKey,
            StackableChainHook hook = null)
        {
            var values = await CallAsync(new StackableChainValues
            {
                Hook = hook,
            });

            return (T)values.Value[resultKey];
        }
        public async UniTask<(T1, T2)> Run<T1, T2>(
            string resultKey1,
            string resultKey2,
            StackableChainHook hook = null)
        {
            var values = await CallAsync(new StackableChainValues
            {
                Hook = hook,
            });

            return ((T1)values.Value[resultKey1], (T2)values.Value[resultKey2]);
        }
        public async UniTask<(T1, T2, T3)> Run<T1, T2, T3>(
            string resultKey1,
            string resultKey2,
            string resultKey3,
            StackableChainHook hook = null)
        {
            var values = await CallAsync(new StackableChainValues
            {
                Hook = hook,
            });
            return ((T1)values.Value[resultKey1], (T2)values.Value[resultKey2], (T3)values.Value[resultKey3]);
        }


        public UniTask<string> Run(string resultKey)
        {
            return Run(resultKey, null);
        }

        public async UniTask<string> Run(
            Dictionary<string, object> input,
            ICallbacks callbacks = null)
        {
            var res = await CallAsync(new ChainValues(input));

            return res.Value[OutputKeys[0]].ToString() ?? string.Empty;
        }

        private Action<IChainValues> _hook;
        public StackableChain SetHook(Action<IChainValues> hook)
        {
            _hook = hook;
            return this;
        }
        /// <summary>
        /// Track this chain to debug status
        /// </summary>
        /// <param name="stackTrack"></param>
        /// <returns></returns>
        public StackableChain Track(bool stackTrack)
        {
            this.stackTrack = stackTrack;
            return this;
        }
    }
    public class StackChain : StackableChain
    {
        public StackableChain Left { get; set; }
        public StackableChain Right { get; set; }
        public StackChain(StackableChain left, StackableChain right) : base(right)
        {
            Left = left;
            Right = right;
        }
        public IReadOnlyList<string> IsolatedInputKeys { get; set; } = Array.Empty<string>();

        public IReadOnlyList<string> IsolatedOutputKeys { get; set; } = Array.Empty<string>();

        public StackChain AsIsolated(
            string[] inputKeys = null,
            string[] outputKeys = null)
        {
            IsolatedInputKeys = inputKeys ?? IsolatedInputKeys;
            IsolatedOutputKeys = outputKeys ?? IsolatedOutputKeys;
            return this;
        }
        public StackChain AsIsolated(
            string inputKey = null,
            string outputKey = null)
        {
            if (inputKey != null) IsolatedInputKeys = new[] { inputKey };
            if (outputKey != null) IsolatedOutputKeys = new[] { outputKey };

            return this;
        }
        protected override async UniTask<IChainValues> InternalCall(IChainValues values)
        {
            Assert.IsNotNull(values);
            var stackableChainValues = values as StackableChainValues;
            var hook = stackableChainValues?.Hook;
            var originalValues = values;

            if (IsolatedInputKeys.Count > 0)
            {
                var res = new StackableChainValues
                {
                    Hook = hook,
                };
                foreach (var key in IsolatedInputKeys)
                {
                    res.Value[key] = values.Value[key];
                }
                values = res;
            }

            if (Left is not StackChain &&
                stackableChainValues != null)
            {
                hook?.LinkEnter(Left, stackableChainValues);
            }

            await Left.CallAsync(values);

            if (Left is not StackChain &&
                stackableChainValues != null)
            {
                hook?.LinkExit(Left, stackableChainValues);
            }
            if (Right is not StackChain &&
                stackableChainValues != null)
            {
                hook?.LinkEnter(Right, stackableChainValues);
            }

            await Right.CallAsync(values);

            if (Right is not StackChain &&
                stackableChainValues != null)
            {
                hook?.LinkExit(Right, stackableChainValues);
            }

            if (IsolatedOutputKeys.Count > 0)
            {
                foreach (var key in IsolatedOutputKeys)
                {
                    originalValues.Value[key] = values.Value[key];
                }
            }

            return originalValues;
        }

        public static StackChain operator >(StackChain a, StackableChain b)
        {
            a = a ?? throw new ArgumentNullException(nameof(a));

            return a.AsIsolated(outputKey: a.OutputKeys[^1]) | b;
        }

        public static StackChain operator <(StackChain left, StackableChain right)
        {
            return left > right;
        }
    }
}