using System.Globalization;
using System.Text.RegularExpressions;

namespace AdvancedStepSolver.MultipleClasses;

public class StringCalculator
{
    private readonly InfoClass infoClass;
    public decimal? Result;
    public string Expression;
    public string OriginalExpression;
    public string Variable = "";
    public List<string> CalcSteps = new();
    public List<string> TextSteps = new();
    private readonly int Decimals;
    public StringCalculator(string expression, Dictionary<string, (int?, bool?)> settings, InfoClass InfoClass)
    {
        infoClass = InfoClass;
        OriginalExpression = expression;
        if (expression.Contains('='))
        {
            string[] sides = expression.Split('=');
            if (sides[0].Length < sides[1].Length)
            {
                Variable = sides[0];
                expression = sides[1];
            }
            else
            {
                expression = sides[0];
                Variable = sides[1];
            }
        }
        Expression = infoClass.PreCalculationFormatting(ReplaceConstants(expression)).Replace(" ", "");
        infoClass.Settings = settings;
        settings.TryGetValue("#Decimals", out (int?, bool?) deciValue);
        if (settings.TryGetValue("ShowEqualSign", out (int?, bool?) value) && value.Item2 is true)
        {
            Decimals = 28;
            CalcSteps.Add(Expression);
            decimal? result0 = CalculateExpression(Expression);
            CalcSteps.Clear();
            TextSteps.Clear();
            Decimals = deciValue.Item1 ?? 3;
            CalcSteps.Add(Expression);
            Result = CalculateExpression(Expression);
            decimal result;
            if (result0 != null)
            {
                result = result0 ?? 0;
                if (!decimal.IsInteger(result) && double.IsNormal(double.Parse(result.ToString())))
                {
                    int resultDecimalLength = result.ToString().Split(',')[1].Length;
                    int resultStepLength = CalcSteps[^1].Split(',')[1].Length;
                    if (resultStepLength < resultDecimalLength)
                        CalcSteps[^1] = "≈" + CalcSteps[^1];
                    else
                        CalcSteps[^1] = "=" + CalcSteps[^1];
                }
                else
                    CalcSteps[^1] = "=" + CalcSteps[^1];
            }
            else
            {
                CalcSteps.RemoveRange(1, CalcSteps.Count - 1);
                TextSteps.Clear();
                CalcSteps.Add("Result = NaN");
            }

        }
        else
        {
            Decimals = deciValue.Item1 ?? 3;
            CalcSteps.Add(Expression);
            Result = CalculateExpression(Expression);
        }
        if (OriginalExpression.Contains('='))
            for (int i = 0; i < CalcSteps.Count; i++)
                if (i + 1 != CalcSteps.Count)
                    CalcSteps[i] = $"{Variable.Replace(" ", "")}={CalcSteps[i]}";
    }
    private decimal? CalculateExpression(string input)
    {
        input = input.Replace(',', '.');
        Queue<string> postfixQueue = ConvertToPostfix(input);
        return EvaluatePostfix(postfixQueue);
    }
    private string ReplaceConstants(string input)
    {
        //Here you add the constants. First the constant then the value
        List<(string, double)> ConstantList = infoClass.MathConstants;
        #region Dont edit this!
        input = input.Replace(@" ", "");
        foreach ((string, double) constant in ConstantList)
            input = input.Replace(constant.Item1, Math.Round(constant.Item2, Decimals).ToString());
        return input;
        #endregion
    }
    private Queue<string> ConvertToPostfix(string expression)
    {
        Queue<string> outputQueue = new();
        Stack<string> operatorStack = new();

        #region Free to edit, but only for adding operators!
        //Add the operator, and the number which is based on where in the math hierarki, the operator is (the higher in the hierarki, the higher number).

        Dictionary<string, int> precedence = new();
        foreach ((string, bool, int) op in infoClass.GetOperators)
            precedence.Add(op.Item1, op.Item3);

        //Make the regex for the specific operator. Remember that it need to be dynamic, so use the '?' in the regex
        List<Regex> OperatorRegex = infoClass.SearchOperatorRegex;
        string Pattern = "";
        for (int i = 0; i < OperatorRegex.Count; i++)
            Pattern += $"|{OperatorRegex[i]}";
        Regex FullOperatorRegex = new(Pattern[1..]);
        #endregion

        #region Do not edit!
        MatchCollection matches = FullOperatorRegex.Matches(expression);
        for (int i = 0; i < matches.Count; i++)
        {
            string Operator = matches[i].Value;
            if (Operator == "-" && (i == 0 || IsOperator(matches[i - 1].ToString()).Item1 || matches[i - 1].ToString() == "("))
            {
                // Håndter negativ operator og lav det til en Operator for sig selv
                outputQueue.Enqueue("-1");
                operatorStack.Push("*");
            }
            else if (double.TryParse(Operator, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                outputQueue.Enqueue(Operator);
            else if (Operator == "(")
                operatorStack.Push(Operator);
            else if (Operator == ")")
            {
                while (operatorStack.Count > 0 && operatorStack.Peek() != "(")
                    outputQueue.Enqueue(operatorStack.Pop());

                if (operatorStack.Count == 0 || operatorStack.Peek() != "(")
                    throw new ArgumentException("Mismatched parentheses.");

                operatorStack.Pop();
            }
            else
            {
                while (operatorStack.Count > 0 && precedence.ContainsKey(operatorStack.Peek()) && precedence[Operator] <= precedence[operatorStack.Peek()])
                    outputQueue.Enqueue(operatorStack.Pop());
                operatorStack.Push(Operator);
            }
        }

        while (operatorStack.Count > 0)
        {
            if (operatorStack.Peek() == "(")
                throw new ArgumentException("Mismatched parentheses.");
            outputQueue.Enqueue(operatorStack.Pop());
        }

        return outputQueue;
        #endregion
    }
    private decimal? EvaluatePostfix(Queue<string> postfixQueue)
    {
        Stack<decimal?> valueStack = new();
        while (postfixQueue.Count > 0)
        {
            string Operator = postfixQueue.Dequeue().Replace(',', '.');
            (bool, bool) isOperator = IsOperator(Operator);
            if (decimal.TryParse(Operator, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal number))
                valueStack.Push(number);
            else if (isOperator.Item1)
            {
                if (valueStack.Count < 1)
                    throw new ArgumentException("Invalid expression.");

                decimal? a = valueStack.Pop();
                decimal? b = 0;
                if (!isOperator.Item2)
                    b = valueStack.Pop();
                #region Free to edit, but only for adding operators!
                valueStack.Push(infoClass.Calculator(Operator, a, b, Decimals, CalcSteps, TextSteps));
                #endregion Free to edit, but only for adding operators!
            }
            else
                throw new ArgumentException("Invalid operator in the expression.");
        }

        if (valueStack.Count != 1)
            throw new ArgumentException("Invalid expression.");

        return valueStack.Pop();
    }
    private (bool, bool) IsOperator(string Operator)
    {
        List<(string, bool, int)> Operators = infoClass.GetOperators;
        #region Dont edit!
        foreach ((string, bool, int) op in Operators)
            if (op.Item1 == Operator || op.Item2 && Operator.StartsWith(op.Item1))
                return (true, op.Item2);
        return (false, false);
        #endregion
    }
}
