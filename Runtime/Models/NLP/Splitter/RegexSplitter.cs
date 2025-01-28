using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
namespace UniChat.NLP
{
    public class RegexSplitter : ISplitter
    {
        public string pattern = @"(?<=[。！？! ?])";
        public RegexSplitter() { }
        public RegexSplitter(string pattern)
        {
            this.pattern = pattern;
        }

        public void Split(string input, IList<string> outputs)
        {
            var segments = Regex.Split(input, pattern)
                                .Select(x => x.Trim())
                                .Where(x => !string.IsNullOrEmpty(x))
                                .ToList();

            outputs.AddRange(segments);
        }
    }
}