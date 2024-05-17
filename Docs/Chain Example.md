<div align="center">

# Chain Example


This document contains example code of using Chain api.


</div>

## Simple Chain
```C#
string prompt = @"How many planets are there?";
var ollama = new OllamaChat() { Model = OllamaModels.Llama3 };
//Create chain
var chain = Chain.Set(prompt) | Chain.LLM(ollama);
//Run chain
string result = await chain.Run<string>("text");
Debug.Log(result);
```

## With Memory
```C#
string prompt = @"How many planets are there?";
string template = "{history}\n{prompt}";
var ollama = new OllamaChat() { Model = OllamaModels.Llama3 };
var history = new ChatHistory();
var memory = new ChatBufferMemory() { ChatHistory = history };
//Create chain
var chain = Chain.Set(prompt, "prompt")
            //First load memory context and fill in `{history}`
            | Chain.LoadMemory(memory, "history")
            | Chain.Template(template)
            | Chain.LLM(ollama)
            //Append `prompt` and `text` (response of llm) to history
            | Chain.UpdateHistory(history, requestKey: "prompt");
//Run chain
string result = await chain.Run<string>("text");
Debug.Log(result);
//See the latest memory context
Debug.Log(memory.GetMemoryContext());
```

## Reference
https://github.com/tryAGI/LangChain