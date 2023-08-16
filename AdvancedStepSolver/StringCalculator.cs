using System.Globalization;
using System.Text.RegularExpressions;

namespace AdvancedStepSolver;

public class StringCalculator
{
    private readonly InfoClass infoClass = new();
    public double Result;
    public string Expression = "";
    public List<string> CalcSteps = new();
    public List<string> TextSteps = new();
    private readonly int Decimals;
    public StringCalculator(string expression, Dictionary<string, (int?, bool?)> settings, int decimals)
    {
        Expression = infoClass.PreCalculationFormatting(ReplaceConstants(expression)).Replace(" ", "");
        infoClass.Settings = settings;
        Decimals = decimals;
        CalcSteps.Add(Expression);
        Result = CalculateExpression(Expression);
    }
    private double CalculateExpression(string input)
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
    private double EvaluatePostfix(Queue<string> postfixQueue)
    {
        Stack<double> valueStack = new();
        while (postfixQueue.Count > 0)
        {
            string Operator = postfixQueue.Dequeue().Replace(',', '.');
            (bool, bool) isOperator = IsOperator(Operator);
            if (double.TryParse(Operator, NumberStyles.Any, CultureInfo.InvariantCulture, out double number))
                valueStack.Push(number);
            else if (isOperator.Item1)
            {
                if (valueStack.Count < 1)
                    throw new ArgumentException("Invalid expression.");

                double a = valueStack.Pop();
                double b = 0;
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
