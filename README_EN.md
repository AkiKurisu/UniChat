<div align="center">

# UniChat

[中文](README.md) | [English](README_en.md)

A Unity Chat Bot pipeline for on-line and off-line operation.

<img src="Images/Icon.png" width="256"/>

</div>

  - [Introduction](#introduction)
  - [Core pipeline](#core-pipeline)
    - [Quick use](#quick-use)
    - [Embedded model](#embedded-model)
  - [Chain](#chain)
    - [Combined with the core pipeline](#combined-with-the-core-pipeline)
  - [Middleware](#middleware)
    - [Text to Speech](#text-to-speech)
    - [Sub-classifier](#sub-classifier)
  - [Game Components](#game-components)
    - [Chat StateMachine](#chat-statemachine)
  - [Demo](#demo)
    - [Minimalist Demo Download](#minimalist-demo-download)
    - [Advanced Demo download](#advanced-demo-download)
    - [Demo function description](#demo-function-description)
      - [Personalization: role cards](#personalization-role-cards)
  - [Quote](#quote)




## Introduction

With `Unity.Sentis` the release of, we can use some neural network models in Runtime, including the text vector embedding model (Text Embedding Model) for natural language processing.

Although chatting with AI is nothing new, in games, how to design a conversation that does not deviate from the developer's ideas but is more flexible is a difficult point.

`UniChat` is based on `Unity.Sentis` and text vector embedding technology, which enables <b>offline mode</b> to search text content based on vector databases.

Of course, if you use the online mode, `UniChat` also includes a chain toolkit based on [LangChain](https://github.com/langchain-ai/langchain) to quickly embed LLM and Agent in the game.

The following is the flow chart of UniChat, and the functions that can be used offline are shown in `Local Inference` the box:

![流程图](Images/UniChat.png)

## Core pipeline

### Quick use

1. Create or load

```C#
public void CreatePipelineCtrl()
{
    //1. New file
    ChatPipelineCtrl PipelineCtrl = new(new ChatModelFile() { fileName = $"ChatModel_{Guid.NewGuid().ToString()[0..6]}" });
    //2. Load from filePath
    PipelineCtrl= new(JsonConvert.DeserializeObject<ChatModelFile>(File.ReadAllText(filePath)))
}
```

2. Run the pipeline

```C#
public bool RunPipeline()
{
    var context = await PipelineCtrl.RunPipeline("Hello!");
    if ((context.flag & (1 << 1)) != 0)
    {
        //Get text output
        string output = context.CastStringValue();
        //Update history
        PipelineCtrl.History.AppendBotMessage(output, context.outputEntry.Hash);
        return true;
    }
    else
    {
        //Remove last user input
        PipelineCtrl.History.RemoveLastUserMessage();
        return false;
    }
}
```

3. Save the generated text and embedding vector

```C#
pubic void Save()
{
    //PC save to {ApplicationPath}//UserData//{ModelName}
    //Android save to {Application.persistentDataPath}//UserData//{ModelName}
    PipelineCtrl.SaveModel();
}
```

### Embedded model

The embedded model is used `BAAI/bge-small-zh-v1.5` by default and occupies the least video memory. It only supports Chinese. You can download the same model from `HuggingFaceHub` and convert it to Onnx format.

The loading mode is optional `UserDataProvider`, `StreamingAssetsProvider` and `ResourcesProvider`, if installed `Unity.Addressables`, optional `AddressableProvider`.

The `UserDataProvider` file path is as follows:

![UserData](Images/userdata_provider.png)

 `ResourcesProvider` Place the files in the models folder in the Resources folder.

 `StreamingAssetsProvider` Place the files in the models folder in the StreamingAssets folder.

Address `AddressablesProvider` of is as follows:

![Addressables](Images/addressable-provider.png)

## Chain

UniChat is based on [LangChain C#](https://github.com/tryAGI/LangChain) using a chain structure to connect components in series.

You can see an sample in repo's Example.

The simple use is as follows:

```C#
public class LLM_Chain_Example : MonoBehaviour
{
    public LLMSettingsAsset settingsAsset;
    public AudioSource audioSource;
    public async void Start()
    {
        var chatPrompt = @"
            You are an AI assistant that greets the world.
            User: Hello!
            Assistant:";
        var llm = LLMFactory.Create(LLMType.ChatGPT, settingsAsset);
        //Create chain
        var chain =
            Chain.Set(chatPrompt, outputKey: "prompt")
            | Chain.LLM(llm, inputKey: "prompt", outputKey: "chatResponse");
        //Run chain
        string result = await chain.Run<string>("chatResponse");
        Debug.Log(result);
    }
}
```

### Combined with the core pipeline

The above example uses `Chain` to call LLM directly, but to simplify searching the database and facilitate engineering, it is recommended to use `ChatPipelineCtrl` as the beginning of the chain.

If you run the following example, the first time you call LLM and the second time you reply directly from the database.


```C#
public async void Start()
{
    //Create new chat model file with empty memory and embedding db
    var chatModelFile = new ChatModelFile() { fileName = "NewChatFile", modelProvider = ModelProvider.AddressableProvider };
    //Create an pipeline ctrl to run it
    var pipelineCtrl = new ChatPipelineCtrl(chatModelFile, settingsAsset);
    pipelineCtrl.SwitchGenerator(ChatGeneratorIds.ChatGPT, true);
    //Init pipeline, set verbose to log status
    await pipelineCtrl.InitializePipeline(new PipelineConfig { verbose = true });
    //Add some chat data
    pipelineCtrl.Memory.Context = "You are my personal assistant, you should answer my questions.";
    pipelineCtrl.History.AppendUserMessage("Hello assistant!");
    //Create chain
    var chain = pipelineCtrl.ToChain().CastStringValue(outputKey: "text");
    //Run chain
    string result = await chain.Run<string>("text");
    //Save chat model
    pipelineCtrl.SaveModel();
}
```

## Middleware

### Text to Speech

If you have a speech synthesis solution, you can refer to [VITSClient](./Runtime/Models/Audio/VITSClient.cs) the implementation of a TTS component.

You can use `AudioCache` to store speech so that it can be played when you pick up an answer from the database in offline mode.


```C#
public class LLM_VITS_Chain_Example : MonoBehaviour
{
    public LLMSettingsAsset settingsAsset;
    public AudioSource audioSource;
    public async void Start()
    {
        //Create new chat model file with empty memory and embedding db
        var chatModelFile = new ChatModelFile() { fileName = "NewChatFile", modelProvider = ModelProvider.AddressableProvider };
        //Create an pipeline ctrl to run it
        var pipelineCtrl = new ChatPipelineCtrl(chatModelFile, settingsAsset);
        pipelineCtrl.SwitchGenerator(ChatGeneratorIds.ChatGPT, true);
        //Init pipeline, set verbose to log status
        await pipelineCtrl.InitializePipeline(new PipelineConfig { verbose = true });
        var vitsClient = new VITSClient(lang: "ja");
        //Add some chat data
        pipelineCtrl.Memory.Context = "You are my personal assistant, you should answer my questions.";
        pipelineCtrl.History.AppendUserMessage("Hello assistant!");
        //Create cache to cache audioClips and translated texts
        var audioCache = AudioCache.CreateCache(chatModelFile.DirectoryPath);
        var textCache = TextMemoryCache.CreateCache(chatModelFile.DirectoryPath);
        //Create chain
        var chain = pipelineCtrl.ToChain().CastStringValue(outputKey: "text")
                                //Translate to japanese
                                | Chain.Translate(new GoogleTranslator("en", "ja")).UseCache(textCache)
                                //Split them
                                | Chain.Split(new RegexSplitter(@"(?<=[。！？! ?])"), inputKey: "translated_text")
                                //Auto batched
                                | Chain.TTS(vitsClient, inputKey: "splitted_text").UseCache(audioCache).Verbose(true);
        //Run chain
        (IReadOnlyList<string> segments, IReadOnlyList<AudioClip> audioClips)
            = await chain.Run<IReadOnlyList<string>, IReadOnlyList<AudioClip>>("splitted_text", "audio");
        //Play audios
        for (int i = 0; i < audioClips.Count; ++i)
        {
            Debug.Log(segments[i]);
            audioSource.clip = audioClips[i];
            audioSource.Play();
            await UniTask.WaitUntil(() => !audioSource.isPlaying);
        }
    }
}
```

### Sub-classifier
You can reduce the dependence on LLM by training a downstream classifier on the basis of the embedded model to complete some recognition tasks in the game (such as expression classifier).

**Notice**

*1. You need to make the component in a Python environment.*

*2. Currently, Sentis still requires you to manually export to ONNX format*

Best practice: Use an embedded model to generate traits from your training data before training. Only the downstream model needs to be exported afterwards.

The following is an example `shape=(512,768,20)` of a multi-layer perceptron classifier with an export size of only 1.5MB:

```python
class SubClassifier(nn.Module):
    #input_dim is the output dim of your embedding model
    def __init__(self, input_dim, hidden_dim, output_dim):
        super(CustomClassifier, self).__init__()
        
        self.fc1 = nn.Linear(input_dim, hidden_dim)
        self.relu = nn.ReLU()
        self.dropout = nn.Dropout(p=0.1)
        self.fc2 = nn.Linear(hidden_dim, output_dim)
    
    def forward(self, x):
        x = self.fc1(x)
        x = self.relu(x)
        x = self.dropout(x)
        x = self.fc2(x)
        return x
```

## Game Components

Game components are various tools that are combined with the dialogue function according to the specific game mechanism.

### Chat StateMachine

A stateMachine that switches States according to the chat content. StateMachine nesting (SubStateMachine) is not currently supported. Depending on the conversation, you can jump to different States and execute the corresponding set of behaviors, similar to Unity's animated state machine.

1. Configure in code

```C#
 public void BuildStateMachine()
{
    chatStateMachine = new ChatStateMachine(dim: 512);
    chatStateMachineCtrl = new ChatStateMachineCtrl(
        TextEncoder: encoder, 
        //Input a host Unity.Object
        hostObject: gameObject, 
        layer: 1
    );
    chatStateMachine.AddState("Stand");
    chatStateMachine.AddState("Sit");
    chatStateMachine.states[0].AddBehavior<StandBehavior>();
    chatStateMachine.states[0].AddTransition(new LazyStateReference("Sit"));
    // Add a conversion directive and set scoring thresholds and conditions
    chatStateMachine.states[0].transitions[0].AddCondition(ChatConditionMode.Greater, 0.6f, "I sit down");
    chatStateMachine.states[0].transitions[0].AddCondition(ChatConditionMode.Greater, 0.6f, "I want to have a rest on chair");
    chatStateMachine.states[1].AddBehavior<SitBehavior>();
    chatStateMachine.states[1].AddTransition(new LazyStateReference("Stand"));
    chatStateMachine.states[1].transitions[0].AddCondition(ChatConditionMode.Greater, 0.6f, "I'm well rested");
    chatStateMachineCtrl.SetStateMachine(0, chatStateMachine);
}
```

2. Configured in Editor Window and saved as a text file.

![Configure in editor](Images/edit-fsm.png)


```C#
public void LoadFromBytes(string bytesFilePath)
{
    chatStateMachineCtrl.Load(bytesFilePath);
}
```

3. Customize ChatS Tate MachineBehavior.


```C#
public class CustomChatBehavior : ChatStateMachineBehavior
{
    private GameObject hostGameObject;
    public override void OnStateMachineEnter(UnityEngine.Object hostObject)
    {
        //Get host Unity.Object
        hostGameObject = hostObject as GameObject;
    }
    public override void OnStateEnter()
    {
       //Do something 
    }
    public override void OnStateUpdate()
    {
       //Do something 
    }
    public override void OnStateExit()
    {
       //Do something 
    }
}
```

4. Running the state machine after the core pipeline is run


```C#
//Run stateMachine after chain of pipeline done
private void OnBotAnswer(GenerateContext context)
{
    chatStateMachineCtrl.Execute(context.CastStringValue());
}
```

## Demo

Here are some of the sample apps I've made. Because they include some commercial plug-ins, only Build versions are available.

See [Release](https://github.com/AkiKurisu/UniChat/releases) page


Based on UniChat to make a similar application in Unity> The synchronized repository version is `V0.0.1-alpha`, the Demo is waiting to be updated.
### Minimalist Demo Download

![聊天界面](Images/chat-view.png)

See [Release](https://github.com/AkiKurisu/UniChat/releases) page


### Advanced Demo download

![Demo Pro](Images/demo-pro.png)

It contains behavioral and voice components and is not yet available.

### Demo function description

#### Personalization: role cards

Demo uses `TavernAI` the character data structure, and we can write the character's personality, sample conversations, and chat scenarios into pictures.

![Setting View](Images/setting-view.png)

If you use `TavernAI` a character card, the cue word above is overwritten.

## Quote

- Make a ChatBox in Unity
    >https://www.akikurisu.com/blog/posts/create-chatbox-in-unity-2024-03-19/
- Using NLP Natural Language Processing Technology in Unity
    >https://www.akikurisu.com/blog/posts/use-nlp-in-unity-2024-04-03/
- https://github.com/langchain-ai/langchain
- https://github.com/tryAGI/LangChain
- - Yao S, Zhao J, Yu D, et al. React: Synergizing reasoning and acting in language models[J]. arXiv preprint arXiv:2210.03629, 2022.