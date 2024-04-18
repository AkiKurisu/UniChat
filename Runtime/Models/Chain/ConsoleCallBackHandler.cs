using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;
using UnityEngine;
namespace Kurisu.UniChat.Chains
{
    public class ConsoleCallbackHandler : Tracer
    {
        public override string Name => "console_callback_handler";
        protected override UniTask PersistRun(Run run) => UniTask.CompletedTask;
        protected override UniTask HandleLlmStartAsync(Run run)
        {
            run = run ?? throw new ArgumentNullException(nameof(run));

            var crumbs = GetBreadcrumbs(run);
            object inputs = run.Inputs.TryGetValue("prompts", out var input)
                ? new Dictionary<string, List<string>>
                {
                    ["prompts"] = (input as List<string>)?.Select(p => p.Trim()).ToList() ?? new List<string>(),
                }
                : run.Inputs;

            Print(
                $"{GetColoredText("[llm/start]", ConsoleFormats.Green)} {GetColoredText($"[{crumbs}] Entering LLM run with input:", ConsoleFormats.Bold)}\n" +
                $"{JsonSerializeOrDefault(inputs, "[inputs]")}"
            );

            return UniTask.CompletedTask;
        }


        protected override UniTask HandleLlmNewTokenAsync(Run run, string token)
        {
            return UniTask.CompletedTask;
        }


        protected override UniTask HandleLlmErrorAsync(Run run)
        {
            run = run ?? throw new ArgumentNullException(nameof(run));

            var crumbs = GetBreadcrumbs(run);

            Print($"{GetColoredText("[llm/error]", ConsoleFormats.Red)} {GetColoredText($"[{crumbs}] [{Elapsed(run)}] LLM run errored with error:", ConsoleFormats.Bold)}\n" +
                  $"{JsonSerializeOrDefault(run.Error, "[error]")}"
            );

            return UniTask.CompletedTask;
        }


        protected override UniTask HandleLlmEndAsync(Run run)
        {
            run = run ?? throw new ArgumentNullException(nameof(run));

            var crumbs = GetBreadcrumbs(run);

            Print($"{GetColoredText("[llm/end]", ConsoleFormats.Blue)} {GetColoredText($"[{crumbs}] [{Elapsed(run)}] Exiting LLM run with output:", ConsoleFormats.Bold)}\n" +
                  $"{JsonSerializeOrDefault(run.Outputs, "[response]")}"
                );

            return UniTask.CompletedTask;
        }


        protected override UniTask HandleChatModelStartAsync(Run run)
        {
            return UniTask.CompletedTask;
        }


        protected override UniTask HandleChainStartAsync(Run run)
        {
            run = run ?? throw new ArgumentNullException(nameof(run));

            var crumbs = GetBreadcrumbs(run);
            var runType = run.RunType;
            var input = JsonSerializeOrDefault(run.Inputs, "[inputs]");

            Print(
                $"{GetColoredText("[chain/start]", ConsoleFormats.Green)} {GetColoredText($"[{crumbs}] Entering {runType} run with input:", ConsoleFormats.Bold)}\n" +
                $"{input}"
            );

            return UniTask.CompletedTask;
        }


        protected override UniTask HandleChainErrorAsync(Run run)
        {
            run = run ?? throw new ArgumentNullException(nameof(run));

            var crumbs = GetBreadcrumbs(run);
            var runType = run.RunType;
            var error = JsonSerializeOrDefault(run.Error, "[error]");
            Print(
                $"{GetColoredText("[chain/error]", ConsoleFormats.Red)} {GetColoredText($"[{crumbs}] [{Elapsed(run)}] {runType} run errored with error:", ConsoleFormats.Bold)}\n" +
                $"{error}"
            );

            return UniTask.CompletedTask;
        }


        protected override UniTask HandleChainEndAsync(Run run)
        {
            run = run ?? throw new ArgumentNullException(nameof(run));

            var crumbs = GetBreadcrumbs(run);
            var runType = run.RunType;
            var outputs = JsonSerializeOrDefault(run.Outputs, "[outputs]");

            Print(
                $"{GetColoredText("[chain/end]", ConsoleFormats.Blue)} {GetColoredText($"[{crumbs}] [{Elapsed(run)}] Exiting {runType} run with output:", ConsoleFormats.Bold)}\n" +
                $"{outputs}"
            );

            return UniTask.CompletedTask;
        }


        protected override UniTask HandleToolStartAsync(Run run)
        {
            run = run ?? throw new ArgumentNullException(nameof(run));

            var crumbs = GetBreadcrumbs(run);
            Print(
                $"{GetColoredText("[chain/start]", ConsoleFormats.Green)} {GetColoredText($"[{crumbs}] Entering Tool run with input:", ConsoleFormats.Bold)}\n" +
                $"{run.Inputs["input"].ToString()?.Trim()}"
            );

            return UniTask.CompletedTask;
        }


        protected override UniTask HandleToolErrorAsync(Run run)
        {
            run = run ?? throw new ArgumentNullException(nameof(run));

            var crumbs = GetBreadcrumbs(run);
            Print(
                $"{GetColoredText("[chain/error]", ConsoleFormats.Red)} {GetColoredText($"[{crumbs}] [{Elapsed(run)}] Tool run errored with error:", ConsoleFormats.Bold)}\n" +
                $"{run.Error}"
            );

            return UniTask.CompletedTask;
        }


        protected override UniTask HandleToolEndAsync(Run run)
        {
            run = run ?? throw new ArgumentNullException(nameof(run));

            var crumbs = GetBreadcrumbs(run);
            if (run.Outputs.Count != 0)
                Print(
                    $"{GetColoredText("[chain/end]", ConsoleFormats.Blue)} {GetColoredText($"[{crumbs}] [{Elapsed(run)}] Exiting Tool run with output:", ConsoleFormats.Bold)}\n" +
                    $"{run.Outputs["output"].ToString()?.Trim()}"
                );

            return UniTask.CompletedTask;
        }


        protected override UniTask HandleTextAsync(Run run)
        {
            return UniTask.CompletedTask;
        }


        protected override UniTask HandleAgentActionAsync(Run run)
        {
            return UniTask.CompletedTask;
        }


        protected override UniTask HandleAgentEndAsync(Run run)
        {
            return UniTask.CompletedTask;
        }


        protected override void OnRunCreate(Run run)
        {
        }


        protected override void OnRunUpdate(Run run)
        {
        }

        private List<Run> GetParents(Run run)
        {
            var parents = new List<Run>();
            var currentRun = run;
            while (currentRun.ParentRunId != null)
            {
                if (RunMap.TryGetValue(currentRun.ParentRunId, out var parent) && parent != null)
                {
                    parents.Add(parent);
                    currentRun = parent;
                }
                else break;
            }

            return parents;
        }

        private string GetBreadcrumbs(Run run)
        {
            var parents = GetParents(run);
            parents.Reverse();
            parents.Add(run);

            var breadcrumbs = parents.Select((parent, i) => $"{parent.ExecutionOrder}:{parent.RunType}:{parent.Name}");
            var result = string.Join(" > ", breadcrumbs);

            return result;
        }

        private void Print(string text)
        {
            Debug.Log(text);
        }

        private string GetColoredText(string text, string format)
        {
            return string.Format(format, text);
        }

        private string JsonSerializeOrDefault(object obj, string @default)
        {
            try
            {
                return JsonConvert.SerializeObject(obj);
            }
            catch (Exception)
            {
                return @default;
            }
        }

        /// <summary>
        /// Get the elapsed time of a run.
        /// </summary>
        /// <returns>A string with the elapsed time in seconds or milliseconds if time is less than a second.</returns>
        private string Elapsed(Run run)
        {
            if (!run.EndTime.HasValue)
                return "N/A";

            var elapsedTime = run.EndTime.Value - run.StartTime;
            var milliseconds = elapsedTime.TotalMilliseconds;

            return elapsedTime.TotalMilliseconds < 1000
                ? $"{milliseconds}ms"
                : $"{elapsedTime.TotalSeconds:F1}s";
        }

        private static class ConsoleFormats
        {
            public static string Normal = "<color=#FFFFFF>{0}</color>";
            public static string Red = "<color=#FF6486>{0}</color>";
            public static string Green = "<color=#59CB86>{0}</color>";
            public static string Yellow = "<color=#FFFF00>{0}</color>";
            public static string Blue = "<color=#5973CB>{0}</color>";
            public static string Bold = "<b>{0}</b>";
            public static string Underline = "<u>{0}</u>";
        }
    }
}