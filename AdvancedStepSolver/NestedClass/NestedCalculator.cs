using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AdvancedStepSolver.NestedClass;

public class NestedCalculator
{
    public decimal? Result;
    public string Expression = "";
    public string Variable = "";
    public List<string> CalcSteps = new();
    public List<string> TextSteps = new();
    private int Decimals;
    #region Main Calculator Engine
    public void CalculateFormula(string formula, Dictionary<string, (int?, bool?)> settings, Dictionary<string, decimal?> variableValues)
    {
        formula = InsertVarialbesInFormula(formula, variableValues, GetOperators);
        CalculateExpression(formula, settings);
    }
    public void CalculateExpression(string expression, Dictionary<string, (int?, bool?)> settings)
    {
        Decimals = 0;
        Result = null;
        Expression = "";
        Variable = "";
        CalcSteps = new();
        TextSteps = new();
        settings.TryGetValue("#Decimals", out (int?, bool?) decimals);
        Decimals = decimals.Item1 ?? 3;
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
        Expression = PreCalculationFormatting(ReplaceConstants(expression)).Replace(" ", "");
        settings.TryGetValue("#Decimals", out (int?, bool?) deciValue);
        if (settings.TryGetValue("ShowEqualSign", out (int?, bool?) value) && value.Item2 is true)
        {
            Decimals = 28;
            CalcSteps.Add(Expression);
            decimal? result0 = Calculate(Expression);
            CalcSteps.Clear();
            TextSteps.Clear();
            Decimals = deciValue.Item1 ?? 3;
            CalcSteps.Add(Expression);
            Result = Calculate(Expression);
            decimal result;
            if (result0 != null)
            {
                result = result0 ?? 0;
                if (!decimal.IsInteger(result) && double.IsNormal(double.Parse(result.ToString())))
                {
                    int resultDecimalLength = result.ToString().Split(',')[1].Length;
                    int resultStepLength = CalcSteps[^1].Split(',')[1].Length;
                    bool checkIfNumber1 = decimal.TryParse(Variable, out decimal variableValue);
                    bool checkIfNumber2 = decimal.TryParse(CalcSteps[^1], out decimal formulaValue);

                    if (checkIfNumber1 && checkIfNumber2 && Math.Round(variableValue, Decimals) != Math.Round(formulaValue, Decimals))
                        CalcSteps[^1] = "≠" + CalcSteps[^1];
                    else if (resultStepLength < resultDecimalLength)
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
            Result = Calculate(Expression);
        }
        Console.WriteLine("Variable = "+Variable);
        for (int i = 0; i < CalcSteps.Count; i++)
            if ((i + 1) != CalcSteps.Count)
                CalcSteps[i] = $"{Variable.Replace(" ", "")}={CalcSteps[i]}";
            else
                CalcSteps[i] = $"{Variable.Replace(" ", "")}{CalcSteps[i]}";
        settings.TryGetValue("LaTeX", out (int?, bool?) convert2LaTeX);
        if (convert2LaTeX.Item2 ?? false)
            for (int i = 0; i < CalcSteps.Count; i++)
                CalcSteps[i] = ConvertExpression(CalcSteps[i]);
    }
    #endregion
    #region StringCalculator
    private decimal? Calculate(string input)
    {
        input = input.Replace(',', '.');
        Queue<string> postfixQueue = ConvertToPostfix(input);
        return EvaluatePostfix(postfixQueue);
    }
    private string ReplaceConstants(string input)
    {
        //Here you add the constants. First the constant then the value
        List<(string, decimal)> ConstantList = MathConstants;
        #region Dont edit this!
        input = input.Replace(@" ", "");
        foreach ((string, decimal) constant in ConstantList)
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
        foreach ((string, bool, int) op in GetOperators)
            precedence.Add(op.Item1, op.Item3);

        //Make the regex for the specific operator. Remember that it need to be dynamic, so use the '?' in the regex
        List<Regex> OperatorRegex = SearchOperatorRegex;
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
                valueStack.Push(Calculator(Operator, a, b, Decimals, CalcSteps, TextSteps));
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
        List<(string, bool, int)> Operators = GetOperators;
        #region Dont edit!
        foreach ((string, bool, int) op in Operators)
            if (op.Item1 == Operator || op.Item2 && Operator.StartsWith(op.Item1))
                return (true, op.Item2);
        return (false, false);
        #endregion
    }
    #endregion
    #region Engine Info
    private decimal? Calculator(string Operator, decimal? aa, decimal? bb, int decimals, List<string> CalcSteps, List<string> TextSteps)
    {
        //Return values
        decimal? Value = 0;
        string[] Step = { "", "", "", "", "", "", "", "" };
        string Text = "";
        decimal a = 0;
        decimal b = 0;
        switch (Operator)
        {
            case "^":
                if (aa == null || bb == null)
                    return null;
                else
                {
                    a = aa ?? 0;
                    b = bb ?? 0;
                }
                if (decimals > 15)
                    decimals = 15;
                Value = Dou2Dec(Math.Round(Math.Pow((double)b, (double)a), decimals));
                Step[0] = $"(({b})^({a}))";
                Step[1] = $"(({b})^{a})";
                Step[2] = $"({b}^({a}))";
                Step[3] = $"({b}^{a})";
                Step[4] = $"({b})^({a})";
                Step[5] = $"({b})^{a}";
                Step[6] = $"{b}^({a})";
                Step[7] = $"{b}^{a}";
                Text = $"Exponenten udregnes";
                break;
            case "*":
                if (aa == null || bb == null)
                    return null;
                else
                {
                    a = aa ?? 0;
                    b = bb ?? 0;
                }
                Value = Math.Round(b * a, decimals);
                Step[0] = $"(({b})*({a}))";
                Step[1] = $"(({b})*{a})";
                Step[2] = $"({b}*({a}))";
                Step[3] = $"({b}*{a})";
                Step[4] = $"({b})*({a})";
                Step[5] = $"({b})*{a}";
                Step[6] = $"{b}*({a})";
                Step[7] = $"{b}*{a}";
                Text = $"Der ganges";
                break;
            case "/":
                if (aa == null || bb == null)
                    return null;
                else
                {
                    a = aa ?? 0;
                    b = bb ?? 0;
                }
                Value = Math.Round(b / a, decimals);
                Step[0] = $"(({b})/({a}))";
                Step[1] = $"(({b})/{a})";
                Step[2] = $"({b}/({a}))";
                Step[3] = $"({b}/{a})";
                Step[4] = $"({b})/({a})";
                Step[5] = $"({b})/{a}";
                Step[6] = $"{b}/({a})";
                Step[7] = $"{b}/{a}";
                Text = $"Der divideres";
                break;
            case "+":
                if (aa == null || bb == null)
                    return null;
                else
                {
                    a = aa ?? 0;
                    b = bb ?? 0;
                }
                Value = Math.Round(b + a, decimals);
                Step[0] = $"(({b})+({a}))";
                Step[1] = $"(({b})+{a})";
                Step[2] = $"({b}+({a}))";
                Step[3] = $"({b}+{a})";
                Step[4] = $"({b})+({a})";
                Step[5] = $"({b})+{a}";
                Step[6] = $"{b}+({a})";
                Step[7] = $"{b}+{a}";
                Text = $"Der plusses";
                break;
            case "-":
                if (aa == null || bb == null)
                    return null;
                else
                {
                    a = aa ?? 0;
                    b = bb ?? 0;
                }
                Value = Math.Round(b - a, decimals);
                Step[0] = $"(({b})-({a}))";
                Step[1] = $"(({b})-{a})";
                Step[2] = $"({b}-({a}))";
                Step[3] = $"({b}-{a})";
                Step[4] = $"({b})-({a})";
                Step[5] = $"({b})-{a}";
                Step[6] = $"{b}-({a})";
                Step[7] = $"{b}-{a}";
                Text = $"Der minuses";
                break;

            default:
                if (Operator.Contains("sqrt"))
                {
                    if (aa == null)
                        return null;
                    else
                        a = aa ?? 0;
                    if (decimals > 15)
                        decimals = 15;
                    //sqrt
                    if (Operator == "sqrt")
                    {
                        Value = Dou2Dec(Math.Round(Math.Sqrt((double)a), decimals));
                        Step[0] = $"(sqrt({a}))";
                        Step[1] = $"sqrt({a})";
                        Text = $"Kvadratroden udregnes";
                    }
                    //sqrt[n]
                    else if (Operator.StartsWith("sqrt[") && Operator.EndsWith("]"))
                    {
                        decimal baseValue = decimal.Parse(Operator[5..^1], CultureInfo.InvariantCulture); // Extract the number from "sqrt[...]"
                        Value = Dou2Dec(Math.Round(Math.Pow((double)a, 1.0 / (double)baseValue), decimals));
                        Step[0] = $"(sqrt[{baseValue}]({a}))";
                        Step[1] = $"sqrt[{baseValue}]({a})";
                        Text = $"Roden til {baseValue} udregnes";
                    }
                }
                else if (Operator.Contains("log"))
                {
                    if (aa == null)
                        return null;
                    else
                        a = aa ?? 0;
                    if (decimals > 15)
                        decimals = 15;
                    //log
                    if (Operator == "log")
                    {
                        Value = Dou2Dec(Math.Round(Math.Log10((double)a), decimals));
                        Step[0] = $"(log({a}))";
                        Step[1] = $"log({a})";
                        Text = $"Log udregnes";
                    }
                    //log[n]
                    else if (Operator.StartsWith("log_(") && Operator.EndsWith(")"))
                    {
                        decimal baseValue = decimal.Parse(Operator[5..^1], CultureInfo.InvariantCulture); // Extract the number from "log[...]"
                        Value = Dou2Dec(Math.Round(Math.Log((double)a, (double)baseValue), decimals));
                        Step[0] = $"(log_({baseValue})({a}))";
                        Step[1] = $"log_({baseValue})({a})";
                        Text = $"Log med basen {baseValue} udregnes";
                    }
                }
                else if (Operator.Contains("sin") || Operator.Contains("cos") || Operator.Contains("tan"))
                {
                    if (Operator.Contains("sin"))
                    {
                        if (aa == null)
                            return null;
                        else
                            a = aa ?? 0;
                        if (decimals > 15)
                            decimals = 15;
                        //sin
                        if (Operator == "sin")
                        {
                            Value = a;
                            if (Settings.TryGetValue("Radians", out (int?, bool?) value) && value.Item2 == false)
                                Value = decimal.Parse(((double)Value / 180 / Math.PI).ToString());
                            Value = Dou2Dec(Math.Round(Math.Sin((double)Value), decimals));
                            Step[0] = $"(sin({a}))";
                            Step[1] = $"sin({a})";
                            Text = $"Sinus udregnes";
                        }
                        //sin^(-1)
                        else if (Operator == "sin^(-1)")
                        {
                            Value = a;
                            if (Settings.TryGetValue("Radians", out (int?, bool?) value) && value.Item2 == false)
                                Value = decimal.Parse(((double)Value / 180 / Math.PI).ToString());
                            Value = Dou2Dec(Math.Round(Math.Asin((double)Value), decimals));
                            Step[0] = $"(sin^(-1)({a}))";
                            Step[1] = $"sin^(-1)({a})";
                            Text = $"Sinus udregnes";
                        }
                    }
                    else if (Operator.Contains("cos"))
                    {
                        if (aa == null)
                            return null;
                        else
                            a = aa ?? 0;
                        if (decimals > 15)
                            decimals = 15;
                        //cos
                        if (Operator == "cos")
                        {
                            Value = a;
                            if (Settings.TryGetValue("Radians", out (int?, bool?) value) && value.Item2 == false)
                                Value = decimal.Parse(((double)Value / 180 / Math.PI).ToString());
                            Value = Dou2Dec(Math.Round(Math.Cos((double)Value), decimals));
                            Step[0] = $"(cos({a}))";
                            Step[1] = $"cos({a})";
                            Text = $"Cosinus udregnes";
                        }
                        //cos^(-1)
                        else if (Operator == "cos^(-1)")
                        {
                            Value = a;
                            if (Settings.TryGetValue("Radians", out (int?, bool?) value) && value.Item2 == false)
                                Value = decimal.Parse(((double)Value / 180 / Math.PI).ToString());
                            Value = Dou2Dec(Math.Round(Math.Acos((double)Value), decimals));
                            Step[0] = $"(cos^(-1)({a}))";
                            Step[1] = $"cos^(-1)({a})";
                            Text = $"Cosinus udregnes";
                        }
                    }
                    else if (Operator.Contains("tan"))
                    {
                        if (aa == null)
                            return null;
                        else
                            a = aa ?? 0;
                        if (decimals > 15)
                            decimals = 15;
                        //tan
                        if (Operator == "tan")
                        {
                            Value = a;
                            if (Settings.TryGetValue("Radians", out (int?, bool?) value) && value.Item2 == false)
                                Value = decimal.Parse(((double)Value / 180 / Math.PI).ToString());
                            Value = Dou2Dec(Math.Round(Math.Tan((double)Value), decimals));
                            Step[0] = $"(tan^(-1)({a}))";
                            Step[1] = $"tan^(-1)({a})";
                            Text = $"Tangens udregnes";
                        }
                        //tan^(-1)
                        else if (Operator == "tan^(-1)")
                        {
                            Value = a;
                            if (Settings.TryGetValue("Radians", out (int?, bool?) value) && value.Item2 == false)
                                Value = decimal.Parse(((double)Value / 180 / Math.PI).ToString());
                            Value = Dou2Dec(Math.Round(Math.Atan((double)Value), decimals));
                            Step[0] = $"(tan^(-1)({a}))";
                            Step[1] = $"tan^(-1)({a})";
                            Text = $"Tangens udregnes";
                        }
                    }
                }
                else
                    throw new ArgumentException("Invalid operator.");
                break;
        };
        foreach (string calc in Step.Where(x => !string.IsNullOrEmpty(x)))
        {
            if (CalcSteps[^1].Contains(calc))
            {
                CalcSteps.Add(CalcSteps[^1].Replace(calc, Value.ToString()));
                TextSteps.Add(Text);
                break;
            }
        }
        //Change the first one formatting, without disturbing the rest
        if (CalcSteps.Count > 1)
            CalcSteps[^2] = AfterCalculationFormatting(CalcSteps[^2]);
        return Value;
    }
    // Info
    private readonly List<Regex> SearchOperatorRegex = new()
    {
        new(@"[+\-*/^()]"), // +,-,*,/,^,()
        new(@"\d+(\.\d+)?"), // Numbers: 0-9
        new(@"\bsqrt(\[-?\d+(\.\d+)?\])?"), // sqrt[n](x) or sqrt(x)
        new(@"\blog(_\(-?\d+(\.\d+)?\))?"), // log[n](x) or log(x)
        new(@"\bsin(\^\(-1\))?"), // sin^(-1)(x) or sin(x)
        new(@"\bcos(\^\(-1\))?"), // cos^(-1)(x) or cos(x)
        new(@"\btan(\^\(-1\))?"), // tan^(-1)(x) or tan(x)
    };
    private readonly List<(string Operator, bool MultipleFormats, int MathHierarchy)> GetOperators = new()
    {
        //Add the operator, and then a bool saying if its special, then the math-hierarchy, then what it does... (Special meaning, if theres multiple ways the operator can look like)
        ("sqrt", true, 4), //sqrt{} and sqrt[n]{}
        ("log", true, 3), //log{} and log[n]{}
        ("sin", true, 3), //sin{} and sin^{-1}{}
        ("cos", true, 3), //cos{} and cos^{-1}{}
        ("tan", true, 3), //tan{} and tan^{-1}{}
        ("^", false, 3),
        ("*", false, 2),
        ("/", false, 2),
        ("+", false, 1),
        ("-", false, 1),
    };
    private readonly List<(string, decimal)> MathConstants = new()
    {
        ("pi", (decimal)3.1415926535897932384626433832),
        ("phi", (decimal)1.6180339887498948482045868343),
        ("varphi", (decimal)1.6180339887498948482045868343),
    };
    private readonly Dictionary<string, (int?, bool?)> Settings = new()
    {
        { "Radians", (null, null) },
    };
    #endregion
    #region Converters
    private decimal? Dou2Dec(double check)
    {
        if (double.IsNormal(check))
            return decimal.Parse(check.ToString());
        else
            return null;
    }
    #endregion
    #region Formatting
    private string PreCalculationFormatting(string input)
    {
        Regex Regex;
        List<(int, int)> MatchedParenthesis;
        int where;
        int LeftParenthesis = 0;
        int RightParenthesis;
        Match match;
        MatchCollection matchCollection;
        input = input.Replace('.', ',');

        //Insert Parenthesis rundt om [-n]^, så vi sikrer os at den bliver udregnet før den bliver lagt sammen med andet, til venstre for ^
        Regex = new(@"-(\d+(\,\d+)?)\^");
        while ((match = Regex.Match(input)).Success)
        {
            where = match.Index;
            RightParenthesis = where + match.Value[..^1].Length + 1;
            input = input.Insert(where, "(");
            input = input.Insert(RightParenthesis, ")");
        }

        //Parentes rundt om efter ^
        Regex = new(@"\^(-?\d+(\,\d+)?)");
        while ((match = Regex.Match(input)).Success)
        {
            where = match.Index + 1;
            RightParenthesis = where + match.Value[..^1].Length + 1;
            input = input.Insert(where, "(");
            input = input.Insert(RightParenthesis, ")");
        }

        //Parentes rundt om efter ^ hvis calculable inside
        Regex = new(@"(?<!\))\)\^");
        Regex numbers = new(@"(-?\d+(\,\d+)?)");
        MatchCollection matchCollection1 = Regex.Matches(input);
        int count = 0;
        string checkThis;
        while ((match = Regex.Match(input)).Success && matchCollection1.Count > count)
        {
            RightParenthesis = match.Index;
            MatchedParenthesis = FindMatchingParentheses(input);
            foreach ((int, int) parMatch in MatchedParenthesis)
                if (parMatch.Item2 == RightParenthesis)
                    LeftParenthesis = parMatch.Item1 + 1;
            checkThis = input[LeftParenthesis..RightParenthesis];
            matchCollection = numbers.Matches(checkThis);
            foreach ((string, bool, int) opMatch in GetOperators)
            {
                if (checkThis.Contains(opMatch.Item1) && matchCollection.Count > 1)
                {
                    input = input.Insert(LeftParenthesis, "(");
                    input = input.Insert(RightParenthesis + 1, ")");
                    break;
                }
            }
            count++;
        }

        //Insert Parenthesis rundt om sqrt, så vi sikrer os at den bliver udregnet før den bliver lagt sammen med andet, til venstre for sqrt
        Regex = new(@"(?<!\()((\bsqrt(\[-?\d+(\,\d+)?\])?)|(\bsin(\^\(-1\))?)|(\\bcos(\^\(-1\))?)|(\btan(\^\(-1\))?))");
        while ((match = Regex.Match(input)).Success)
        {
            where = match.Index;
            LeftParenthesis = where + match.Length;
            RightParenthesis = 0;
            MatchedParenthesis = FindMatchingParentheses(input);
            foreach ((int, int) item in MatchedParenthesis)
                if (item.Item1 == LeftParenthesis)
                {
                    RightParenthesis = item.Item2;
                    break;
                }
            input = input.Insert(RightParenthesis, ")");
            input = input.Insert(where, "(");
            LeftParenthesis += 1;
            RightParenthesis += 2;
            string CheckForOperators = input.Substring(LeftParenthesis + 1, RightParenthesis - LeftParenthesis - 2);
            foreach ((string Operator, bool, int) item in GetOperators)
                if (CheckForOperators.Contains(item.Operator))
                {
                    input = input.Insert(LeftParenthesis, "(");
                    input = input.Insert(RightParenthesis, ")");
                    break;
                }
        }

        //Lav om til integer hvis n.0
        Regex = new(@"(\d+(\,\d+))");
        matchCollection = Regex.Matches(input);
        int substractValue = 0;
        foreach (Match item in matchCollection.Cast<Match>())
        {
            where = item.Index - substractValue;
            if (double.IsInteger(double.Parse(item.Value)))
            {
                substractValue += item.Value.Split(',')[1].Length + 1;
                input = input.Remove(where, item.Length).Insert(where, $"{item.Value.Split(',')[0]}");
            }
        }

        Regex = new(@"--");
        while ((match = Regex.Match(input)).Success)
        {
            where = match.Index;
            if (input[where-1] == '(')
                input = input.Remove(where, 2);
            else
                input = ReplaceAtIndex(input, (where, 2), "+");
        }

        Regex = new(@"(\+-)|(-\+)");
        while ((match = Regex.Match(input)).Success)
        {
            where = match.Index;
            if (input[where - 1] == '(')
                input = input.Remove(where, 2);
            else
                input = ReplaceAtIndex(input, (where, 2), "-");
        }
        
        return input;
    }
    private string AfterCalculationFormatting(string input)
    {
        Regex Regex;
        List<(int, int)> ParenthesisMatches;
        int LeftParenthesis;
        int LeftParenthesis2;
        int RightParenthesis = 0;
        int RightParenthesis2 = 0;
        input = input.Replace('.', ',');
        Match match;

        //Indsæt parentes rundt om negative tal efter * og -
        Regex = new(@"(\*|-)(-\d+(\,\d+)?)");
        while ((match = Regex.Match(input)).Success)
        {
            LeftParenthesis = match.Index + 1;
            RightParenthesis = match.Value.Length + LeftParenthesis;
            input = input.Insert(LeftParenthesis, "(");
            input = input.Insert(RightParenthesis, ")");
        }

        //Remove '('sqrt(...)')'
        Regex = new(@"(\(\bsqrt)|(\(\bsin)");
        while ((match = Regex.Match(input)).Success)
        {
            LeftParenthesis = match.Index;
            ParenthesisMatches = FindMatchingParentheses(input);
            foreach ((int left, int right) parMatch in ParenthesisMatches)
                if (LeftParenthesis == parMatch.left)
                    RightParenthesis = parMatch.right;
            input = input.Remove(LeftParenthesis, 1);
            input = input.Remove(RightParenthesis - 1, 1);
        }

        //Remove UnNeededParenthesis
        Regex = new(@"(?<!§)\(\(");
        while ((match = Regex.Match(input)).Success)
        {
            LeftParenthesis = match.Index;
            LeftParenthesis2 = match.Index + 1;
            ParenthesisMatches = FindMatchingParentheses(input);
            foreach ((int, int) parMatch in ParenthesisMatches)
            {
                if (LeftParenthesis == parMatch.Item1)
                    RightParenthesis = parMatch.Item2;
                else if (LeftParenthesis2 == parMatch.Item1)
                    RightParenthesis2 = parMatch.Item2;
            }
            if (LeftParenthesis + 1 == LeftParenthesis2 && RightParenthesis - 1 == RightParenthesis2)
            {
                input = input.Remove(LeftParenthesis, 1);
                input = input.Remove(RightParenthesis - 1, 1);
            }
            else
            {
                input = input.Insert(LeftParenthesis, "§");
            }
        }
        input = input.Replace("§", "");

        //Parentes rundt om division venstre side
        Regex = new(@"(-?\d+(\,\d+)?)/");
        MatchCollection matchCollection = Regex.Matches(input);
        foreach (Match matcha in matchCollection.Cast<Match>())
        {
            int where = matcha.Index;
            string number = matcha.Value[1..];
            input = input.Insert(where + number.Length, ")").Insert(where, "(");
        }

        //Parentes rundt om division højre side
        Regex = new(@"/(-?\d+(\,\d+)?)");
        matchCollection = Regex.Matches(input);
        foreach (Match matcha in matchCollection.Cast<Match>())
        {
            int where = matcha.Index;
            string number = matcha.Value[1..];
            input = input.Insert(where + number.Length + 1, ")").Insert(where + 1, "(");
        }

        return input;
    }
    #endregion
    #region LaTeX Converter
    private string ConvertExpression(string expression)
    {
        Regex regex;
        MatchCollection matchCollection;
        List<(int, int)> parenthesisMatches;
        int index;
        int rightParenthesis = 0;
        int leftParenthesis;
        int changeRightParenthesis;
        int changeLeftParenthesis;

        #region frac{}{}
        if (expression.Contains('/'))
        {
            Match match;
            regex = new(@"\)/\(");
            while ((match = regex.Match(expression)).Success)
            {
                parenthesisMatches = FindMatchingParentheses(expression);
                index = match.Index;
                rightParenthesis = index;
                leftParenthesis = index + 2;
                changeRightParenthesis = 0;
                changeLeftParenthesis = 0;
                foreach ((int left, int right) parMatch in parenthesisMatches)
                {
                    if (parMatch.left == leftParenthesis)
                        changeRightParenthesis = parMatch.right;
                    if (parMatch.right == rightParenthesis)
                        changeLeftParenthesis = parMatch.left;
                }
                expression = ReplaceAtIndex(expression, (changeLeftParenthesis, 1), @"{");
                expression = ReplaceAtIndex(expression, (rightParenthesis, 1), @"}");
                expression = ReplaceAtIndex(expression, (leftParenthesis, 1), @"{");
                expression = ReplaceAtIndex(expression, (changeRightParenthesis, 1), @"}");
                expression = expression.Remove(index + 1, 1).Insert(changeLeftParenthesis, @"\frac");
            }
        }
        #endregion frac{}{}
        #region \sqrt[n]{}
        if (expression.Contains("sqrt"))
            expression = ConvertCommands(expression, (new Regex(@"\bsqrt(\[(-?\d+(\,\d+)?)\])?\("), @"\sqrt"), false);
        #endregion \sqrt[n]{}
        #region \cdot
        if (expression.Contains('*'))
            expression = expression.Replace("*", @" \cdot ");
        #endregion \cdot
        #region n^x
        if (expression.Contains('^'))
        {
            regex = new(@"\^\(");
            matchCollection = regex.Matches(expression);
            foreach (Match match in matchCollection.Cast<Match>())
            {
                index = match.Index;
                leftParenthesis = index + 1;
                parenthesisMatches = FindMatchingParentheses(expression);
                foreach ((int, int) parMatch in parenthesisMatches)
                    if (parMatch.Item1 == leftParenthesis)
                    {
                        rightParenthesis = parMatch.Item2;
                        break;
                    }
                expression = ReplaceAtIndex(expression, (leftParenthesis, 1), "{");
                expression = ReplaceAtIndex(expression, (rightParenthesis, 1), "}");
            }
        }
        #endregion n^x
        #region \log{}
        if (expression.Contains("log"))
            expression = ConvertCommands(expression, (new Regex(@"\blog"), @"\log"), true);
        #endregion \log{}
        #region \sin{}
        if (expression.Contains("sin"))
            expression = ConvertCommands(expression, (new Regex(@"\bsin(\^{-1})?\("), @"\sin"), true);
        #endregion \sin{}
        #region \cos{}
        if (expression.Contains("cos"))
            expression = ConvertCommands(expression, (new Regex(@"\bcos(\^{-1})?\("), @"\cos"), true);
        #endregion \cos{}
        #region \tan{}
        if (expression.Contains("tan"))
            expression = ConvertCommands(expression, (new Regex(@"\btan(\^{-1})?\("), @"\tan"), true);
        #endregion \tan{}

        #region Symbols
        foreach ((string Symbol, string LaTeX_Symbol) in Symbols)
            expression = expression.Replace(Symbol, $"{LaTeX_Symbol} ");
        #endregion Symbols
        return expression;
    }
    private readonly List<(string Symbol, string LaTeX_Symbol)> Symbols = new()
    {
        ("≈", @"\approx"),
        ("≠", @"\neq"),
    };
    private string ConvertCommands(string expression, (Regex searchOpera, string replaceOpera) opera, bool keepParenthesis)
    {
        List<(int, int)> parenthesisMatches;
        int index;
        int rightParenthesis = 0;
        int leftParenthesis;
        int length;

        Match match;
        while ((match = opera.searchOpera.Match(expression)).Success)
        {
            index = match.Index;
            length = match.Length;
            leftParenthesis = index + length - 1;
            parenthesisMatches = FindMatchingParentheses(expression);

            foreach ((int left, int right) parMatch in parenthesisMatches)
            {
                if (parMatch.left == leftParenthesis)
                    rightParenthesis = parMatch.right;
            }
            if (keepParenthesis)
            {
                expression = ReplaceAtIndex(expression, (leftParenthesis, 1), "{(");
                expression = ReplaceAtIndex(expression, (rightParenthesis + 1, 1), ")}");
            }
            else
            {
                expression = ReplaceAtIndex(expression, (leftParenthesis, 1), "{");
                expression = ReplaceAtIndex(expression, (rightParenthesis, 1), "}");
            }
            expression = ReplaceAtIndex(expression, (index, opera.replaceOpera[1..].Length), opera.replaceOpera);
        }
        return expression;
    }
    private string ReplaceAtIndex(string text, (int index, int length) index, string item)
    {
        return text.Remove(index.index, index.length).Insert(index.index, item);
    }
    #endregion
    #region Parenthesis Matcher
    private List<(int, int)> FindMatchingParentheses(string input)
    {
        List<(int, int)> matchingPairs = new();
        Stack<int> stack = new();
        for (int i = 0; i < input.Length; i++)
            if (input[i] == '(')
                stack.Push(i);
            else if (input[i] == ')')
                if (stack.Count > 0)
                    matchingPairs.Add((stack.Pop(), i));
        return matchingPairs;
    }
    #endregion
    #region InsertVariablesInFormula
    private string InsertVarialbesInFormula(string formula, Dictionary<string, decimal?> variableValues, List<(string, bool, int)> getOperators)
    {
        variableValues = variableValues.OrderByDescending(kv => kv.Key.Length).ToDictionary(kv => kv.Key, kv => kv.Value);
        formula = ReplaceOperators(formula, false, getOperators);
        foreach (var item in variableValues)
            if (formula.Contains(item.Key))
                if (!string.IsNullOrEmpty(item.Value.ToString()))
                    formula = formula.Replace(item.Key, item.Value.ToString());
        return ReplaceOperators(formula, true, getOperators);
    }
    private string ReplaceOperators(string formula, bool getOriginal, List<(string, bool, int)> getOperators)
    {
        if (getOriginal)
            for (int i = 0; i < getOperators.Count; i++)
                formula = formula.Replace($"§{i}§", getOperators[i].Item1);
        else
            for (int i = 0; i < getOperators.Count; i++)
                formula = formula.Replace(getOperators[i].Item1, $"§{i}§");
        return formula;
    }
    #endregion
}
