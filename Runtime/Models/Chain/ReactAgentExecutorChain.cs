//TODO: Not implement in unity yet
// using System;
// using System.Collections.Generic;
// using Cysharp.Threading.Tasks;
// using Kurisu.UniChat;
// using System.Linq;
// using Kurisu.UniChat.Tools;
// using static Kurisu.UniChat.Chains.Chain;
// namespace Kurisu.UniChat.Chains
// {
//     public class ReactAgentExecutorChain : StackableChain
//     {
//         public const string DefaultPrompt =
//             @"Answer the following questions as best you can. You have access to the following tools:
//             {tools}
//             Use the following format:
//             Question: the input question you must answer.
//             Thought: you should always think about what to do.
//             Action: the tool name, should be one of [{tool_names}].
//             Action Input: the input to the tool.
//             Observation: the result of the tool (this Thought/Action/Action Input/Observation can repeat multiple times).
//             Thought: I now know the final answer (no actions before final answer).
//             Final Answer: the final answer to the original input question.
//             You always add [END] after final answer.
//             <Start>
//             Question: {input}
//             Thought:{history}";

//         private StackChain _chain;
//         private readonly Dictionary<string, AgentTool> _tools = new();
//         private readonly GPTAgent _model;
//         private readonly string _reactPrompt;
//         private readonly int _maxActions;
//         private readonly MessageFormatter _messageFormatter;
//         private readonly ChatMessageHistory _chatMessageHistory;
//         private readonly ConversationBufferMemory _conversationBufferMemory;

//         /// <summary>
//         /// 
//         /// </summary>
//         /// <param name="model"></param>
//         /// <param name="reActPrompt"></param>
//         /// <param name="maxActions"></param>
//         /// <param name="inputKey"></param>
//         /// <param name="outputKey"></param>
//         public ReactAgentExecutorChain(
//             GPTAgent model,
//             string reactPrompt = null,
//             int maxActions = 5,
//             string inputKey = "answer",
//             string outputKey = "final_answer")
//         {
//             reactPrompt ??= DefaultPrompt;
//             _model = model;
//             _reactPrompt = reactPrompt;
//             _maxActions = maxActions;

//             InputKeys = new[] { inputKey };
//             OutputKeys = new[] { outputKey };

//             _messageFormatter = new MessageFormatter
//             {
//                 AiPrefix = "",
//                 HumanPrefix = "",
//                 SystemPrefix = ""
//             };

//             _chatMessageHistory = new ChatMessageHistory()
//             {
//                 // Do not save human messages
//                 IsMessageAccepted = x => (x.Role != MessageRole.Human)
//             };

//             _conversationBufferMemory = new ConversationBufferMemory(_chatMessageHistory)
//             {
//                 Formatter = _messageFormatter
//             };
//         }

//         private string _userInput = string.Empty;
//         private const string ReActAnswer = "answer";
//         private void InitializeChain()
//         {
//             var toolNames = string.Join(",", _tools.Select(x => x.Key));
//             var tools = string.Join("\n", _tools.Select(x => $"{x.Value.Name}, {x.Value.Description}"));

//             var chain =
//                 Set(() => _userInput, "input")
//                 | Set(tools, "tools")
//                 | Set(toolNames, "tool_names")
//                 | LoadMemory(_conversationBufferMemory, outputKey: "history")
//                 | Template(_reactPrompt)
//                 | LLM(_model)
//                 | UpdateMemory(_conversationBufferMemory, requestKey: "input", responseKey: "text")
//                 | ReactParser(inputKey: "text", outputKey: ReActAnswer);

//             _chain = chain;
//         }

//         protected override async UniTask<IChainValues> InternalCall(IChainValues values)
//         {
//             values = values ?? throw new ArgumentNullException(nameof(values));

//             var input = (string)values.Value[InputKeys[0]];
//             var valuesChain = new ChainValues();

//             _userInput = input;

//             if (_chain == null)
//             {
//                 InitializeChain();
//             }

//             for (int i = 0; i < _maxActions; i++)
//             {
//                 var res = await _chain!.CallAsync(valuesChain);
//                 if (res.Value[ReActAnswer] is AgentAction)
//                 {
//                     var action = (AgentAction)res.Value[ReActAnswer];
//                     var tool = _tools[action.Action.ToLower(CultureInfo.InvariantCulture)];
//                     var toolRes = await tool.ToolTask(action.ActionInput).ConfigureAwait(false);
//                     await _conversationBufferMemory.ChatHistory.AddMessage(new Message("Observation: " + toolRes, MessageRole.System))
//                         .ConfigureAwait(false);
//                     await _conversationBufferMemory.ChatHistory.AddMessage(new Message("Thought:", MessageRole.System))
//                         .ConfigureAwait(false);
//                     continue;
//                 }
//                 else if (res.Value[ReActAnswer] is AgentFinish)
//                 {
//                     var finish = (AgentFinish)res.Value[ReActAnswer];
//                     values.Value[OutputKeys[0]] = finish.Output;
//                     return values;
//                 }
//             }



//             return values;
//         }

//         public ReactAgentExecutorChain UseTool(AgentTool tool)
//         {
//             tool = tool ?? throw new ArgumentNullException(nameof(tool));
//             _tools.Add(tool.Name, tool);
//             return this;
//         }
//     }
// }