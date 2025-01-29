using System.Text.RegularExpressions;

namespace UniChat.LLMs
{
    public static class DeepSeekFormater
    {
        public static (string think, string output) Format(string input)
        {
            var regex = new Regex(@"<think>(.*?)</think>(.*)", RegexOptions.Singleline);
            var match = regex.Match(input);

            if (match.Success)
            {
                string thinkInner = match.Groups[1].Value.Trim();
                string thinkOuter = match.Groups[2].Value.Trim();
                return (thinkInner, thinkOuter);
            }

            return (null, input);
        }
    }
}