using System;

namespace mxpsql.MFK.NET
{
    public class MFKException : Exception
    {
        public MFKException() : base() { }
        public MFKException(string message) : base(message) { }
        public MFKException(string message, Exception inner) : base(message, inner) { }
    }

    public class ParsingException : MFKException
    {
        public ParsingException() : base() { }
        public ParsingException(string message) : base(message) { }
        public ParsingException(string message, Exception inner) : base(message, inner) { }
    }


    public class IdentityException : MFKException
    {
        public IdentityException() : base() {}
        public IdentityException(string message) : base(message) {}
    }
    public class IllegalQueryableException : MFKException
    {
        public IllegalQueryableException() : base() {}
        public IllegalQueryableException(string message) : base(message) {}
    }


    public class ParametricException : MFKException
    {
        public ParametricException() : base() { }
        public ParametricException(string message) : base(message) { }
        public ParametricException(string message, Exception inner) : base(message, inner) { }
    }
}
