using mxpsql.MFK.NET;


while(true)
{
    Console.Write("> ");
    string line = Console.ReadLine() ?? "n=?[Initial N?],m=1,s=10,";
    if(line == ".exit") break;

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
