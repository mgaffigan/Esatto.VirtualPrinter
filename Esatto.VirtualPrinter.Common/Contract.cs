using System;

namespace Esatto.VirtualPrinter
{
    internal class Contract
    {
        [Serializable]
        private class ContractViolationException : Exception
        {
            public ContractViolationException() { }
            public ContractViolationException(string message) : base(message) { }
            public ContractViolationException(string message, Exception inner) : base(message, inner) { }
            protected ContractViolationException(
              System.Runtime.Serialization.SerializationInfo info,
              System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        }

        public static void Requires(bool assert, string desc)
        {
            if (!assert)
            {
                throw new ContractViolationException($"Contract violation: {desc}");
            }
        }

        public static void Assert(bool assert, string desc)
        {
            if (!assert)
            {
                throw new ContractViolationException($"Assertion failed: {desc}");
            }
        }
    }
}