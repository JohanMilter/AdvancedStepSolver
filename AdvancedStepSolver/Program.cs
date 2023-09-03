using AdvancedStepSolver;
using System.Diagnostics;

Dictionary<string, (int?, bool?)> Settings = new()
{
    { "#Decimals", (null, null) },
    { "Format Formula", (null, true) },
    { "Normal/LaTeX/MathML/OMathML", (null, null) },
    { "ShowEqualSign", (null, null) },
    { "Radians", (null, null) },
};
List<string> Calculate = new()
{
    "K = K_n/((1+r)^n)",
    "r = sqrt[n](K_n/K) - 1",
    "K_n = K * (1 + r)^n",
    "n = log(K_n/K)/log(1+r)",
};
Dictionary<string, decimal?> VariableValues = new()
{
    { "K", 0.25m },
    { "K_n", 0.5m },
    { "r", 3m },
    { "n", 0.5m },
};


List<long> Counter = new();
List<string> CalcSteps;
List<string> TextSteps;
StringCalculator startCalculating = new();
for (int i_1 = 0; i_1 < Calculate.Count; i_1++)
{
    Console.WriteLine("-----------------------------------------");
    Stopwatch stopwatch = Stopwatch.StartNew();
    startCalculating.ChangeSettings(Settings);
    startCalculating.Calculate(Calculate[i_1], VariableValues);
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

