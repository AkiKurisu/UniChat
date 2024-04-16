using System;
namespace Kurisu.UniChat.Chains
{
    public class StackableChainException : Exception
    {
        public StackableChainException(string message, Exception inner) : base(message, inner) { }

        public StackableChainException() { }

        public StackableChainException(string message) : base(message) { }
    }
    public class OutputParserException : Exception
    {
        public string Output { get; } = string.Empty;

        public OutputParserException(string message, string output = null) : base(message)
        {
            Output = output ?? string.Empty;
        }

        public OutputParserException()
        {
        }

        public OutputParserException(string message) : base(message)
        {
        }

        public OutputParserException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}