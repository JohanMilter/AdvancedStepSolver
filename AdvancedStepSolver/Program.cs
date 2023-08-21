using AdvancedStepSolver.NestedClass;
using System.Diagnostics;

Dictionary<string, (int?, bool?)> Settings = new()
{
    { "#Decimals", (28, null) },
    { "LaTeX", (null, true) },
    { "ShowEqualSign", (null, true) },
    { "Radians", (null, true) },
};
List<string> Calculate = new()
{
    "S = B * (1 + r)",
    "B = S/(1 + r)",
    "r = S/B-1",
};
Dictionary<string, decimal?> VariableValues = new()
{
    { "B", 13m },
    { "r", 0.4m },
    { "S", 18.2m},
};
List<long> Counter = new();
List<string> CalcSteps = new();
List<string> TextSteps = new();
Stopwatch stopwatch = new();
NestedCalculator startCalculating = new();
for (int i_1 = 0; i_1 < Calculate.Count; i_1++)
{
    Console.WriteLine("-----------------------------------------");
    stopwatch.Start();
    startCalculating.CalculateFormula(Calculate[i_1], Settings, VariableValues);
    CalcSteps = startCalculating.CalcSteps;
    TextSteps = startCalculating.TextSteps;
    for (int i = 0; i < CalcSteps.Count; i++)
    {
        Console.WriteLine(CalcSteps[i]);
        if (TextSteps.Count > i)
            Console.WriteLine(TextSteps[i]);
    }
    stopwatch.Stop();
    Counter.Add(stopwatch.ElapsedMilliseconds);
}
foreach (var item in Counter)
    Console.WriteLine(item);

