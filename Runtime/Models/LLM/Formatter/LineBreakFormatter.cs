namespace UniChat.LLMs
{
    public static class LineBreakFormatter
    {
        public static string Format(string input)
        {
            int startIndex = 0;
            int endIndex = 0;
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] != '\n')
                {
                    startIndex = i;
                    break;
                }
            }
            for (int i = input.Length - 1; i >= 0; i--)
            {
                if (input[i] != '\n')
                {
                    endIndex = i;
                    break;
                }
            }
            if (startIndex > endIndex) return string.Empty;
            return input.Substring(startIndex, endIndex - startIndex + 1);
        }
    }
}