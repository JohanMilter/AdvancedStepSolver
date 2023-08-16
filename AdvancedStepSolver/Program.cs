using AdvancedStepSolver;
using System.Diagnostics;

Stopwatch sw = Stopwatch.StartNew();

List<string> Calculate = new()
{
    "(-2 - sqrt((-3 -5)^2 - 4 * -62 * 4))/(2 * -675)",
    "(-2 - sqrt(4))/(2 * -675)",
    "sqrt[34](23434 + 34534) * sqrt(35.23 + 4.23)",
    "sin(20+23) * sin(25+42)",
};
Dictionary<string, (int?, bool?)> Settings = new()
{
    { "Radians", (null, false) },
};

for (int i_1 = 0; i_1 < Calculate.Count; i_1++)
{
    Console.WriteLine($"Calculation {i_1+1}-------------------------------------------------------------------------");
    StringCalculator calcs = new(Calculate[i_1], Settings, 5);
    List<string> CalcSteps = calcs.CalcSteps;
    List<string> TextSteps = calcs.TextSteps;
    for (int i_2 = 0; i_2 < CalcSteps.Count; i_2++)
    {
        //Console.WriteLine(new CustomExpression2LaTeX(CalcSteps[i_2]).LaTeX);
        Console.WriteLine(CalcSteps[i_2]);
        if (i_2 < TextSteps.Count)
            Console.WriteLine(TextSteps[i_2]);
    }
}
sw.Stop();
Console.WriteLine((float)sw.ElapsedMilliseconds / 1000 + " Seconds");
