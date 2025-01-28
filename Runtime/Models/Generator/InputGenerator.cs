using System;
using System.Threading;
using Cysharp.Threading.Tasks;
namespace UniChat
{
    /// <summary>
    /// Generate contents from external input
    /// </summary>
    public class InputGenerator : IGenerator
    {
        private readonly Func<GenerateContext, UniTaskCompletionSource<bool>> _onListenInput;
        
        public InputGenerator(Func<GenerateContext, UniTaskCompletionSource<bool>> onListenInput)
        {
            _onListenInput = onListenInput;
        }
        
        public InputGenerator() { }
        
        public async UniTask<bool> Generate(GenerateContext context, CancellationToken _)
        {
            return await _onListenInput(context).Task;
        }
    }
}