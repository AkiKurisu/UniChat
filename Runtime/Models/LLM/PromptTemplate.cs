using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using Newtonsoft.Json;
namespace UniChat.LLMs
{
    public class PromptTemplate
    {
        private readonly string _templateText;
        
        public PromptTemplate(string text)
        {
            _templateText = text;
        }
        
        public static PromptTemplate FromFilePath(string path)
        {
            if (File.Exists(path))
            {
                return new PromptTemplate(File.ReadAllText(path));
            }

            throw new Exception($"File not exist in {path}");
        }
        
        public List<string> GetVariables()
        {
            string pattern = @"\{([^\{\}]+)\}";
            var variables = new List<string>();
            var matches = Regex.Matches(_templateText, pattern);
            foreach (Match match in matches)
            {
                variables.Add(match.Groups[1].Value);
            }
            return variables;
        }
        
        public string Format(Dictionary<string, object> inputs)
        {
            string output = _templateText;
            foreach (var pair in inputs)
            {
                var key = "{" + pair.Key + "}";
                if (pair.Value is string stringValue)
                {
                    output = output.Replace(key, stringValue);
                }
                else
                {
                    output = output.Replace(key, JsonConvert.SerializeObject(pair.Value));
                }
            }
            return output;
        }
    }
}