using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace AdvancedStepSolver;

public class StringCalculator
{
    private readonly InfoClass infoClass = new();
    public double CalculationResult;
    public (double, string) CalculationResultAndExplanation = new();
    public List<(string, string)> CalculationSteps = new();
    public List<string> ExplanationSteps = new();
    public List<(string, string)> CalculationStepsAndExplanation = new();
    private readonly int Decimals;
    public StringCalculator(string input, int decimals)
    {
        //If you want to add or remove operators, remember to change these methods: ConvertToPostfix, EvaluatePostfix, GetOperators
        //If you want to add or remove constants, remember to change this method: ReplaceConstants
        Decimals = decimals;
        input = ReplaceConstants(infoClass.InsertParenthesisAround(input)).Replace(" ", "");
        CalculationResult = System.Math.Round(CalculateExpression(input), Decimals);
        for (int i = 0; i < CalculationSteps.Count; i++)
        {
            CalculationStepsAndExplanation.Add((CheckDivisionFormatting(input), ExplanationSteps[i]));
            string Calc2 = System.Math.Round(double.Parse(CalculationSteps[i].Item2), decimals).ToString();
            if (input.Contains($"({CalculationSteps[i].Item1})"))
                input = input.Replace($"({CalculationSteps[i].Item1})", Calc2);
            else
                input = input.Replace($"{CalculationSteps[i].Item1}", Calc2);
        }
        bool DidRemove = true;
        while (DidRemove)
        {
            if (!DidRemove)
                break;
            DidRemove = false;
            for (int i = 0; i < CalculationStepsAndExplanation.Count; i++)
                if (CalculationStepsAndExplanation.Count != (i + 1) && CalculationStepsAndExplanation[i].Item1 == CalculationStepsAndExplanation[i + 1].Item1)
                {
                    CalculationStepsAndExplanation.RemoveAt(i);
                    DidRemove = true;
                }
        }
        CalculationStepsAndExplanation.Add((CalculationResult.ToString(), ""));
    }
    #region CalculationResult Methods
    private string CheckDivisionFormatting(string input)
    {
        Regex Parenthesis;
        MatchCollection matchCollection;

        Parenthesis = new(@"(-?\d+(\,\d+)?)/");
        matchCollection = Parenthesis.Matches(input);
        foreach (Match match in matchCollection.Cast<Match>())
        {
            int where = match.Index;
            string number = match.Value[1..];
            input = input.Insert(where + number.Length, ")").Insert(where, "(");
        }

        Parenthesis = new(@"/(-?\d+(\,\d+)?)");
        matchCollection = Parenthesis.Matches(input);
        foreach (Match match in matchCollection.Cast<Match>())
        {
            int where = match.Index;
            string number = match.Value[1..];
            input = input.Insert(where + number.Length + 1, ")").Insert(where + 1, "(");
        }
        return input;
    }
    private double CalculateExpression(string input)
    {
        if (CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator == ".")
            input = input.Replace(',', '.');
        else if (CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator == ",")
            input = input.Replace('.', ',');
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
            input = input.Replace(constant.Item1, System.Math.Round(constant.Item2, Decimals).ToString());
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
            string Operator = postfixQueue.Dequeue();
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
                valueStack.Push(infoClass.Calculator(Operator, a, b, Decimals, CalculationSteps, ExplanationSteps));
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
            if (op.Item1 == Operator || (op.Item2 && Operator.StartsWith(op.Item1)))
                return (true, op.Item2);
        return (false, false);
        #endregion
    }
    #endregion
}
