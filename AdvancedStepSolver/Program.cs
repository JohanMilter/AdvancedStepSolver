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
    "A = b * ((1 + r)^n - 1)/r",
    "b = (A * r)/((1 + r)^n - 1)",
    "n = log[1 + r](1 + (A * r)/b)",
};
Dictionary<string, decimal?> VariableValues = new()
{
    { "A", 1m },
    { "b", -6m },
    { "r", 3m },
    { "n", -0.5m },
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

