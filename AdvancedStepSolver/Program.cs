using AdvancedStepSolver;
using System.Diagnostics;

Stopwatch sw = Stopwatch.StartNew();

List<string> Calculate = new()
{
    "(-3 - sqrt(3^2 - 4 * -6 * 4))/(2 * -675,86)",
    "3 * (48 + 6)",
};
for (int i = 0; i < Calculate.Count; i++)
{
    Console.WriteLine($"Calculation {i+1}-------------------------------------------------------------------------");
    List<(string, string)> LaTeX = new StringCalculator(Calculate[i], 15).CalculationStepsAndExplanation;
    foreach ((string, string) tuple in LaTeX)
    {
        //_ = new Expression2LaTeX(tuple.Item1).LaTeX;
        Console.WriteLine(new Expression2LaTeX(tuple.Item1).LaTeX);
        ////Console.WriteLine(tuple.Item1);
        Console.WriteLine(tuple.Item2);
    }
}

sw.Stop();
Console.WriteLine((float)sw.ElapsedMilliseconds / 1000 + " Seconds");
