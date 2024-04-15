using System.Collections.Generic;
namespace Kurisu.UniChat.NLP
{
    /// <summary>
    /// Common recursive text splitter for natural language processing
    /// </summary>
    public class RecursiveCharacterTextSplitter : ISplitter
    {
        public char[] punctuations = new char[] { '。', '？', '！', '.', ':', ';', '!', '?', '~' };
        public RecursiveCharacterTextSplitter() { }
        public RecursiveCharacterTextSplitter(char[] punctuations)
        {
            this.punctuations = punctuations;
        }
        public void Split(string input, IList<string> outputs)
        {
            int endIndex = input.IndexOfAny(punctuations);
            if (endIndex != -1)
            {
                string sentence = input[..(endIndex + 1)].Trim();
                if (!string.IsNullOrEmpty(sentence))
                    outputs.Add(sentence);
                string remainingText = input[(endIndex + 1)..];
                Split(remainingText, outputs);
            }
            else
            {
                string sentence = input.Trim();
                if (!string.IsNullOrEmpty(sentence))
                    outputs.Add(sentence);
            }
        }
    }
}