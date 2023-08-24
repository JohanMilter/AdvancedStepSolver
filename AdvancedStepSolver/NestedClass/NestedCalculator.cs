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

    //Regex Patterns Readonly
    //...
    //...
    //...

    #region Main Calculator methods
    public void CalculateFormula(string formula, Dictionary<string, (int?, bool?)> settings, Dictionary<string, decimal?> variableValues)
    {

        string? form = InsertVariablesInFormula(formula.Replace(" ", ""), variableValues, GetOperators);

        //Tjek om der er et lighedstegn og om formula er null
        if (formula.Contains('='))
        {
            bool left = false;
            string[] sides = formula.Split('=');
            foreach (KeyValuePair<string, decimal?> item in variableValues)
                if (sides[0].Replace(" ", "") == item.Key.Replace(" ", ""))
                {
                    left = true;
                    break;
                }
                else if (sides[1].Replace(" ", "") == item.Key.Replace(" ", ""))
                {
                    left = false;
                    break;
                }
            if (form is not null)
            {
                sides = form.Split('=');
                if (left)
                {
                    Variable = sides[0];
                    form = sides[1];
                }
                else
                {
                    form = sides[0];
                    Variable = sides[1];
                }
            }
        }
        CalculateExpression(form, settings);
    }
    public void CalculateExpression(string? expression, Dictionary<string, (int?, bool?)> settings)
    {
        //Reset Values
        Decimals = 0;
        Result = null;
        Expression = "";
        CalcSteps = new();
        TextSteps = new();
        string equalSign;
        if (!string.IsNullOrEmpty(expression))
        {
            //Instanciate Decimals
            settings.TryGetValue("#Decimals", out (int?, bool?) decimals);
            Decimals = decimals.Item1 ?? 3;

            //Format Expression før udregning
            Expression = PreCalculationFormatting(ReplaceConstants(expression)).Replace(" ", "");

            CalcSteps.Add(Expression);
            decimal? fullResult = Calculate(Expression, true);

            //Tjek om der skete en fejl (NaN)
            if (fullResult != null)
            {
                //Tjek om der skal lave et dynamisk lighedstegn (lighedstegnet ændree sig på baggrund af hvad resultatet er og mængden af decimaler man gern vil have).
                if (settings.TryGetValue("ShowEqualSign", out (int?, bool?) value) && value.Item2 is true)
                {
                    Result = decimal.Parse(CalculationDoneFormatting(fullResult.ToString() ?? ""));
                    decimal result = fullResult ?? 0;
                    //Dynamic lighedstegn tjekning og instanciating
                    if (double.IsNormal(double.Parse(result.ToString())))
                    {
                        bool checkIfNumber1 = decimal.TryParse(Variable, out decimal variableValue);
                        bool checkIfNumber2 = decimal.TryParse($"{Result}", out decimal formulaValue);
                        if (checkIfNumber1 && checkIfNumber2 && System.Math.Round(variableValue, Decimals) != System.Math.Round(formulaValue, Decimals))
                            equalSign = "≠";
                        else if ($"{Result}".Contains(',') && $"{result}".Contains(','))
                        {
                            int resultDecimalLength = result.ToString().Split(',')[1].Length;
                            int resultStepDecimalLength = $"{Result}".Split(',')[1].Length;

                            if (resultStepDecimalLength < resultDecimalLength)
                                equalSign = "≈";
                            else
                                equalSign = "=";
                        }
                        else
                            equalSign = "=";
                    }
                    else
                        equalSign = "=";

                    //Insert Dynamic lighedstegn
                    for (int i = 0; i < CalcSteps.Count; i++)
                        CalcSteps[i] = $"{Variable.Replace(" ", "")}{equalSign}{CalcSteps[i]}";
                }
            }
            else
            {
                CalcSteps.RemoveRange(1, CalcSteps.Count - 1);
                TextSteps.Clear();
                CalcSteps.Add("Result = NaN");
            }

            //Last Formatting - Round to specific decimals value
            CalcSteps = CalculationDoneFormatting(CalcSteps);

            //LaTeX converter tjekning og LaTeX conversion
            settings.TryGetValue("LaTeX", out (int?, bool?) convert2LaTeX);
            if (convert2LaTeX.Item2 ?? false)
                for (int i = 0; i < CalcSteps.Count; i++)
                    CalcSteps[i] = ConvertExpression(CalcSteps[i]);
        }
    }
    #endregion
    #region StringCalculator
    private decimal? Calculate(string input, bool steps)
    {
        //Replace decimal-point
        input = input.Replace(',', '.');
        Queue<string> postfixQueue = ConvertToPostfix(input);
        return EvaluatePostfix(postfixQueue, steps);
    }
    private string ReplaceConstants(string input)
    {
        //Replace the constants with their values in MathConstants
        List<(string, decimal)> ConstantList = MathConstants;
        #region Dont edit this!
        input = input.Replace(@" ", "");
        foreach ((string, decimal) constant in ConstantList)
            input = input.Replace(constant.Item1, System.Math.Round(constant.Item2, Decimals).ToString());
        return input;
        #endregion
    }
    private Queue<string> ConvertToPostfix(string expression)
    {
        //Instanciating
        Queue<string> outputQueue = new();
        Stack<string> operatorStack = new();
        Dictionary<string, int> precedence = new();
        foreach ((string, bool, int) op in GetOperators)
            precedence.Add(op.Item1, op.Item3);
        List<Regex> OperatorRegex = SearchOperatorRegex;
        string Pattern = "";
        for (int i = 0; i < OperatorRegex.Count; i++)
            Pattern += $"|{OperatorRegex[i]}";
        Regex FullOperatorRegex = new(Pattern[1..]);
        MatchCollection matches = FullOperatorRegex.Matches(expression);

        for (int i = 0; i < matches.Count; i++)
        {
            string Operator = matches[i].Value;
            // Håndter negativ operator og lav det til en Operator for sig selv
            if (Operator == "-" && (i == 0 || IsOperator(matches[i - 1].ToString()).Item1 || matches[i - 1].ToString() == "("))
            {
                outputQueue.Enqueue("-1");
                operatorStack.Push("*");
            }
            //Tjek om Operator er et tal
            else if (decimal.TryParse(Operator, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                outputQueue.Enqueue(Operator);
            //Håndter nogle parentheser 
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

        //Tjek om der er samme antal parenteser
        while (operatorStack.Count > 0)
        {
            if (operatorStack.Peek() == "(")
                throw new ArgumentException("Mismatched parentheses.");
            outputQueue.Enqueue(operatorStack.Pop());
        }

        return outputQueue;
    }
    private decimal? EvaluatePostfix(Queue<string> postfixQueue, bool steps)
    {
        //Instanciate
        Stack<decimal?> valueStack = new(); 
        string Operator;
        decimal? a, b;
        (bool, bool) isOperator;

        //Evaluate hver item i stacken en efter en
        while (postfixQueue.Count > 0)
        {
            Operator = postfixQueue.Dequeue().Replace(',', '.');
            isOperator = IsOperator(Operator);
            if (decimal.TryParse(Operator, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal number))
                valueStack.Push(number);
            else if (isOperator.Item1)
            {
                if (valueStack.Count < 1)
                    throw new ArgumentException("Invalid expression.");

                //Declaring instances
                a = valueStack.Pop();
                b = 0;
                if (!isOperator.Item2)
                    b = valueStack.Pop();

                //Calculate
                valueStack.Push(Calculator(Operator, a, b, steps, CalcSteps, TextSteps));
            }
            else
                throw new ArgumentException("Invalid operator in the expression.");
        }

        if (valueStack.Count < 1)
            throw new ArgumentException("Invalid expression.");

        return valueStack.Pop();
    }
    private (bool, bool) IsOperator(string Operator)
    {
        List<(string, bool, int)> Operators = GetOperators;
        foreach ((string, bool, int) op in Operators)
            if (op.Item1 == Operator || op.Item2 && Operator.StartsWith(op.Item1))
                return (true, op.Item2);
        return (false, false);
    }
    #endregion
    #region Engine Info
    private decimal? Calculator(string Operator, decimal? aa, decimal? bb, bool steps, List<string> CalcSteps, List<string> TextSteps)
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
                a = aa ?? 0;
                b = bb ?? 0;
                Value = (decimal)System.Math.Pow((double)b, (double)a);
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
                a = aa ?? 0;
                b = bb ?? 0;
                Value = b * a;
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
                a = aa ?? 0;
                b = bb ?? 0;
                Value = b / a;
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
                a = aa ?? 0;
                b = bb ?? 0;
                Value = b + a;
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
                a = aa ?? 0;
                b = bb ?? 0;
                Value = b - a;
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
                    a = aa ?? 0;
                    //sqrt
                    if (Operator == "sqrt")
                    {
                        Value = (decimal)System.Math.Sqrt((double)a);
                        Step[0] = $"(sqrt({a}))";
                        Step[1] = $"sqrt({a})";
                        Text = $"Kvadratroden udregnes";
                    }
                    //sqrt[n]
                    else if (Operator.StartsWith("sqrt[") && Operator.EndsWith("]"))
                    {
                        decimal baseValue = decimal.Parse(Operator[5..^1], CultureInfo.InvariantCulture); // Extract the number from "sqrt[...]"
                        Value = (decimal)System.Math.Pow((double)a, 1.0 / (double)baseValue);
                        Step[0] = $"(sqrt[{baseValue}]({a}))";
                        Step[1] = $"sqrt[{baseValue}]({a})";
                        Text = $"Roden til {baseValue} udregnes";
                    }
                }
                else if (Operator.Contains("log"))
                {
                    if (aa == null)
                        return null;
                    a = aa ?? 0;
                    //log
                    if (Operator == "log")
                    {
                        Value = (decimal)System.Math.Log10((double)a);
                        Step[0] = $"(log({a}))";
                        Step[1] = $"log({a})";
                        Text = $"Log udregnes";
                    }
                    //log[n]
                    else if (Operator.StartsWith("log[") && Operator.EndsWith("]"))
                    {
                        decimal baseValue = decimal.Parse(Operator[4..^1], CultureInfo.InvariantCulture); // Extract the number from "log[...]"
                        Value = (decimal)System.Math.Log((double)a, (double)baseValue);
                        Step[0] = $"(log[{baseValue}]({a}))";
                        Step[1] = $"log[{baseValue}]({a})";
                        Text = $"Log med basen {baseValue} udregnes";
                    }
                }
                else if (Operator.Contains("sin") || Operator.Contains("cos") || Operator.Contains("tan"))
                {
                    if (aa == null)
                        return null;
                    a = aa ?? 0;
                    Value = a;
                    //Convert from or to Radians
                    if (Settings.TryGetValue("Radians", out (int?, bool?) value) && value.Item2 == false)
                        Value = decimal.Parse(((double)Value / 180 / System.Math.PI).ToString());

                    if (Operator.Contains("sin"))
                    {
                        //sin
                        if (Operator == "sin")
                        {
                            Value = (decimal)System.Math.Sin((double)Value);
                            Step[0] = $"(sin({a}))";
                            Step[1] = $"sin({a})";
                            Text = $"Sinus udregnes";
                        }
                        //sin^(-1)
                        else if (Operator == "sin^(-1)")
                        {
                            Value = (decimal)System.Math.Asin((double)Value);
                            Step[0] = $"(sin^(-1)({a}))";
                            Step[1] = $"sin^(-1)({a})";
                            Text = $"Sinus udregnes";
                        }
                    }
                    else if (Operator.Contains("cos"))
                    {
                        //cos
                        if (Operator == "cos")
                        {
                            Value = (decimal)System.Math.Cos((double)Value);
                            Step[0] = $"(cos({a}))";
                            Step[1] = $"cos({a})";
                            Text = $"Cosinus udregnes";
                        }
                        //cos^(-1)
                        else if (Operator == "cos^(-1)")
                        {
                            Value = (decimal)System.Math.Acos((double)Value);
                            Step[0] = $"(cos^(-1)({a}))";
                            Step[1] = $"cos^(-1)({a})";
                            Text = $"Cosinus udregnes";
                        }
                    }
                    else if (Operator.Contains("tan"))
                    {
                        //tan
                        if (Operator == "tan")
                        {
                            Value = (decimal)System.Math.Tan((double)Value);
                            Step[0] = $"(tan^(-1)({a}))";
                            Step[1] = $"tan^(-1)({a})";
                            Text = $"Tangens udregnes";
                        }
                        //tan^(-1)
                        else if (Operator == "tan^(-1)")
                        {
                            Value = (decimal)System.Math.Atan((double)Value);
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
        if (steps)
        {
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
        }
        
        return Value;
    }
    // Info
    private readonly List<Regex> SearchOperatorRegex = new()
    {
        new(@"[+\-*/^()]"), // +,-,*,/,^,()
        new(@"\d+(\.\d+)?"), // Numbers: 0-9
        new(@"\bsqrt(\[.*\])?"), // sqrt[n](x) or sqrt(x)
        new(@"\blog(\[.*\])?"), // log[n](x) or log(x)
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
    private readonly Regex[] SpecialRegexParenthesis =
    {
        new(@"(\bsqrt)"),
        new(@"(\bsin)"),
        new(@"(\bcos)"),
        new(@"(\btan)"),
        new(@"(\blog)"),
    };
    private readonly List<(string, decimal)> MathConstants = new()
    {
        ("pi", (decimal)3.1415926535897932384626433832),
        ("phi", (decimal)1.6180339887498948482045868343),
        ("varphi", (decimal)1.6180339887498948482045868343),
    };
    private readonly Dictionary<string, (int?, bool?)> Settings = new();
    #endregion
    #region Formatting
    private string PreCalculationFormatting(string input)
    {
        Regex Regex;
        List<(int, int)> MatchedParenthesis;
        int where;
        int LeftParenthesis = 0;
        int RightParenthesis = 0;
        int LeftParenthesis2 = 0;
        int RightParenthesis2 = 0;
        Match match;
        MatchCollection matchCollection;
        input = input.Replace('.', ',');

        //Calculate Special Operator values
        Regex = new(@"(?<!§)\[.*\]");
        while ((match = Regex.Match(input)).Success)
        {
            decimal? CalcValue;
            string value = match.Value[1..^1];
            foreach ((string Operator, _, _) in GetOperators)
                if (value.Contains(Operator))
                {
                    CalcValue = Calculate(value, false);
                    input = input.Replace(match.Value, $"§[{CalcValue}]");
                    break;
                }
        }
        input = input.Replace("§", "");

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

        Regex = new(@"_(-?\d+(\,\d+)?)");
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
            MatchedParenthesis = FindMatchingParentheses(input, '(', ')');
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
        string fullRegex = "";
        foreach (Regex iRegex in SpecialRegexParenthesis)
            fullRegex += $"|{iRegex}";
        Regex = new(@$"(?<!\()({fullRegex[1..]})");
        Regex numberRegex = new(@"\d+(\,\d+)?");
        while ((match = Regex.Match(input)).Success)
        {
            int addInt = 0;
            where = match.Index;
            LeftParenthesis = where + match.Length;
            RightParenthesis = 0;
            if (input[LeftParenthesis] == '[')
                MatchedParenthesis = FindMatchingParentheses(input, '[', ']');
            else
                MatchedParenthesis = FindMatchingParentheses(input, '(', ')');
            foreach ((int left, int right) parMatch in MatchedParenthesis)
                if (LeftParenthesis == parMatch.left)
                    RightParenthesis = parMatch.right;

            string checkText = input.Substring(LeftParenthesis + 1, RightParenthesis - LeftParenthesis - 1);
            matchCollection = numberRegex.Matches(checkText);
            if (input[LeftParenthesis] == '(')
            {
                if (matchCollection.Count > 1)
                {
                    input = input.Insert(RightParenthesis, ")");
                    input = input.Insert(LeftParenthesis + 1, "(");
                    RightParenthesis += 2;
                }

                //Insert Around
                input = input.Insert(RightParenthesis + 1, ")");
                input = input.Insert(where, "(");
            }
            else if (input[LeftParenthesis] == '[')
            {
                if (matchCollection.Count > 1)
                {
                    input = input.Insert(RightParenthesis, ")");
                    input = input.Insert(LeftParenthesis + 1, "(");
                    addInt += 2;
                }
                LeftParenthesis2 = RightParenthesis + addInt + 1;
                MatchedParenthesis = FindMatchingParentheses(input, '(', ')');

                //Check næste parentes
                foreach ((int left, int right) parMatch in MatchedParenthesis)
                    if (LeftParenthesis2 == parMatch.left)
                        RightParenthesis2 = parMatch.right;
                checkText = input.Substring(LeftParenthesis2 + 1, RightParenthesis2 - LeftParenthesis2 - 1); 
                matchCollection = numberRegex.Matches(checkText);
                if (matchCollection.Count > 1)
                {
                    input = input.Insert(RightParenthesis2, ")");
                    input = input.Insert(LeftParenthesis2 + 1, "(");
                    RightParenthesis2 += 2;
                }

                //Insert Around
                input = input.Insert(RightParenthesis2 + 1, ")");
                input = input.Insert(where, "(");
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
        int where;
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

        //Remove UnNeededParenthesis
        Regex = new(@"(?<!§)\(\(");
        while ((match = Regex.Match(input)).Success)
        {
            LeftParenthesis = match.Index;
            LeftParenthesis2 = match.Index + 1;
            ParenthesisMatches = FindMatchingParentheses(input, '(', ')');
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
            where = matcha.Index;
            string number = matcha.Value[1..];
            input = input.Insert(where + number.Length, ")").Insert(where, "(");
        }

        //Parentes rundt om division højre side
        Regex = new(@"/(-?\d+(\,\d+)?)");
        matchCollection = Regex.Matches(input);
        foreach (Match matcha in matchCollection.Cast<Match>())
        {
            where = matcha.Index;
            string number = matcha.Value[1..];
            input = input.Insert(where + number.Length + 1, ")").Insert(where + 1, "(");
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
            if (input[where - 1] == '(')
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
    private List<string> CalculationDoneFormatting(List<string> inputs)
    {
        Regex regex = new(@"\d+\,\d+");
        MatchCollection matchCollection;
        int where;
        for (int i = 0; i < inputs.Count; i++)
        {
            matchCollection = regex.Matches(inputs[i]);
            foreach (Match match in matchCollection.Cast<Match>())
                inputs[i] = inputs[i].Replace(match.Value, System.Math.Round(decimal.Parse(match.Value), Decimals).ToString());

            inputs[i] = inputs[i].Replace('.', ',');
            //Lav om til integer hvis n.0
            regex = new(@"(\d+(\,\d+))");
            matchCollection = regex.Matches(inputs[i]);
            int substractValue = 0;
            foreach (Match item in matchCollection.Cast<Match>())
            {
                where = item.Index - substractValue;
                if (double.IsInteger(double.Parse(item.Value)))
                {
                    substractValue += item.Value.Split(',')[1].Length + 1;
                    inputs[i] = inputs[i].Remove(where, item.Length).Insert(where, $"{item.Value.Split(',')[0]}");
                }
            }
        }

        return inputs;
    }
    private string CalculationDoneFormatting(string input)
    {
        input = input.Replace('.', ',');
        Regex regex = new(@"\d+\,\d+");
        MatchCollection matchCollection;
        int where;
        matchCollection = regex.Matches(input);
        foreach (Match match in matchCollection.Cast<Match>())
            input = input.Replace(match.Value, System.Math.Round(decimal.Parse(match.Value), Decimals).ToString());

        //Lav om til integer hvis n.0
        regex = new(@"(\d+(\,\d+))");
        matchCollection = regex.Matches(input);
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
        return input;
    }
    private string AfterLaTeXFormatting(string input)
    {
        Regex Regex;
        List<(int, int)> ParenthesisMatches;
        int LeftParenthesis;
        int RightParenthesis = 0;
        input = input.Replace('.', ',');
        Match match;

        //Remove '('\sqrt(...)')' or '('\sin(...)')' or '('\cos(...)')' or '('\tan(...)')' or '('\log(...)')'
        Regex = new(@"(\(\\\bsqrt)|(\(\\\bsin)|(\(\\\bcos)|(\\\(\btan)|(\(\\\blog)");
        while ((match = Regex.Match(input)).Success)
        {
            LeftParenthesis = match.Index;
            ParenthesisMatches = FindMatchingParentheses(input, '(', ')');
            foreach ((int left, int right) parMatch in ParenthesisMatches)
                if (LeftParenthesis == parMatch.left)
                    RightParenthesis = parMatch.right;
            input = input.Remove(LeftParenthesis, 1);
            input = input.Remove(RightParenthesis - 1, 1);
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
        Match match;
        int length;
        int index;
        int rightParenthesis = 0;
        int leftParenthesis;
        int changeRightParenthesis = 0;
        int changeLeftParenthesis;


        #region frac{}{}
        if (expression.Contains('/'))
        {
            regex = new(@"\)/\(");
            while ((match = regex.Match(expression)).Success)
            {
                parenthesisMatches = FindMatchingParentheses(expression, '(', ')');
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
            foreach (Match matcha in matchCollection.Cast<Match>())
            {
                index = matcha.Index;
                leftParenthesis = index + 1;
                parenthesisMatches = FindMatchingParentheses(expression, '(', ')');
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
        #region \log{}
        if (expression.Contains("log"))
        {
            regex = new(@"\blog(\[.*\])?\(");
            while ((match = regex.Match(expression)).Success)
            {
                string specialOperatorExtensionAdd = "";
                index = match.Index;
                length = match.Length;
                if (expression[index + 3] == '[')
                {
                    changeLeftParenthesis = index + 3;
                    parenthesisMatches = FindMatchingParentheses(expression, '[', ']');
                    foreach ((int left, int right) parMatch in parenthesisMatches)
                        if (changeLeftParenthesis == parMatch.left)
                            changeRightParenthesis = parMatch.right;
                    specialOperatorExtensionAdd = "_{"+expression.Substring(changeLeftParenthesis+1, changeRightParenthesis - changeLeftParenthesis - 1)+"}";
                }
                leftParenthesis = index + length - 1;
                parenthesisMatches = FindMatchingParentheses(expression, '(', ')');

                foreach ((int left, int right) parMatch in parenthesisMatches)
                {
                    if (parMatch.left == leftParenthesis)
                    {
                        rightParenthesis = parMatch.right;
                        break;
                    }
                }
                string repalceValue = @"\log" + specialOperatorExtensionAdd;
                expression = ReplaceAtIndex(expression, (leftParenthesis, 1), "{(");
                expression = ReplaceAtIndex(expression, (rightParenthesis + 1, 1), ")}");

                expression = ReplaceAtIndex(expression, (index, length - 1), repalceValue);
            }
        }
        #endregion \log{}

        #region Symbols
        foreach ((string Symbol, string LaTeX_Symbol) in Symbols)
            expression = expression.Replace(Symbol, $"{LaTeX_Symbol} ");
        #endregion Symbols
        
        return AfterLaTeXFormatting(expression);
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
            parenthesisMatches = FindMatchingParentheses(expression, '(', ')');

            foreach ((int left, int right) parMatch in parenthesisMatches)
            {
                if (parMatch.left == leftParenthesis)
                {
                    rightParenthesis = parMatch.right;
                    break;
                }
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
    private List<(int, int)> FindMatchingParentheses(string input, char left, char right)
    {
        List<(int, int)> matchingPairs = new();
        Stack<int> stack = new();
        for (int i = 0; i < input.Length; i++)
            if (input[i] == left)
                stack.Push(i);
            else if (input[i] == right)
                if (stack.Count > 0)
                    matchingPairs.Add((stack.Pop(), i));
        return matchingPairs;
    }
    #endregion
    #region InsertVariablesInFormula
    private string? InsertVariablesInFormula(string formula, Dictionary<string, decimal?> variableValues, List<(string, bool, int)> getOperators)
    {
        variableValues = variableValues.OrderByDescending(kv => kv.Key.Length).ToDictionary(kv => kv.Key, kv => kv.Value);
        formula = ReplaceOperators(formula, false, getOperators);
        string variable = formula.Split('=')[0].Replace(" ", "");
        foreach (KeyValuePair<string, decimal?> dic in variableValues)
        {
            if (dic.Key != variable && dic.Value == null)
                return null;
            if (formula.Contains(dic.Key))
                if (!string.IsNullOrEmpty(dic.Value.ToString()))
                    formula = formula.Replace(dic.Key, dic.Value.ToString());
        }
        return ReplaceOperators(formula, true, getOperators);
    }
    private string ReplaceOperators(string formula, bool getOriginal, List<(string, bool, int)> getOperators)
    {
        if (getOriginal)
            for (int i = 0; i < getOperators.Count; i++)
                formula = formula.Replace($" §{i}§ ", getOperators[i].Item1);
        else
            for (int i = 0; i < getOperators.Count; i++)
                formula = formula.Replace(getOperators[i].Item1, $" §{i}§ ");
        return formula;
    }
    #endregion
}
