using System.Text;
namespace UniChat
{
    public class TavernAICard
    {
        public string char_name;

        public string char_persona;

        public string char_greeting;

        public string example_dialogue;
        
        public string world_scenario;
    }

    public static class DefaultTavernAIFormatter
    {
        public static string Format(TavernAICard tavernAICard, string user_Name)
        {
            StringBuilder stringBuilder = new();
            if (!string.IsNullOrEmpty(tavernAICard.char_persona))
                stringBuilder.AppendLine($"{tavernAICard.char_name}'s persona: {ReplaceCharName(tavernAICard.char_persona)}");
            if (!string.IsNullOrEmpty(tavernAICard.world_scenario))
                stringBuilder.AppendLine($"Scenario: {ReplaceCharName(tavernAICard.world_scenario)}");
            if (!string.IsNullOrEmpty(tavernAICard.example_dialogue))
            {
                // Few-shot
                stringBuilder.AppendLine("<START>");
                stringBuilder.AppendLine($"{ReplaceCharName(tavernAICard.example_dialogue)}");
            }
            stringBuilder.AppendLine("<START>");
            if (!string.IsNullOrEmpty(tavernAICard.char_greeting))
            {
                stringBuilder.AppendLine($"{ReplaceCharName(tavernAICard.char_greeting)}");
            }
            return stringBuilder.ToString();
            string ReplaceCharName(string input)
            {
                return input.Replace("{{char}}", tavernAICard.char_name).Replace("{{user}}", user_Name);
            }
        }
    }
}