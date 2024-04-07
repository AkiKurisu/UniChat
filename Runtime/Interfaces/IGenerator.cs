using System.Threading;
using Cysharp.Threading.Tasks;
namespace Kurisu.UniChat
{
    public interface IGenerator
    {
        /// <summary>
        /// Call context generation
        /// </summary>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        UniTask<bool> Generate(GenerateContext context, CancellationToken ct);
    }
}