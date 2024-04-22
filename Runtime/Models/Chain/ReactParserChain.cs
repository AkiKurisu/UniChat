using System;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
namespace Kurisu.UniChat.Chains
{
    /// <summary>
    /// Reference https://api.python.langchain.com/en/latest/_modules/langchain/agents/output_parsers/react_single_input.html#ReActSingleInputOutputParser
    /// </summary>
    public class ReActParserChain : StackableChain
    {
        public ReActParserChain(
            string inputKey = "text",
            string outputText = "answer")
        {
            InputKeys = new[] { inputKey };
            OutputKeys = new[] { outputText };
        }

        private const string FinalAnswerAction = "Final Answer:";
        private const string MissingActionAfterThoughtErrorMessage = "Invalid Format: Missing 'Action:' after 'Thought:";
        private const string MissingActionInputAfterActionErrorMessage = "Invalid Format: Missing 'Action Input:' after 'Action:'";
        private const string FinalAnswerAndParsableActionErrorMessage = "Parsing LLM output produced both a final answer and a parse-able action:";

        public object Parse(string text)
        {
            text = text ?? throw new ArgumentNullException(nameof(text));

            bool includesAnswer = text.Contains(FinalAnswerAction);
            string regex = @"Action\s*\d*\s*:[\s]*(.*?)[\s]*Action\s*\d*\s*Input\s*\d*\s*:[\s]*(.*)";
            Match actionMatch = Regex.Match(text, regex, RegexOptions.Singleline);

            if (actionMatch.Success)
            {
                if (includesAnswer)
                {
                    throw new OutputParserException($"{FinalAnswerAndParsableActionErrorMessage}: {text}");
                }
                string action = actionMatch.Groups[1].Value.Trim();
                string actionInput = actionMatch.Groups[2].Value.Trim().Trim('\"');

                return new AgentAction(action, actionInput, text);
            }
            else if (includesAnswer)
            {
                return new AgentFinish(text.Split(FinalAnswerAction, StringSplitOptions.None)[^1].Trim(), text);
            }

            if (!Regex.IsMatch(text, @"Action\s*\d*\s*:[\s]*(.*?)", RegexOptions.Singleline))
            {
                throw new OutputParserException($"Could not parse LLM output: `{text}`", MissingActionAfterThoughtErrorMessage);
            }
            else if (!Regex.IsMatch(text, @"[\s]*Action\s*\d*\s*Input\s*\d*\s*:[\s]*(.*)", RegexOptions.Singleline))
            {
                throw new OutputParserException($"Could not parse LLM output: `{text}`", MissingActionInputAfterActionErrorMessage);
            }
            else
            {
                throw new OutputParserException($"Could not parse LLM output: `{text}`");
            }
        }

        protected override UniTask<IChainValues> InternalCall(IChainValues values)
        {
            values = values ?? throw new ArgumentNullException(nameof(values));

            values.Value[OutputKeys[0]] = Parse(values.Value[InputKeys[0]].ToString()!);
            return UniTask.FromResult(values);
        }
    }


    public class AgentAction
    {
        public string Action { get; }

        public string ActionInput { get; }

        public string Text { get; }
        public AgentAction(string action, string actionInput, string text)
        {
            Action = action;
            ActionInput = actionInput;
            Text = text;
        }
        public override string ToString()
        {
            return $"Action: {Action}, Action Input: {ActionInput}";
        }
    }

    public class AgentFinish
    {
        public string Output { get; }

        public string Text { get; }

        public AgentFinish(string output, string text)
        {
            Output = output;
            Text = text;
        }
        public override string ToString()
        {
            return $"Final Answer: {Output}";
        }
    }
}
