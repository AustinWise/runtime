// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

try
{
    try
    {
        throw new Exception("inner exception");
    }
    catch (Exception ex) when (MyFilter())
    {
        System.Console.WriteLine("inner catch: SHOULD NOT CATCH");
    }
}
catch (Exception ex)
{
    System.Console.WriteLine($"outer catch: {ex.Message}");
}

System.Console.WriteLine("done");

static bool MyFilter()
{
    // This will be caught in Runtime.Base and cause the filter to return false.
    throw new Exception("Filter exception");
}
