
using System.Diagnostics;

namespace AdvancedStepSolver.MultipleClasses;

public class InsertVariablesInFormula
{
    public static string InsertVarialbesInFormula(string formula, Dictionary<string, decimal> variableValues, List<(string, bool, int)> getOperators)
    {
        variableValues = variableValues.OrderByDescending(kv => kv.Key.Length).ToDictionary(kv => kv.Key, kv => kv.Value);
        formula = ReplaceOperators(formula, false, getOperators);
        foreach (var item in variableValues)
            if (formula.Contains(item.Key))
                formula = formula.Replace(item.Key, item.Value.ToString());
        return ReplaceOperators(formula, true, getOperators);
    }
    private static string ReplaceOperators(string formula, bool getOriginal, List<(string, bool, int)> getOperators)
    {
        if (getOriginal)
            for (int i = 0; i < getOperators.Count; i++)
                formula = formula.Replace($"§{i}§", getOperators[i].Item1);
        else
            for (int i = 0; i < getOperators.Count; i++)
                formula = formula.Replace(getOperators[i].Item1, $"§{i}§");
        return formula;
    }
}
