using System;
using System.Numerics;
using System.Collections.Generic;
using System.Collections;

namespace mxpsql.MFK.NET {
    // The class that interprets the FRACTRAN from a compiled object
    // Run like an Iterator
    public class Interpreter : IEnumerable<BigInteger>, IEnumerator<BigInteger>
    {
#region SupportingTypes
        public delegate BigInteger? UserInputResolverFunc(string prompt);

        public static BigInteger? GetInputFromConsole(string prompt)
        {
            Console.Write(prompt);
            return 0;
        }
#endregion

        public BigInteger InstructionPointer { get; private set; }
        private UserInputResolverFunc UserInputResolver;
        private Random rand = new Random();

        public BigInteger Cycle { get; private set; }
        public BigInteger Current { get; private set; }
        object IEnumerator.Current => Current;

        CompiledMFKProgram prog;

        public Interpreter(CompiledMFKProgram cmp, UserInputResolverFunc uirf)
        {
            prog = cmp;
            UserInputResolver = uirf;
            Reset();
        }
        public Interpreter(CompiledMFKProgram cmp) : this(cmp, GetInputFromConsole) {}

#region Dispose
        public void Dispose() { Dispose(true); }
        private void Dispose(bool disposing = false) {}
        ~Interpreter() { Dispose(false); }
#endregion

#region ExecutionEngine

        public bool MoveNext()
        {
            throw new NotImplementedException();
        }

        private BigInteger ResolveQueryable(Queryable q, bool allowUnbounded = false)
        {
            if(q is Numeral N)
            {
                if((N == Numeral.Unbounded && N.Value == -1) && !allowUnbounded) throw new IllegalQueryableException("Found an unbounded numeral when unexpected!");
                return N.Value;
            }

            throw new IllegalQueryableException("An unknown queryable has been found!");
        }

#region Reset
        public void Reset()
        {
            this.Current = ResolveQueryable(prog.InitialN);
        }

        private void SetupRandom()
        {
            
        }
#endregion
#endregion

#region Enumeration
        public IEnumerator<BigInteger> GetEnumerator()
        {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
#endregion
    }
}
