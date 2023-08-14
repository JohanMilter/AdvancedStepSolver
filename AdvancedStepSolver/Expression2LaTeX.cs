using System.Globalization;
using System.Text.RegularExpressions;

namespace AdvancedStepSolver;

public class Expression2LaTeX
{
    private readonly InfoClass infoClass = new();
    public string LaTeX = "";
    public Expression2LaTeX(string expression)
    {
        LaTeX = ConvertExpression(expression);
    }
    private string ConvertExpression(string input)
    {
        if (CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator == ".")
            input = input.Replace(',', '.').Replace(" ", "");
        else if (CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator == ",")
            input = input.Replace('.', ',').Replace(" ", "");
        Queue<string> postfixQueue = ConvertToPostfix(input);
        return EvaluatePostfix(postfixQueue);
    }
    private Queue<string> ConvertToPostfix(string expression)
    {
        #region Do not edit!
        Queue<string> outputQueue = new();
        Stack<string> operatorStack = new();

        //Add the operator, and the number which is based on where in the math hierarki, the operator is (the higher in the hierarki, the higher number).
        Dictionary<string, int> precedence = new();
        foreach ((string, bool, int) op in infoClass.GetOperators)
        {
            precedence.Add(op.Item1, op.Item3);
        }

        //Make the regex for the specific operator. Remember that it need to be dynamic, so use the '?' in the regex
        List<Regex> OperatorRegex = infoClass.SearchOperatorRegex;
        string Pattern = "";
        for (int i = 0; i < OperatorRegex.Count; i++)
            Pattern += $"|{OperatorRegex[i]}";
        Regex FullOperatorRegex = new(Pattern[1..]);

        MatchCollection matches = FullOperatorRegex.Matches(expression);
        for (int i = 0; i < matches.Count; i++)
        {
            string Operator = matches[i].Value;
            if (Operator == "-" && (i == 0 || IsOperator(matches[i - 1].ToString()).Item1 || matches[i - 1].ToString() == "("))
            {
                //Console.WriteLine("If1 = "+true);
                // Håndter negativ operator og lav det til en Operator for sig selv
                outputQueue.Enqueue($"-1");
                operatorStack.Push("*");
            }
            else if (double.TryParse(Operator, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            {
                //Console.WriteLine("If2 = " + true);
                outputQueue.Enqueue(Operator);
            }
            else if (Operator == "(")
            {
                //Console.WriteLine("If3 = " + true);
                operatorStack.Push(Operator);
            }
            else if (Operator == ")")
            {
                //Console.WriteLine("If4 = " + true);
                while (operatorStack.Count > 0 && operatorStack.Peek() != "(")
                    outputQueue.Enqueue(operatorStack.Pop());

                if (operatorStack.Count == 0 || operatorStack.Peek() != "(")
                    throw new ArgumentException("Mismatched parentheses.");

                operatorStack.Pop();
            }
            else
            {
                //Console.WriteLine("If5 = " + true);
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
    private string EvaluatePostfix(Queue<string> postfixQueue)
    {
        //foreach (var item in postfixQueue)
        //{
        //    Console.WriteLine("Item = "+item);
        //}
        Stack<string> valueStack = new();
        while (postfixQueue.Count > 0)
        {
            string Operator = postfixQueue.Dequeue();
            (bool, bool) isOperator = IsOperator(Operator);
            if (double.TryParse(Operator, NumberStyles.Any, CultureInfo.InvariantCulture, out double number))
                valueStack.Push(number.ToString());
            else if (isOperator.Item1)
            {
                if (valueStack.Count < 1)
                    throw new ArgumentException("Invalid expression.");

                string a = valueStack.Pop();
                string b = "";
                if (!isOperator.Item2)
                    b = valueStack.Pop();
                
                valueStack.Push(infoClass.Converter(Operator, a, b));
            }
            else
                throw new ArgumentException("Invalid operator in the expression.");
        }

        if (valueStack.Count < 1)
            throw new ArgumentException("Invalid expression.");

        return LastFormatting(valueStack.Pop().ToString());
    }
    private string LastFormatting(string input)
    {
        Regex regex;
        MatchCollection matchCollection;
        int LeftParenthesis;
        int RightParenthesis;
        int Amount = 0;

        //Fjern -1 \cdot
        input = input.Replace(@"1 \cdot ", "");

        //Parenthesis around negative values after \cdot
        regex = new(@"\\cdot (-\d+(\,\d+)?)");
        matchCollection = regex.Matches(input);
        foreach (Match match in matchCollection.Cast<Match>())
        {
            LeftParenthesis = match.Index + 6 + Amount;
            RightParenthesis = match.Value[6..].Length + LeftParenthesis + 1;
            input = input.Insert(LeftParenthesis, "(");
            input = input.Insert(RightParenthesis, ")");
            Amount += 2;
        }

        //Parenthesis around negative values after -
        regex = new(@"- (-\d+(\,\d+)?)");
        matchCollection = regex.Matches(input);
        Amount = 0;
        foreach (Match match in matchCollection.Cast<Match>())
        {
            LeftParenthesis = match.Index + 2 + Amount;
            RightParenthesis = match.Value[2..].Length + LeftParenthesis + 1;
            input = input.Insert(LeftParenthesis, "(");
            input = input.Insert(RightParenthesis, ")");
            Amount += 2;
        }

        //Parenthesis around negative values before ^
        regex = new(@"(-\d+(\,\d+)?)\^");
        matchCollection = regex.Matches(input);
        foreach (Match match in matchCollection.Cast<Match>())
        {
            LeftParenthesis = match.Index;
            RightParenthesis = LeftParenthesis + match.Value[..^1].Length + 1;
            input = input.Insert(LeftParenthesis, "(");
            input = input.Insert(RightParenthesis, ")");
        }
        return input;
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
}
