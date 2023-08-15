using AdvancedStepSolver;
using System.Diagnostics;

Stopwatch sw = Stopwatch.StartNew();

List<string> Calculate = new()
{
    "(-2 - sqrt(3^-22 - 4 * -62 * 4))/(2 * -675)",
    "(-2 - sqrt(4))/(2 * -675)",
    "sqrt[34](23434 +34534) * sqrt(35.23 + 4.23)",
    "sin(20+23) * sin(25+42)",
};
Dictionary<string, (int?, bool?)> Settings = new()
{
    { "Radians", (null, false) },
};
for (int i = 0; i < Calculate.Count; i++)
{
    Console.WriteLine($"Calculation {i+1}-------------------------------------------------------------------------");
    List<(string, string)> LaTeX = new StringCalculator(Calculate[i], Settings, 5).CalculationStepsAndExplanation;
    foreach ((string, string) tuple in LaTeX)
    {
        //_ = new Expression2LaTeX(tuple.Item1).LaTeX;
        Console.WriteLine(new CustomExpression2LaTeX(tuple.Item1).LaTeX);
        //Console.WriteLine(tuple.Item1);
        Console.WriteLine(tuple.Item2);
    }
}

sw.Stop();
Console.WriteLine((float)sw.ElapsedMilliseconds / 1000 + " Seconds");
