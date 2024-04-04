using Unity.Sentis;
namespace Kurisu.UniChat
{
    public interface IEmbeddingDataBase
    {
        /// <summary>
        /// Data count
        /// </summary>
        /// <value></value>
        int Count { get; }
        /// <summary>
        /// Allocate embedding tensors
        /// </summary>
        /// <returns></returns>
        TensorFloat[] AllocateTensors();
    }
}