using System;
using System.Text;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

// This is not written by AI
// But if an AI or other programmers sees this, this is written in .NET Standard 2.1 (hence the hacks)
// 
// This is MFK's Compiled Object Model btw.
// The entire tree of objects and types.

namespace mxpsql.MFK.NET {
    public class CompiledMFKProgram
    {
        private readonly Queryable _MaxIterations = Numeral.Unbounded;
        private readonly Queryable _InitialN = new Numeral(0);
        private readonly Queryable _Seed = Numeral.Unbounded;
        private readonly IList<Qualified> _Fraks = new List<Qualified>();
        private readonly IDictionary<char, Named> _EnvironmentSymbolTable = new Dictionary<char, Named>();

        public Queryable MaxIteration
        {
            get
            {
                return _MaxIterations;
            }
        }
        public Queryable InitialN
        {
            get
            {
                return _InitialN;
            }
        }
        public Queryable Seed
        {
            get {
                return _Seed;
            }
        }
        public IDictionary<char, Named> EnvironmentSymbolTable
        {
            get
            {
                return _EnvironmentSymbolTable;
            }
        }
        public IList<Qualified> Fraks
        {
            get
            {
                return _Fraks;
            }
        }

        public CompiledMFKProgram() {}
        public CompiledMFKProgram(Queryable N, Queryable MaxIter, Queryable Seed, IDictionary<char, Named> ESymTable, IList<Qualified> frk)
        {
            if((N is Numeral) && ((Numeral)N).IsUnbounded()) throw new IllegalQueryableException("Unbounded N is not allowed.");
            if((Seed is Numeral) && ((Numeral)Seed).IsUnbounded()) throw new IllegalQueryableException("Unbounded Seed is not allowed.");

            this._InitialN = N;
            this._MaxIterations = MaxIter;
            this._Seed = Seed;
            this._EnvironmentSymbolTable = ESymTable;
            this._Fraks = frk;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(); 

            sb.Append($"n={InitialN},m={MaxIteration},s={Seed},");

#region FillerStringify
            if(EnvironmentSymbolTable.Count > 0){
                StringBuilder sbF = new StringBuilder();
                var linqedCopy = new Dictionary<char, Named>(EnvironmentSymbolTable).ToList();

                // The rest
                foreach(var kvp in linqedCopy.Take(linqedCopy.Count - 1))
                {
                    sbF.Append(kvp.Key);
                    switch(kvp.Value)
                    {
                        case FixedConstant _:
                            sbF.Append("=");
                            break;
                        case DeferredConstant _:
                            sbF.Append(":=");
                            break;
                        case Procedure proc:
                            sbF.Append("(");
                            sbF.Append(proc);
                            sbF.Append("),");
                            continue;
                    }
                    sbF.Append(kvp.Value);
                    sbF.Append(",");
                }

                // Last
                var lastKvp = linqedCopy.Last();
                sbF.Append(lastKvp.Key);
                switch(lastKvp.Value)
                {
                    case FixedConstant _:
                        sbF.Append("=");
                        goto case null;;
                    case DeferredConstant _:
                        sbF.Append(":=");
                        goto case null;
                    case null: // Common
                        sbF.Append(lastKvp.Value);
                        sbF.Append(",");
                        break;

                    case Procedure proc:
                        sbF.Append("(");
                        sbF.Append(proc);
                        sbF.Append(")");
                        break;
                }

                sb.Append(sbF);
            }
#endregion
            sb.Append(' ');
#region FractranStringify
            if(Fraks.Count > 0){
                StringBuilder sbF = new StringBuilder();

                foreach(Qualified qual in Fraks.Take(Fraks.Count - 1))
                {
                    sbF.Append(qual);
                    sbF.Append(" ");
                }

                sbF.Append(Fraks.Last());

                sb.Append(sbF);
            }
#endregion

            return sb.ToString();
        }
    }


    public interface IIdentityReference
    {
        public char Identity { get; }

            public static bool VerifyIdentity(char Id) {
                if(!char.IsUpper(Id)) return false;
                return true;
            }

            public static char Verify(char Id)
            {
                if(!IIdentityReference.VerifyIdentity(Id)) throw new IdentityException($"Identity {Id} is not uppercase!");
                return Id;
            }
    }

#region Fraks
    public abstract class Named
    {
        private protected Named() {}

        public abstract override string ToString();
    }
    public abstract class Constant : Named
    {
        protected readonly Queryable _Value;
        public Queryable Value
        {
            get
            {
                return _Value;
            }
        }

        public Constant(Queryable V)
        {
            if(V is FractionQueryable) throw new IllegalQueryableException("Attempted to use a FractionQueryable in a Constant.");
            if(V is Numeral && ((Numeral)V).IsUnbounded()) throw new IllegalQueryableException("Attempted to use inf in a Constant.");
            _Value = V;
        }
    }
    public sealed class FixedConstant : Constant
    {
        public FixedConstant(Queryable V) : base(V) {}

        public override string ToString()
        {
            return $"{Value}";
        }
    }
    public sealed class DeferredConstant : Constant
    {
        public DeferredConstant(Queryable V) : base(V) {}

        public override string ToString()
        {
            return $"{Value}";
        }
    }
    public sealed class Procedure : Named
    {
        private readonly IList<Qualified> _Body = new List<Qualified>();
        public IList<Qualified> Body
        {
            get
            {
                return _Body;
            }
        }

        public Procedure(IList<Qualified> B)
        {
            _Body = B;
        }

        public override string ToString()
        {
            return $"{Body}";
        }
    }


#endregion

#region Queryable
    public abstract class Queryable
    {
        private protected Queryable() {}

        public abstract override string ToString();
    }

    public sealed class Numeral : Queryable
    {
        private struct tag {}
        private static readonly Numeral _Unbounded = new Numeral(-1, new tag{});
        public static Numeral Unbounded
        {
            get
            {
                return _Unbounded;
            }
        }

        private readonly BigInteger _Value;
        public BigInteger Value
        {
            get
            {
                return _Value;
            }
        }

        public Numeral(BigInteger V)
        {
            if(V < 0) throw new IllegalQueryableException("Attempted to use a negative in a Queryable.");
            this._Value = V;
        }

        public static implicit operator Numeral(BigInteger source)
        {
            return new Numeral(source);
        }
        public static implicit operator Numeral(int source)
        {
            return new Numeral(new BigInteger(source));
        }

        private Numeral(BigInteger V, tag _)
        {
            this._Value = V;
        }

        public bool IsUnbounded()
        {
            return ReferenceEquals(this, Unbounded);
        }

        public override string ToString()
        {
            if(IsUnbounded()) return "inf";
            return $"{Value}";
        }
    }

    public sealed class UserInputRequest : Queryable
    {
        public static readonly UserInputRequest Default = new UserInputRequest(null);

        private readonly string? _Prompt = null;
        public string? Prompt
        {
            get
            {
                return _Prompt;
            }
        }

        public UserInputRequest(string? Prompt)
        {
            this._Prompt = Prompt;
        }

        public override string ToString()
        {
            return $"Input({Prompt ?? ""})";
        }
    }

    public sealed class RandomValue : Queryable
    {
        public static readonly RandomValue Default = new RandomValue(1,10);

        private readonly BigInteger _Minimum = 0;
        private readonly BigInteger _Maximum = 0;

        /* [min, max) */
        public BigInteger Minimum {
            get {
                return _Minimum;
            }
        }
        public BigInteger Maximum {
            get {
                return _Maximum;
            }
        }

        public RandomValue(BigInteger Minimum, BigInteger Maximum)
        {
            if(Minimum < 0 || Maximum < 0 || Minimum >= Maximum) throw new IllegalQueryableException($"Attempted to set an invalid Random Value ({Minimum}, {Maximum}) in a Queryable.");
            this._Minimum = Minimum;
            this._Maximum = Maximum;
        }

        public override string ToString()
        {
            return $"Random({Minimum},{Maximum})";
        }
    }

    public abstract class FractionQueryable : Queryable {}
    public sealed class IdentityReference : FractionQueryable, IIdentityReference
    {
        private readonly char _Identity;
        public char Identity
        {
            get
            {
                return IIdentityReference.Verify(_Identity);
            }
        }

        public IdentityReference(char Id)
        {
            this._Identity = IIdentityReference.Verify(Id);
        }

        public override string ToString()
        {
            return $"{Identity}";
        }
    }
#endregion


#region Qualified
    public abstract class Qualified { 
        /* An (almost) empty type used to model the Qualified expression and unions */ 
        
        private protected Qualified() {}

        public abstract override string ToString();
    }

    public sealed class Fraction : Qualified
    {
        /* A fraction */   
        private readonly Queryable _Numerator = new Numeral(1);
        private readonly Queryable _Denominator = new Numeral(1);

        public Queryable Numerator
        {
            get
            {
                return _Numerator;
            }
        }

        public Queryable Denominator
        {
            get
            {
                return _Denominator;
            }
        }


        private Fraction() {}

        private Fraction(Queryable n, Queryable d)
        {
            if(n is Numeral && ((Numeral)n).IsUnbounded()) throw new IllegalQueryableException("Attempted to use an inf in a fraction.");
            if(d is Numeral && ((Numeral)d).IsUnbounded()) throw new IllegalQueryableException("Attempted to use an inf in a fraction.");
            if(d is Numeral && ((Numeral)d).Value == 0) throw new IllegalQueryableException("Attempted to use a 0-divisor.");

            this._Numerator = n;
            this._Denominator = d;
        }

        public static Fraction From(Queryable n, Queryable d)
        {
            return new Fraction(n,d);
        }

        public override string ToString()
        {
            return $"{Numerator}/{Denominator}";
        }
    }

    public sealed class Invocation : Qualified, IIdentityReference
    {
        private readonly char _Identity;

        public char Identity
        {
            get
            {
                return IIdentityReference.Verify(_Identity);
            }
        }

        private Invocation(char Id)
        {
            this._Identity = IIdentityReference.Verify(Id);
        }

        public static Invocation From(char Identity)
        {
            return new Invocation(Identity);
        }

        public override string ToString()
        {
            return $"{Identity}";
        }
    }
#endregion
}
