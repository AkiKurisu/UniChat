using System;
using System.Threading;
using Cysharp.Threading.Tasks;
namespace Kurisu.UniChat
{
    /// <summary>
    /// Generate contents from external input
    /// </summary>
    public class InputGenerator : IGenerator
    {
        public Func<GenerateContext, UniTaskCompletionSource<bool>> onListenInput;
        public InputGenerator(Func<GenerateContext, UniTaskCompletionSource<bool>> onListenInput)
        {
            this.onListenInput = onListenInput;
        }
        public InputGenerator() { }
        public async UniTask<bool> Generate(GenerateContext context, CancellationToken _)
        {
            return await onListenInput(context).Task;
        }
    }
}