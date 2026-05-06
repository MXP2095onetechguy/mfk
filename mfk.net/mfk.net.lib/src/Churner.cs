using System;
using System.Text;
using System.Numerics;
using System.Collections.Generic;

namespace mxpsql.MFK.NET
{
    // Lexerless Parser
    // The language literally fits in one line
    // It's a FRACTRAN parser/compiler with extensions
    // Churns source code into a compiled object
    // 
    // The language design is not in just the head or the code, but an EBNF specification.
    // This spec does exist, but in a file called mfk.ebnf.
    // 
    // Error recovery is non-existent. 
    // This is intentional, as the program probably is crap if it causes the churner to error out.
    // It mostly relies on exceptions.
    public class Churner
    {
        private readonly ParsingStream pStream;
        private CompiledMFKProgram Compiled = new CompiledMFKProgram();


        public Churner(string source)
        {
            pStream = new ParsingStream(source ?? throw new ArgumentNullException(nameof(source)));
            Reset();
        }


        public void Reset()
        {
            pStream.Reset();
        }

        private void Throw(string message, Exception? Th = null)
        {
            if(Th == null) {
                throw new ParsingException(message);
            }
            else {
                throw new ParsingException(message, Th);
            }
        }

#region Parser

#region ParserQueryable
        private Queryable ParseNumeral()
        {
            BigInteger result = 0;
            // Keep reading the next characters as long as they are digits
            while (char.IsDigit(pStream.Peek()))
            {
                result = result * 10 + (pStream.Peek() - '0'); // Accumulate digits
                pStream.Advance(); // Move to the next character
            }
            return new Numeral(result);
        }
        private Queryable ParseUserInputRequest()
        {
            pStream.Expect('?'); // Expect '?'

            // Is there a prompt?
            if (pStream.Match('['))
            {
                // Collector
                StringBuilder pB = new StringBuilder();

                // Loop through characters inside the brackets until we find a closing ']'
                while (!pStream.AtEnd && pStream.Peek() != ']')
                {
                    pB.Append(pStream.Peek()); // Append the character to the prompt
                    pStream.Advance(); // Move to the next character
                }

                // Expect the closing ']'
                pStream.Expect(']');

                // Create a UserInputRequest with the parsed prompt string
                return new UserInputRequest(pB.ToString());
            }

            // No prompt?
            return new UserInputRequest(null);
        }
        private Queryable ParseRandom()
        {
            // Expect "R["
            pStream.Expect('R');
            pStream.Expect('[');

            // Parse the first integer
            BigInteger Minimum = 0;
            while (char.IsDigit(pStream.Peek()))
            {
                Minimum = Minimum * 10 + (pStream.Peek() - '0'); // Accumulate digits
                pStream.Advance(); // Move to the next character
            }

            // Match a semicolon
            pStream.Expect(';');

            // Parse the second
            BigInteger Maximum = 0;
            while (char.IsDigit(pStream.Peek()))
            {
                Maximum = Maximum * 10 + (pStream.Peek() - '0'); // Accumulate digits
                pStream.Advance(); // Move to the next character
            }

            // Expect ']'
            pStream.Expect(']');

            if(Minimum < 0 || Maximum < 0 || Minimum >= Maximum) Throw("Random Queryable.", new IllegalQueryableException($"Attempted to set an invalid Random Value ({Minimum}, {Maximum}) in a Queryable."));

            return new RandomValue(Minimum, Maximum);
        }
        private Queryable ParseQueryable(bool RandomReady=true)
        {
            char first = pStream.Peek();

            if(char.IsDigit(first)) return ParseNumeral();

            switch(first)
            {
                case '?':
                    return ParseUserInputRequest();
                case 'R':
                    if(!RandomReady) Throw("Random Queryable.", new IllegalQueryableException("Attempted to use the random number generator when not ready!"));
                    return ParseRandom();
                default:
                    Throw("Unknown Queryable found.", new IllegalQueryableException($"No known Queryable starts with a '{first}' and it is still found at {pStream.HumanIndex}."));
                    break;
            }

            Throw("Reached an unreachable section while parsing a queryable.", new NotImplementedException());
            return new Numeral(1); // unreachable
        }
#endregion

#region ParseParametrics
        private Queryable ParseInitialN()
        { 
            pStream.Expect('n');
            pStream.Expect('=');
            Queryable N = ParseQueryable(); // TODO: Replace with a ParseQueryable
            pStream.Expect(',');
            return N;
        }
        private Queryable ParseMaxIter()
        {
            Queryable MaxIter = Numeral.Unbounded;
            pStream.Expect('m');
            pStream.Expect('=');
            if(pStream.Match('i')){ // Unbounded?
                pStream.Expect('n');
                pStream.Expect('f'); 

                // By now, should have expected "inf"
                MaxIter = Numeral.Unbounded;
            } else { // it has bounds
                MaxIter = ParseQueryable(); // TODO: Replace with a ParseQueryable
            }
            pStream.Expect(',');
            return MaxIter;
        }
        private Queryable ParseSeed()
        { 
            Queryable SeedV = Numeral.Unbounded;
            pStream.Expect('s');
            pStream.Expect('=');
            if(pStream.Match('i')){ // Unbounded?
                pStream.Expect('n');
                pStream.Expect('f'); 

                // By now, should have expected "inf"
                // inf means just up to the interpreter
                SeedV = Numeral.Unbounded;
            } else { // it has a seed
                // Customize the Seed parser
                SeedV = ParseQueryable(false);
            }
            pStream.Expect(',');
            return SeedV;
        }
#endregion

        private IDictionary<char, Named> ParseFillers() {
            IDictionary<char, Named> table = new Dictionary<char, Named>();

            while(pStream.Unmatch(' '))
            {
                // Consume for a test
                continue;
            }
            pStream.Match(' ');

            return table;
        }

        public void Parse() {

            Queryable N = ParseInitialN(); // Initial N is always the first
            Queryable MaxIter = ParseMaxIter(); // MaxIter is always second
            Queryable Seed = ParseSeed(); // Seed is always third
            IDictionary<char, Named> table = ParseFillers();

            Compiled = new CompiledMFKProgram(N, MaxIter, Seed, table);
        }
#endregion


#region ParserGetter
        public CompiledMFKProgram GetCompiledProgram()
        {
            return Compiled;
        }

        public string GetSource()
        {
            return pStream.Source;
        }
#endregion
    }


    internal class ParsingStream
    {
        private readonly string _Source;
        public string Source => _Source;
#region Indexer
        public int Index { get; private set; } = 0;
        public int HumanIndex => (Index + 1);
        public bool AtEnd => (Index >= Source.Length);
#endregion

        private readonly Stack<int> _IndexStack = new Stack<int>();

        public ParsingStream(string source)
        {
            _Source = source ?? throw new ArgumentNullException(nameof(source));
        }


        public void Reset()
        {
            Index = 0;
            _IndexStack.Clear();
        }

        private void Throw(string message, Exception? Th = null)
        {
            if(Th == null) {
                throw new ParsingException(message);
            }
            else {
                throw new ParsingException(message, Th);
            }
        }


#region StreamFundamentalOps
        public bool TryPeek(out char c)
        {
            if(AtEnd) // EOL :P Not found
            {
                c = default;
                return false;
            }

            // We found something
            c = Source[Index];
            return true;
        }
        public char Peek()
        {
            if(TryPeek(out char C))
            {
                return C;
            }

            Throw("Attempted to read beyond the end of source.", new IndexOutOfRangeException($"Attempted to read beyond the end of source at {HumanIndex}."));
            return default; // unreachable
        }
        public void Advance()
        {
            if(AtEnd)
            {
                Throw("Attempted to read beyond the end of source.", new IndexOutOfRangeException($"Attempted to read beyond the end of source at {HumanIndex}."));
            }

            Index++;            
        }

        public void Mark()
        {
            _IndexStack.Push(Index);
        }
        public void Restore()
        {
            if(_IndexStack.Count == 0)
                Throw("Unable to pop the stack further to restore.", new InvalidOperationException());
            Index = _IndexStack.Pop();
        }
        public void Commit()
        {
            if(_IndexStack.Count == 0)
                Throw("No mark(s) to commit.", new InvalidOperationException());
            _IndexStack.Pop();
        }
#endregion

#region StreamOps
        public bool Match(char C)
        {
            if (TryPeek(out char actual) && actual == C)
            {
                Advance();
                return true;
            }

            return false;
        }
        public void Expect(char C)
        {
            if(!Match(C))
            {
                Throw(
                    $"Expected '{C}' but found '{(TryPeek(out char a) ? a.ToString() : "EOF")}' at position {HumanIndex}.",
                    new FormatException()
                );
            }
        }

        public bool Unmatch(char C)
        {
            if (TryPeek(out char actual) && actual != C)
            {
                Advance();
                return true;
            }

            return false;
        }
#endregion
    }
}
