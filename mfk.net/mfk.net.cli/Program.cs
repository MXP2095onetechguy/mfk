using mxpsql.MFK.NET;
using Mono.Terminal;

LineEditor le = new LineEditor("mfk");
string line = "n=?[Initial N?],m=1,s=10";
while((line = le.Edit("mfk> ", ".demo")) != null)
{
    if(line == ".exit") break;
    if(line == ".demo") // This is the only (suppousedly) finished part as of May 11, 2026
    {
        // This is just plain fractran but for demo
        IList<Qualified> fraks = new List<Qualified>()
        {
            Fraction.From(new Numeral(455), new Numeral(33)),
            Fraction.From(new Numeral(11), new Numeral(13)),
            Fraction.From(new Numeral(1), new Numeral(11)),
            Fraction.From(new Numeral(3), new Numeral(7)),
            Fraction.From(new Numeral(11), new Numeral(2)),
            Fraction.From(new Numeral(1), new Numeral(3)),
        };

        CompiledMFKProgram prog = new CompiledMFKProgram(
            new Numeral(72),
            Numeral.Unbounded,
            new Numeral(0),
            new Dictionary<char, Named>(),
            fraks
        );

        Console.WriteLine(prog);
        Interpreter intr = new Interpreter(prog);
        foreach(var c in intr)
        {
            Console.WriteLine($"{intr.Cycle} ({intr.InstructionPointer}): {c}");
        }
        Console.WriteLine($"={intr.Current}");
        continue;
    }

    Churner churner = new Churner(line);
    try{
        churner.Parse();
        Console.WriteLine(churner.GetCompiledProgram());
    }
    catch(Exception e)
    {
        Console.WriteLine(e);
    }
}
