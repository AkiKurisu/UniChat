using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Linq;
using Kurisu.UniChat.Tools;
using static Kurisu.UniChat.Chains.Chain;
using Kurisu.UniChat.LLMs;
using Kurisu.UniChat.Memory;
namespace Kurisu.UniChat.Chains
{
    //See https://github.com/langchain-ai/langchain/blob/master/libs/langchain/langchain/agents/agent.py
    public class ReActAgentExecutorChain : StackableChain
    {
        //See https://api.python.langchain.com/en/latest/agents/langchain.agents.react.agent.create_react_agent.html
        public const string DefaultPrompt =
            @"Answer the following questions as best you can. You have access to the following tools:

{tools}

Use the following format:

Question: the input question you must answer
Thought: you should always think about what to do
Action: the tool name, should be one of [{tool_names}]
Action Input: the input to the tool
Observation: the result of the tool
(this Thought/Action/Action Input/Observation can repeat multiple times)
Thought: I now know the final answer
(no actions before final answer)
Final Answer: the final answer to the original input question
Always add [END] after final answer

Begin!

Question: {input}
Thought:{history}";

        private StackChain _chain;
        private readonly Dictionary<string, AgentTool> _tools = new();
        private readonly ILargeLanguageModel _model;
        private readonly string _reactPrompt;
        private readonly int _maxActions;
        private readonly MessageFormatter _messageFormatter;
        private readonly ChatHistory chatHistory;
        private readonly ChatMemory chatMemory;
        private bool verbose;
        public ReActAgentExecutorChain(
            ILargeLanguageModel model,
            string reactPrompt = null,
            int maxActions = 5,
            string inputKey = "answer",
            string outputKey = "final_answer")
        {
            reactPrompt ??= DefaultPrompt;
            _model = model;
            _reactPrompt = reactPrompt;
            _maxActions = maxActions;

            InputKeys = new[] { inputKey };
            OutputKeys = new[] { outputKey };

            _messageFormatter = new MessageFormatter
            {
                BotPrefix = "",
                UserPrefix = "",
                SystemPrefix = ""
            };

            chatHistory = new ChatHistory();

            chatMemory = new ToolUseMemory(chatHistory)
            {
                Formatter = _messageFormatter
            };
        }

        private string _userInput = string.Empty;
        private const string ReactAnswerKey = "answer";
        private void InitializeChain()
        {
            var toolNames = string.Join(",", _tools.Select(x => x.Key));
            var tools = string.Join("\n", _tools.Select(x => $"{x.Value.Name}: {x.Value.Description}"));

            var chain =
                Set(() => _userInput, "input")
                | Set(tools, "tools")
                | Set(toolNames, "tool_names")
                | LoadMemory(chatMemory, outputKey: "history")
                | Template(_reactPrompt)
                | LLM(_model).Verbose(verbose)
                | UpdateHistory(chatHistory, requestKey: "input", responseKey: "text")
                | ReActParser(inputKey: "text", outputKey: ReactAnswerKey);

            _chain = chain;
        }

        protected override async UniTask<IChainValues> InternalCall(IChainValues values)
        {
            values = values ?? throw new ArgumentNullException(nameof(values));

            var input = (string)values.Value[InputKeys[0]];
            var valuesChain = new ChainValues();

            _userInput = input;

            if (_chain == null)
            {
                InitializeChain();
            }

            for (int i = 0; i < _maxActions; i++)
            {
                var res = await _chain!.CallAsync(valuesChain);
                if (res.Value[ReactAnswerKey] is AgentAction action)
                {
                    var tool = _tools[action.Action];
                    var toolRes = await tool.ExecuteTool(action.ActionInput);
                    chatMemory.ChatHistory.AppendSystemMessage("Observation: " + toolRes);
                    chatMemory.ChatHistory.AppendSystemMessage("Thought:");
                    continue;
                }
                else if (res.Value[ReactAnswerKey] is AgentFinish finish)
                {
                    values.Value[OutputKeys[0]] = finish.Output;
                    return values;
                }
            }
            return values;
        }
        public ReActAgentExecutorChain Verbose(bool verbose)
        {
            this.verbose = verbose;
            return this;
        }
        public ReActAgentExecutorChain UseTool(AgentTool tool)
        {
            tool = tool ?? throw new ArgumentNullException(nameof(tool));
            _tools.Add(tool.Name, tool);
            return this;
        }
        public ReActAgentExecutorChain UseTool(IEnumerable<AgentTool> tools)
        {
            foreach (var tool in tools)
            {
                UseTool(tool);
            }
            return this;
        }
    }
}