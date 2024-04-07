using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kurisu.NGDS.AI;
using Newtonsoft.Json;
using UnityEngine;
using System.IO;
namespace Kurisu.UniChat
{
    public class ChatGenerateCtrl
    {
        public const int InputGeneratorId = 0;
        public const int ChatGPTGeneratorId = 1;
        public const int OobaboogaGeneratorId = 2;
        public const int KoboldCPPGeneratorId = 3;
        public string Context { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string BotName { get; set; } = string.Empty;
        public event Action<GenerateContext> OnGetContext;
        public ChatHistoryContext History { get; } = new();
        private readonly Dictionary<int, IGenerator> generatorMap = new();
        private UniTaskCompletionSource<bool> waitSource;
        private GenerateContext generateContext;
        public IGenerator Generator { get; private set; }
        private readonly AITurboSetting aiTurboSetting;
        public ChatGenerateCtrl(AITurboSetting aiTurboSetting)
        {
            this.aiTurboSetting = aiTurboSetting;
            Generator = generatorMap[-1] = new InputGenerator(OnCallGeneration);
        }
        public void SwapGenerator(int generatorId)
        {
            if (generatorId == InputGeneratorId)
            {
                SwitchInputGenerator();
            }
            else
            {
                var llmType = generatorId switch
                {
                    ChatGPTGeneratorId => LLMType.ChatGPT,
                    OobaboogaGeneratorId => LLMType.Oobabooga,
                    KoboldCPPGeneratorId => LLMType.KoboldCPP,
                    _ => throw new ArgumentOutOfRangeException()
                };
                SwitchLLMGenerator(llmType);
            }
        }
        private IGenerator SwitchLLMGenerator(LLMType llmType)
        {
            int id = (int)llmType;
            if (!generatorMap.TryGetValue(id, out var generator))
            {
                generator = generatorMap[id] = new LLMGenerator(LLMFactory.Create(llmType, aiTurboSetting), History);
            }
            return Generator = generator;
        }
        private IGenerator SwitchInputGenerator()
        {
            return Generator = generatorMap[-1];
        }
        public UniTaskCompletionSource<bool> OnCallGeneration(GenerateContext generateContext)
        {
            this.generateContext = generateContext;
            Debug.Log("Call generator");
            OnGetContext?.Invoke(generateContext);
            waitSource = new UniTaskCompletionSource<bool>();
            return waitSource;
        }
        public void SetResult(string generatedContent)
        {
            generateContext.generatedContent = generatedContent;
            waitSource?.TrySetResult(true);
            waitSource = null;
        }
        public void CancelGeneration()
        {
            waitSource?.TrySetResult(false);
            waitSource = null;
        }
        public void SaveSession(string filePath)
        {
            Debug.Log($"Chat session was saved to {filePath}");
            File.WriteAllText(filePath, JsonConvert.SerializeObject(History.SaveSession(), Formatting.Indented));
        }
        public bool LoadSession(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return false;
            }
            var session = JsonConvert.DeserializeObject<ChatSession>(File.ReadAllText(filePath));
            History.LoadSession(session); ;
            return true;
        }
    }
}