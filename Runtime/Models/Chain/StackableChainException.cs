using System;
namespace Kurisu.UniChat.Chains
{
    public class StackableChainException : Exception
    {
        public StackableChainException(string message, Exception inner) : base(message, inner) { }

        public StackableChainException() { }

        public StackableChainException(string message) : base(message) { }
    }
}