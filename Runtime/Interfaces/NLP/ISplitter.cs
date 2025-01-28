using System.Collections.Generic;
namespace UniChat
{
    public interface ISplitter
    {
        /// <summary>
        /// Split substrings from input
        /// </summary>
        /// <param name="input"></param>
        /// <param name="outputs"></param>
        /// <returns></returns>
        void Split(string input, IList<string> outputs);
    }
}