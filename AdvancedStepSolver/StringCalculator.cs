using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Xsl;
using System.Xml;

namespace AdvancedStepSolver;

public class StringCalculator
{
    public decimal? Result;
    public string Expression = "";
    public string Variable = "";
    public List<string> CalcSteps = new();
    public List<string> TextSteps = new();
    private int Decimals;
    private bool StartNew;
    private int Format;

    #region Regex Patterns Readonly
    public StringCalculator()
    {
        string fullRegex = "";
        foreach (Regex iRegex in SpecialRegexParenthesis)
            fullRegex += $"|{iRegex}";
        FullSpecialOperatorOperatorRegex = new(@$"(?<!\()({fullRegex[1..]})");
    }
    private readonly Regex CalculateSpecialOperatorValues = new(@"(?<!§)\[");
    private readonly Regex ParenthesisAroundNegativePowOperatorLeft = new(@"-(\d+(\,\d+)?)\^");
    private readonly Regex ParenthesisAroundNegativePowOperatorRight = new(@"\^(-?\d+(\,\d+)?)");
    private readonly Regex ParenthesisAroundUndersocreValues = new(@"_(-?\d+(\,\d+)?)");
    private readonly Regex ParenthesisAroundPowIfCalculable = new(@"(?<!\))\)\^");
    private readonly Regex AllNumbers = new(@"(-?\d+(\,\d+)?)");
    private readonly Regex PositiveNumbers = new(@"\d+(\,\d+)?");
    private readonly Regex FullSpecialOperatorOperatorRegex;
    private readonly Regex SimplifyDecimals = new(@"(?<!§)(\d+(\,\d+))");
    private readonly Regex SimplifySubtractSubtract = new(@"--");
    private readonly Regex SimplifyPlusSubtract = new(@"(\+-)|(-\+)");
    private readonly Regex ParenthesisAroundNegativeAfterOperator = new(@"(\*|-)(-\d+(\,\d+)?)");
    private readonly Regex UnNeededParenthesis = new(@"(?<!§)\(\(");
    private readonly Regex ParenthesisDivisionLeft = new(@"(-?\d+(\,\d+)?)/");
    private readonly Regex ParenthesisDivisionRight = new(@"/(-?\d+(\,\d+)?)");
    private readonly Regex RemoveParenthesisAroundOperators = new(@"(\(\\\bsqrt)|(\(\\\bsin)|(\(\\\bcos)|(\(\\\btan)|(\(\\\blog)");
    private readonly Regex RemoveParenthesisAroundPositive = new(@"(?<!§)\((\d+(\,\d+)?)\)");
    private readonly Regex FormulaFormat = new(@"((<\|(\d+)\|>)\*(<\|(\d+)\|>))|((<\|(\d+)\|>)\*\()");
    #endregion

    #region Main Calculator methods
    public void ChangeSettings(Dictionary<string, (int?, bool?)> userSettings)
    {
        List<(string, (int?, bool?))> SettingsList = new();
        foreach (KeyValuePair<string, (int?, bool?)> setting in Settings)
            SettingsList.Add((setting.Key, setting.Value));
        List<(string, (int?, bool?))> userSettingsList = new();
        foreach (KeyValuePair<string, (int?, bool?)> setting in userSettings)
            userSettingsList.Add((setting.Key, setting.Value));
        Settings.Clear();
        for (int i = 0; i < SettingsList.Count; i++)
            foreach ((string, (int?, bool?)) setting in userSettingsList)
                if (SettingsList[i].Item1 == setting.Item1)
                {
                    if (setting.Item2.Item1 is not null || setting.Item2.Item2 is not null)
                        SettingsList[i] = (setting.Item1, setting.Item2);
                    break;
                }
        foreach ((string, (int?, bool?)) setting in SettingsList)
            Settings.Add(setting.Item1, setting.Item2);
    }
    public void Calculate(string formula, Dictionary<string, decimal?> variableValues)
    {
        //Debug.WriteLine("Start: CalculateFormula");
        //Reset Values
        Decimals = 0;
        Result = null;
        Expression = "";
        CalcSteps.Clear();
        TextSteps.Clear();

        variableValues = variableValues.OrderByDescending(kv => kv.Key.Length).ToDictionary(kv => kv.Key, kv => kv.Value);
        CalcSteps.Add(FormulaFormatting(formula, variableValues));
        TextSteps.Add("Tallene sættes ind i formlen");
        string? form = InsertVariablesInFormula(formula.Replace(" ", null), variableValues, GetOperators);
        //Tjek om der er et lighedstegn og om formula er null
        if (formula.Contains('='))
        {
            bool left = false;
            string[] sides = formula.Split('=');
            foreach (KeyValuePair<string, decimal?> item in variableValues)
                if (sides[0].Replace(" ", null) == item.Key.Replace(" ", null))
                {
                    left = true;
                    break;
                }
                else if (sides[1].Replace(" ", null) == item.Key.Replace(" ", null))
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
        StartNew = false;
        Calculate(form);
        //Debug.WriteLine("Exit: CalculateFormula");
    }
    public void Calculate(string? expression)
    {
        Settings.TryGetValue("Normal/LaTeX/MathML/OMathML", out (int?, bool?) convert2LaTeX);
        Format = convert2LaTeX.Item1 ?? 0;
        //Debug.WriteLine("Start: CalculateExpression");
        if (StartNew)
        {
            //Reset Values
            Decimals = 0;
            Result = null;
            Expression = "";
            CalcSteps.Clear();
            TextSteps.Clear();
        }
        string equalSign;
        if (!string.IsNullOrEmpty(expression))
        {
            //Instanciate Decimals
            Settings.TryGetValue("#Decimals", out (int?, bool?) decimals);
            Decimals = decimals.Item1 ?? 3;

            //Format Expression før udregning
            Expression = PreCalculationFormatting(ReplaceConstants(expression)).Replace(" ", null);

            CalcSteps.Add(Expression);
            decimal? fullResult = CalculateExpression(Expression, true);

            //Tjek om der skete en fejl (NaN)
            if (fullResult != null)
            {
                //Tjek om der skal lave et dynamisk lighedstegn
                //(lighedstegnet ændree sig på baggrund af hvad resultatet
                //er og mængden af decimaler man gern vil have).
                if (Settings.TryGetValue("Show Equal Sign", out (int?, bool?) value) && value.Item2 is true)
                {
                    Result = decimal.Parse(CalculationDoneFormatting(fullResult.ToString() ?? ""));
                    decimal result = fullResult ?? 0;
                    //Dynamic lighedstegn tjekning og instanciating
                    if (double.IsNormal(double.Parse(result.ToString())))
                    {
                        bool checkIfNumber1 = decimal.TryParse(Variable, out decimal variableValue);
                        bool checkIfNumber2 = decimal.TryParse($"{Result}", out decimal formulaValue);
                        if (checkIfNumber1 && checkIfNumber2 && Math.Round(variableValue, Decimals) != Math.Round(formulaValue, Decimals))
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
                    for (int i = 1; i < CalcSteps.Count; i++)
                        CalcSteps[i] = $"{Variable.Replace(" ", null)}{equalSign}{CalcSteps[i]}";
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
            if (Format is 1)
                for (int i = 0; i < CalcSteps.Count; i++)
                    CalcSteps[i] = ConvertExpression(CalcSteps[i]);
            else if (Format is 2)
            {
                for (int i = 0; i < CalcSteps.Count; i++)
                    CalcSteps[i] = ConvertExpression(CalcSteps[i]);
                CalcSteps = LaTeX2MML(CalcSteps, false, false);
                TextSteps = LaTeX2MML(TextSteps, false, true);
            }
            else if (Format is 3)
            {
                for (int i = 0; i < CalcSteps.Count; i++)
                    CalcSteps[i] = ConvertExpression(CalcSteps[i]);
                CalcSteps = LaTeX2MML(CalcSteps, true, false);
                TextSteps = LaTeX2MML(TextSteps, true, true);
            }
        }
        else
        {
            CalcSteps.Add("Insert numbers or else nothing will be calculated");
            TextSteps.Clear();
        }

        StartNew = true;
        //Debug.WriteLine("Exit: CalculateExpression");
    }
    #endregion
    #region StringCalculator
    private decimal? CalculateExpression(string input, bool steps)
    {
        //Debug.WriteLine("Start: Calculate");
        //Replace decimal-point
        input = input.Replace(',', '.');
        Queue<string> postfixQueue = ConvertToPostfix(input);
        //Debug.WriteLine("Exit: Calculate");
        return EvaluatePostfix(postfixQueue, steps);
    }
    private string ReplaceConstants(string input)
    {
        //Debug.WriteLine("Start: ReplaceConstants");
        //Replace the constants with their values in MathConstants
        List<(string, decimal)> ConstantList = MathConstants;
        input = input.Replace(@" ", "");
        foreach ((string, decimal) constant in ConstantList)
            input = input.Replace(constant.Item1, Math.Round(constant.Item2, Decimals).ToString());
        //Debug.WriteLine("Stop: ReplaceConstants");
        return input;
    }
    private Queue<string> ConvertToPostfix(string expression)
    {
        //Debug.WriteLine("Start: ConvertToPostfix");
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

        //Debug.WriteLine("Exit: ConvertToPostfix");
        return outputQueue;
    }
    private decimal? EvaluatePostfix(Queue<string> postfixQueue, bool steps)
    {
        //Debug.WriteLine("Start: EvaluatePostfix");
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

        //Debug.WriteLine("Exit: EvaluatePostfix");
        return valueStack.Pop();
    }
    private (bool, bool) IsOperator(string Operator)
    {
        //Debug.WriteLine("Start: IsOperator");
        List<(string, bool, int)> Operators = GetOperators;
        foreach ((string, bool, int) op in Operators)
            if (op.Item1 == Operator || op.Item2 && Operator.StartsWith(op.Item1))
                return (true, op.Item2);
        //Debug.WriteLine("Exit: IsOperator");
        return (false, false);
    }
    #endregion
    #region Engine Info
    private decimal? Calculator(string Operator, decimal? aa, decimal? bb, bool steps, List<string> CalcSteps, List<string> TextSteps)
    {
        //Debug.WriteLine("Start: Calculator");
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
                Value = (decimal)Math.Pow((double)b, (double)a);
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
                        Value = (decimal)Math.Sqrt((double)a);
                        Step[0] = $"(sqrt({a}))";
                        Step[1] = $"sqrt({a})";
                        Text = $"Kvadratroden udregnes";
                    }
                    //sqrt[n]
                    else if (Operator.StartsWith("sqrt[") && Operator.EndsWith("]"))
                    {
                        decimal baseValue = decimal.Parse(Operator[5..^1], CultureInfo.InvariantCulture); // Extract the number from "sqrt[...]"
                        Value = (decimal)Math.Pow((double)a, 1.0 / (double)baseValue);
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
                        Value = (decimal)Math.Log10((double)a);
                        Step[0] = $"(log({a}))";
                        Step[1] = $"log({a})";
                        Text = $"Log udregnes";
                    }
                    //log[n]
                    else if (Operator.StartsWith("log[") && Operator.EndsWith("]"))
                    {
                        decimal baseValue = decimal.Parse(Operator[4..^1], CultureInfo.InvariantCulture); // Extract the number from "log[...]"
                        Value = (decimal)Math.Log((double)a, (double)baseValue);
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
                        Value = decimal.Parse(((double)Value / 180 / Math.PI).ToString());

                    if (Operator.Contains("sin"))
                    {
                        //sin
                        if (Operator == "sin")
                        {
                            Value = (decimal)Math.Sin((double)Value);
                            Step[0] = $"(sin({a}))";
                            Step[1] = $"sin({a})";
                            Text = $"Sinus udregnes";
                        }
                        //sin^(-1)
                        else if (Operator == "sin^(-1)")
                        {
                            Value = (decimal)Math.Asin((double)Value);
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
                            Value = (decimal)Math.Cos((double)Value);
                            Step[0] = $"(cos({a}))";
                            Step[1] = $"cos({a})";
                            Text = $"Cosinus udregnes";
                        }
                        //cos^(-1)
                        else if (Operator == "cos^(-1)")
                        {
                            Value = (decimal)Math.Acos((double)Value);
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
                            Value = (decimal)Math.Tan((double)Value);
                            Step[0] = $"(tan^(-1)({a}))";
                            Step[1] = $"tan^(-1)({a})";
                            Text = $"Tangens udregnes";
                        }
                        //tan^(-1)
                        else if (Operator == "tan^(-1)")
                        {
                            Value = (decimal)Math.Atan((double)Value);
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

        //Debug.WriteLine("Exit: Calculator");
        return Value;
    }
    // Info
    private readonly List<Regex> SearchOperatorRegex = new()
    {
        new(@"[+\-*/^()]"), // +,-,*,/,^,()
        new(@"\d+(\.\d+)?"), // Numbers: 0-9
        new(@"\bsqrt(\[(-?\d+(\.\d+)?)\])?"), // sqrt[n](x) or sqrt(x)
        new(@"\blog(\[(-?\d+(\.\d+)?)\])?"), // log[n](x) or log(x)
        new(@"\bsin(\^\(-1\))?"), // sin^(-1)(x) or sin(x)
        new(@"\bcos(\^\(-1\))?"), // cos^(-1)(x) or cos(x)
        new(@"\btan(\^\(-1\))?"), // tan^(-1)(x) or tan(x)
    };
    private readonly List<(string Operator, bool MultipleFormats, int MathHierarchy)> GetOperators = new()
    {
        //Add the operator, and then a bool saying if its special,
        //then the math-hierarchy, then what it does... (Special meaning,
        //if theres multiple ways the operator can look like)
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

    private readonly Dictionary<string, (int?, bool?)> Settings = new()
    {
        { "#Decimals", (3, null) },
        { "Format Formula", (0, true) },
        { "Normal/LaTeX/MathML/OMathML", (0, null) },
        { "Show Equal Sign", (null, false) },
        { "Radians", (null, true) },
    };
    #endregion
    #region Formatting
    public string FormulaFormatting(string input, Dictionary<string, decimal?> variableValues)
    {
        //Debug.WriteLine("Start: FormulaFormatting");
        List<(int, int)> MatchedParenthesis;
        int where;
        int LeftParenthesis = 0;
        int RightParenthesis = 0;
        int LeftParenthesis2 = 0;
        int RightParenthesis2 = 0;
        Match match;
        MatchCollection matchCollection;
        input = input.Replace('.', ',').Replace(" ", null);

        input = ReplaceOperators(input, false, GetOperators);

        Dictionary<string, string> VarialbesReplace = new();
        int varCount = 0;
        foreach (var item in variableValues)
        {
            VarialbesReplace.Add(item.Key, $"<|{varCount}|>");
            input = input.Replace(item.Key, $"<|{varCount++}|>");
        }
        input = ReplaceOperators(input, true, GetOperators);

        //Lav om til integer hvis n.0
        matchCollection = SimplifyDecimals.Matches(input);
        int substractValue = 0;
        foreach (Match item in matchCollection.Cast<Match>())
        {
            where = item.Index - substractValue;
            if (decimal.IsInteger(decimal.Parse(item.Value)))
            {
                substractValue += item.Value.Split(',')[1].Length + 1;
                input = input.Remove(where, item.Length).Insert(where, $"{item.Value.Split(',')[0]}");
            }
        }

        while ((match = SimplifySubtractSubtract.Match(input)).Success)
        {
            where = match.Index;
            if (input[where - 1] == '(')
                input = input.Remove(where, 2);
            else
                input = ReplaceAtIndex(input, (where, 2), "+");
        }

        while ((match = SimplifyPlusSubtract.Match(input)).Success)
        {
            where = match.Index;
            if (input[where - 1] == '(')
                input = input.Remove(where, 2);
            else
                input = ReplaceAtIndex(input, (where, 2), "-");
        }

        //Indsæt parentes rundt om negative tal efter * og -
        while ((match = ParenthesisAroundNegativeAfterOperator.Match(input)).Success)
        {
            LeftParenthesis = match.Index + 1;
            RightParenthesis = match.Value.Length + LeftParenthesis;
            input = input.Insert(LeftParenthesis, "(");
            input = input.Insert(RightParenthesis, ")");
        }

        //Remove UnNeededParenthesis
        while ((match = UnNeededParenthesis.Match(input)).Success)
        {
            LeftParenthesis = match.Index;
            LeftParenthesis2 = match.Index + 1;
            MatchedParenthesis = FindMatchingParentheses(input, '(', ')');
            foreach ((int, int) parMatch in MatchedParenthesis)
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
        while ((match = new Regex(@"(\<\|(\d+)\|\>)\/").Match(input)).Success)
        {
            where = match.Index;
            string number = match.Value[1..];
            input = input.Insert(where + number.Length, ")").Insert(where, "(");
        }

        //Parentes rundt om division højre side
        while ((match = new Regex(@"\/(\<\|(\d+)\|\>)").Match(input)).Success)
        {
            where = match.Index;
            string number = match.Value[1..];
            input = input.Insert(where + number.Length + 1, ")").Insert(where + 1, "(");
        }

        //Insert Parenthesis rundt om sqrt, så vi sikrer os at den bliver
        //udregnet før den bliver lagt sammen med andet, til venstre for sqrt
        while ((match = FullSpecialOperatorOperatorRegex.Match(input)).Success)
        {
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

            if (input[LeftParenthesis] == '(')
            {
                //Insert Around
                input = input.Insert(RightParenthesis + 1, ")");
                input = input.Insert(where, "(");
            }
            else if (input[LeftParenthesis] == '[')
            {
                LeftParenthesis2 = RightParenthesis + 1;
                MatchedParenthesis = FindMatchingParentheses(input, '(', ')');

                //Check næste parentes
                foreach ((int left, int right) parMatch in MatchedParenthesis)
                    if (LeftParenthesis2 == parMatch.left)
                        RightParenthesis2 = parMatch.right;

                //Insert Around
                input = input.Insert(RightParenthesis2 + 1, ")");
                input = input.Insert(where, "(");
            }
        }

        if (Settings.TryGetValue("Format Formula", out (int?, bool?) format) && format.Item2 == true)
            while ((match = FormulaFormat.Match(input)).Success)
                input = input.Replace(match.Value, match.Value.Replace("*", ""));

        //Replace Variables
        varCount = 0;
        foreach (var item in variableValues)
            input = input.Replace($"<|{varCount++}|>", item.Key);

        //Debug.WriteLine("Stop: FormulaFormatting");
        return input;
    }
    private string PreCalculationFormatting(string input)
    {
        //Debug.WriteLine("Start: PreCalculationFormatting");
        List<(int, int)> MatchedParenthesis;
        int LeftParenthesis = 0;
        int RightParenthesis = 0;
        int LeftParenthesis2 = 0;
        int RightParenthesis2 = 0;
        MatchCollection matchCollection;
        Match match;
        int where;
        input = input.Replace('.', ',');

        //Calculate Special Operator values
        while ((match = CalculateSpecialOperatorValues.Match(input)).Success)
        {
            decimal? CalcValue;
            LeftParenthesis = match.Index;
            MatchedParenthesis = FindMatchingParentheses(input, '[', ']');
            foreach ((int leftPar, int rightPar) in MatchedParenthesis)
                if (LeftParenthesis == leftPar)
                    RightParenthesis = rightPar;

            string value = input.Substring(LeftParenthesis, RightParenthesis - LeftParenthesis + 1);
            foreach ((string Operator, _, _) in GetOperators)
                if (value.Contains(Operator))
                {
                    CalcSteps.Add(AfterCalculationFormatting(input));
                    TextSteps.Add("Udregner operator tal");
                    CalcValue = CalculateExpression(value[1..^1], false);
                    input = input.Replace(value, $"§[{CalcValue}]");
                    break;
                }
            input = input.Insert(LeftParenthesis, "§");
        }
        input = input.Replace("§", "");

        //Insert Parenthesis rundt om [-n]^, så vi sikrer os at den bliver
        //udregnet før den bliver lagt sammen med andet, til venstre for ^
        while ((match = ParenthesisAroundNegativePowOperatorLeft.Match(input)).Success)
        {
            where = match.Index;
            RightParenthesis = where + match.Value[..^1].Length + 1;
            input = input.Insert(where, "(");
            input = input.Insert(RightParenthesis, ")");
        }

        //Parentes rundt om efter ^
        while ((match = ParenthesisAroundNegativePowOperatorRight.Match(input)).Success)
        {
            where = match.Index + 1;
            RightParenthesis = where + match.Value[..^1].Length + 1;
            input = input.Insert(where, "(");
            input = input.Insert(RightParenthesis, ")");
        }

        //Parenthesis around undercore values
        while ((match = ParenthesisAroundUndersocreValues.Match(input)).Success)
        {
            where = match.Index + 1;
            RightParenthesis = where + match.Value[..^1].Length + 1;
            input = input.Insert(where, "(");
            input = input.Insert(RightParenthesis, ")");
        }

        //Parentes rundt om efter ^ hvis calculable inside
        MatchCollection matchCollection1 = ParenthesisAroundPowIfCalculable.Matches(input);
        int count = 0;
        string checkThis;
        while ((match = ParenthesisAroundPowIfCalculable.Match(input)).Success && matchCollection1.Count > count)
        {
            RightParenthesis = match.Index;
            MatchedParenthesis = FindMatchingParentheses(input, '(', ')');
            foreach ((int, int) parMatch in MatchedParenthesis)
                if (parMatch.Item2 == RightParenthesis)
                    LeftParenthesis = parMatch.Item1 + 1;
            checkThis = input[LeftParenthesis..RightParenthesis];
            matchCollection = AllNumbers.Matches(checkThis);
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

        //Insert Parenthesis rundt om sqrt, så vi sikrer os at den bliver
        //udregnet før den bliver lagt sammen med andet, til venstre for sqrt
        while ((match = FullSpecialOperatorOperatorRegex.Match(input)).Success)
        {
            int addInt = 0;
            where = match.Index;
            LeftParenthesis = where + match.Length;
            if (input[LeftParenthesis] == '[')
                MatchedParenthesis = FindMatchingParentheses(input, '[', ']');
            else
                MatchedParenthesis = FindMatchingParentheses(input, '(', ')');
            foreach ((int left, int right) parMatch in MatchedParenthesis)
                if (LeftParenthesis == parMatch.left)
                    RightParenthesis = parMatch.right;

            string checkText = input.Substring(LeftParenthesis + 1, RightParenthesis - LeftParenthesis - 1);
            matchCollection = PositiveNumbers.Matches(checkText);
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
                matchCollection = PositiveNumbers.Matches(checkText);
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
        //Make this a while loop!
        matchCollection = SimplifyDecimals.Matches(input);
        int substractValue = 0;
        foreach (Match item in matchCollection.Cast<Match>())
        {
            where = item.Index - substractValue;
            if (decimal.IsInteger(decimal.Parse(item.Value)))
            {
                substractValue += item.Value.Split(',')[1].Length + 1;
                input = input.Remove(where, item.Length).Insert(where, $"{item.Value.Split(',')[0]}");
            }
        }

        while ((match = SimplifySubtractSubtract.Match(input)).Success)
        {
            where = match.Index;
            if (input[where - 1] == '(')
                input = input.Remove(where, 2);
            else
                input = ReplaceAtIndex(input, (where, 2), "+");
        }

        while ((match = SimplifyPlusSubtract.Match(input)).Success)
        {
            where = match.Index;
            if (input[where - 1] == '(')
                input = input.Remove(where, 2);
            else
                input = ReplaceAtIndex(input, (where, 2), "-");
        }

        //Debug.WriteLine("Stop: PreCalculationFormatting");
        return input;
    }
    private string AfterCalculationFormatting(string input)
    {
        //Debug.WriteLine("Start: AfterCalculationFormatting");
        List<(int, int)> ParenthesisMatches;
        int LeftParenthesis;
        int LeftParenthesis2;
        int RightParenthesis = 0;
        int RightParenthesis2 = 0;
        string value;
        int length;
        int where;
        bool okay;
        input = input.Replace('.', ',');
        Match match;
        char[] SpecialOperatorsLastChar = SpecialRegexParenthesis.Select(x => x.ToString()[..^1].Last()).ToArray();

        //Indsæt parentes rundt om negative tal efter * og -
        while ((match = ParenthesisAroundNegativeAfterOperator.Match(input)).Success)
        {
            LeftParenthesis = match.Index + 1;
            RightParenthesis = match.Value.Length + LeftParenthesis;
            input = input.Insert(LeftParenthesis, "(");
            input = input.Insert(RightParenthesis, ")");
        }

        //Remove UnNeededParenthesis
        while ((match = UnNeededParenthesis.Match(input)).Success)
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
        while ((match = ParenthesisDivisionLeft.Match(input)).Success)
        {
            where = match.Index;
            string number = match.Value[1..];
            input = input.Insert(where + number.Length, ")").Insert(where, "(");
        }

        //Parentes rundt om division højre side
        while ((match = ParenthesisDivisionRight.Match(input)).Success)
        {
            where = match.Index;
            string number = match.Value[1..];
            input = input.Insert(where + number.Length + 1, ")").Insert(where + 1, "(");
        }

        while ((match = RemoveParenthesisAroundPositive.Match(input)).Success)
        {
            LeftParenthesis = match.Index;
            RightParenthesis = LeftParenthesis + match.Length - 1;
            if ((LeftParenthesis - 1 < 0 || input[LeftParenthesis - 1] != '/' && input[LeftParenthesis - 1] != ']') && RightParenthesis + 1 < input.Length && input[RightParenthesis + 1] != '/')
            {
                okay = true;
                foreach (char item in SpecialOperatorsLastChar)
                    if (input[LeftParenthesis - 1] == item)
                    {
                        okay = false;
                        break;
                    }

                if (okay)
                {
                    input = input.Remove(RightParenthesis, 1);
                    input = input.Remove(LeftParenthesis, 1);
                }
                else
                    input = input.Insert(LeftParenthesis, "§");
            }
            else
                input = input.Insert(LeftParenthesis, "§");

        }
        input = input.Replace("§", "");

        //Lav om til integer hvis n.0
        while ((match = SimplifyDecimals.Match(input)).Success)
        {
            length = match.Length;
            value = match.Value;
            where = match.Index;
            if (decimal.IsInteger(decimal.Parse(value)))
                input = input.Remove(where, length).Insert(where, $"{value.Split(',')[0]}");
            input = input.Insert(where, "§");
        }
        input = input.Replace("§", "");

        while ((match = SimplifySubtractSubtract.Match(input)).Success)
        {
            where = match.Index;
            if (input[where - 1] == '(')
                input = input.Remove(where, 2);
            else
                input = ReplaceAtIndex(input, (where, 2), "+");
        }

        while ((match = SimplifyPlusSubtract.Match(input)).Success)
        {
            where = match.Index;
            if (input[where - 1] == '(')
                input = input.Remove(where, 2);
            else
                input = ReplaceAtIndex(input, (where, 2), "-");
        }

        //Debug.WriteLine("Stop: AfterCalculationFormatting");
        return input;
    }
    private List<string> CalculationDoneFormatting(List<string> inputs)
    {
        //Debug.WriteLine("Start: CalculationDoneFormatting");
        MatchCollection matchCollection;
        int where;
        for (int i = 0; i < inputs.Count; i++)
        {
            matchCollection = SimplifyDecimals.Matches(inputs[i]);
            foreach (Match match in matchCollection.Cast<Match>())
                inputs[i] = inputs[i].Replace(match.Value, Math.Round(decimal.Parse(match.Value), Decimals).ToString());

            inputs[i] = inputs[i].Replace('.', ',');
            //Lav om til integer hvis n.0
            matchCollection = SimplifyDecimals.Matches(inputs[i]);
            int substractValue = 0;
            foreach (Match item in matchCollection.Cast<Match>())
            {
                where = item.Index - substractValue;
                if (decimal.IsInteger(decimal.Parse(item.Value)))
                {
                    substractValue += item.Value.Split(',')[1].Length + 1;
                    inputs[i] = inputs[i].Remove(where, item.Length).Insert(where, $"{item.Value.Split(',')[0]}");
                }
            }
        }

        //Debug.WriteLine("Stop: CalculationDoneFormatting");
        return inputs;
    }
    private string CalculationDoneFormatting(string input)
    {
        //Debug.WriteLine("Start: CalculationDoneFormatting");
        input = input.Replace('.', ',');
        MatchCollection matchCollection;
        int where;
        matchCollection = SimplifyDecimals.Matches(input);
        foreach (Match match in matchCollection.Cast<Match>())
            input = input.Replace(match.Value, Math.Round(decimal.Parse(match.Value), Decimals).ToString());

        //Lav om til integer hvis n.0
        matchCollection = SimplifyDecimals.Matches(input);
        int substractValue = 0;
        foreach (Match item in matchCollection.Cast<Match>())
        {
            where = item.Index - substractValue;
            if (decimal.IsInteger(decimal.Parse(item.Value)))
            {
                substractValue += item.Value.Split(',')[1].Length + 1;
                input = input.Remove(where, item.Length).Insert(where, $"{item.Value.Split(',')[0]}");
            }
        }

        //Debug.WriteLine("Stop: CalculationDoneFormatting");
        return input;
    }
    private string AfterLaTeXFormatting(string input)
    {
        //Debug.WriteLine("Start: AfterLaTeXFormatting");
        List<(int, int)> ParenthesisMatches;
        int LeftParenthesis;
        int RightParenthesis = 0;
        input = input.Replace('.', ',');
        Match match;

        //Remove '('\sqrt(...)')' or '('\sin(...)')' or '('\cos(...)')' or '('\tan(...)')' or '('\log(...)')'
        while ((match = RemoveParenthesisAroundOperators.Match(input)).Success)
        {
            LeftParenthesis = match.Index;
            ParenthesisMatches = FindMatchingParentheses(input, '(', ')');
            foreach ((int left, int right) parMatch in ParenthesisMatches)
                if (LeftParenthesis == parMatch.left)
                    RightParenthesis = parMatch.right;
            input = input.Remove(LeftParenthesis, 1);
            input = input.Remove(RightParenthesis - 1, 1);
        }

        //Debug.WriteLine("Stop: AfterLaTeXFormatting");
        return input;
    }
    #endregion
    #region LaTeX Converter
    public string ConvertExpression(string expression)
    {
        //Debug.WriteLine("Start: ConvertExpression");
        Regex regex;
        MatchCollection matchCollection;
        List<(int, int)> parenthesisMatches;
        Match match;
        string value;
        int length;
        int index;
        int rightParenthesis = 0;
        int leftParenthesis;
        int RightParenthesis2 = 0;
        int LeftParenthesis2;
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
                RightParenthesis2 = 0;
                LeftParenthesis2 = 0;
                foreach ((int left, int right) parMatch in parenthesisMatches)
                {
                    if (parMatch.left == leftParenthesis)
                        RightParenthesis2 = parMatch.right;
                    if (parMatch.right == rightParenthesis)
                        LeftParenthesis2 = parMatch.left;
                }
                expression = ReplaceAtIndex(expression, (LeftParenthesis2, 1), @"{");
                expression = ReplaceAtIndex(expression, (rightParenthesis, 1), @"}");
                expression = ReplaceAtIndex(expression, (leftParenthesis, 1), @"{");
                expression = ReplaceAtIndex(expression, (RightParenthesis2, 1), @"}");
                expression = expression.Remove(index + 1, 1).Insert(LeftParenthesis2, @"\frac");
            }
        }
        #endregion frac{}{}
        #region \sqrt[n]{}
        if (expression.Contains("sqrt"))
        {
            regex = new(@"(?<!\\)\bsqrt(\[)?");
            while ((match = regex.Match(expression)).Success)
            {
                value = match.Value;
                length = match.Length;
                index = match.Index;
                leftParenthesis = index + length - 1;

                if (value.Last() == '[')
                {
                    //Find Specialoperator value
                    parenthesisMatches = FindMatchingParentheses(expression, '[', ']');
                    foreach ((int left, int right) parMatch in parenthesisMatches)
                        if (leftParenthesis == parMatch.left)
                        {
                            rightParenthesis = parMatch.right;
                            break;
                        }

                    //Find parenthesis
                    LeftParenthesis2 = rightParenthesis + 1;
                    parenthesisMatches = FindMatchingParentheses(expression, '(', ')');
                    foreach ((int left, int right) parMatch in parenthesisMatches)
                        if (LeftParenthesis2 == parMatch.left)
                        {
                            RightParenthesis2 = parMatch.right;
                            break;
                        }

                    expression = ReplaceAtIndex(expression, (LeftParenthesis2, 1), "{");
                    expression = ReplaceAtIndex(expression, (RightParenthesis2, 1), "}");
                }
                else
                {
                    //Find parenthesis
                    parenthesisMatches = FindMatchingParentheses(expression, '(', ')');
                    foreach ((int left, int right) parMatch in parenthesisMatches)
                        if (leftParenthesis == parMatch.left)
                        {
                            rightParenthesis = parMatch.right;
                            break;
                        }

                    expression = ReplaceAtIndex(expression, (leftParenthesis, 1), "{");
                    expression = ReplaceAtIndex(expression, (rightParenthesis, 1), "}");
                }
                expression = expression.Insert(index, @"\");
            }
        }
        #endregion \sqrt[n]{}
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
            regex = new(@"(?<!\\)\blog");
            while ((match = regex.Match(expression)).Success)
            {
                index = match.Index;
                length = match.Length;
                leftParenthesis = index + length;
                if (expression[leftParenthesis] == '[')
                {
                    parenthesisMatches = FindMatchingParentheses(expression, '[', ']');
                    foreach ((int left, int right) in parenthesisMatches)
                        if (leftParenthesis == left)
                        {
                            rightParenthesis = right;
                            break;
                        }
                    string checkText = expression.Substring(leftParenthesis, rightParenthesis - leftParenthesis + 1);
                    expression = ReplaceAtIndex(expression, (leftParenthesis, checkText.Length), $"_{{{checkText[1..^1]}}}");

                    LeftParenthesis2 = rightParenthesis + 2;
                    parenthesisMatches = FindMatchingParentheses(expression, '(', ')');
                    foreach ((int left, int right) in parenthesisMatches)
                        if (LeftParenthesis2 == left)
                        {
                            RightParenthesis2 = right;
                            break;
                        }
                    expression = ReplaceAtIndex(expression, (RightParenthesis2, 1), ")}");
                    expression = ReplaceAtIndex(expression, (LeftParenthesis2, 1), "{(");
                    expression = expression.Insert(index, @"\");
                }
                else
                {
                    parenthesisMatches = FindMatchingParentheses(expression, '(', ')');
                    foreach ((int left, int right) in parenthesisMatches)
                        if (leftParenthesis == left)
                        {
                            rightParenthesis = right;
                            break;
                        }
                    expression = ReplaceAtIndex(expression, (rightParenthesis, 1), ")}");
                    expression = ReplaceAtIndex(expression, (leftParenthesis, 1), "{(");
                    expression = expression.Insert(index, @"\");
                }
            }
        }
        #endregion \log{}

        #region Symbols
        foreach ((string Symbol, string LaTeX_Symbol) in Symbols)
            expression = expression.Replace(Symbol, $"{LaTeX_Symbol} ");
        #endregion Symbols

        //Debug.WriteLine("Stop: ConvertExpression");
        return AfterLaTeXFormatting(expression);
    }
    private readonly List<(string Symbol, string LaTeX_Symbol)> Symbols = new()
    {
        ("≈", @"\approx"),
        ("≠", @"\neq"),
        ("(", @"\left("),
        (")", @"\right)"),
        ("*", @"\cdot"),
    };
    private string ConvertCommands(string expression, (Regex searchOpera, string replaceOpera) opera, bool keepParenthesis)
    {
        //Debug.WriteLine("Start: ConvertCommands");
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

        //Debug.WriteLine("Stop: ConvertCommands");
        return expression;
    }
    private string ReplaceAtIndex(string text, (int index, int length) index, string item)
    {
        //Debug.WriteLine("Start: ReplaceAtIndex");
        //Debug.WriteLine("Stop: ReplaceAtIndex");
        return text.Remove(index.index, index.length).Insert(index.index, item);
    }
    #endregion
    #region Parenthesis Matcher
    private List<(int, int)> FindMatchingParentheses(string input, char left, char right)
    {
        //Debug.WriteLine("Start: FindMatchingParentheses");
        List<(int, int)> matchingPairs = new();
        Stack<int> stack = new();
        for (int i = 0; i < input.Length; i++)
            if (input[i] == left)
                stack.Push(i);
            else if (input[i] == right)
                if (stack.Count > 0)
                    matchingPairs.Add((stack.Pop(), i));

        //Debug.WriteLine("Stop: FindMatchingParentheses");
        return matchingPairs;
    }
    #endregion
    #region InsertVariablesInFormula
    private string? InsertVariablesInFormula(string formula, Dictionary<string, decimal?> variableValues, List<(string, bool, int)> getOperators)
    {
        //Debug.WriteLine("Start: InsertVariablesInFormula");
        formula = ReplaceOperators(formula, false, getOperators);
        string variable = formula.Split('=')[0].Replace(" ", null);
        foreach (KeyValuePair<string, decimal?> dic in variableValues)
        {
            if (dic.Key != variable && dic.Value == null)
                return null;
            if (formula.Contains(dic.Key))
                if (!string.IsNullOrEmpty(dic.Value.ToString()))
                    formula = formula.Replace(dic.Key, dic.Value.ToString());
        }
        string original = ReplaceOperators(formula, true, getOperators);

        //Debug.WriteLine("Stop: InsertVariablesInFormula");
        return original;
    }
    private string ReplaceOperators(string formula, bool getOriginal, List<(string, bool, int)> getOperators)
    {
        //Debug.WriteLine("Start: ReplaceOperators");
        if (getOriginal)
            for (int i = 0; i < getOperators.Count; i++)
                formula = formula.Replace($" ¤{i}¤ ", getOperators[i].Item1);
        else
            for (int i = 0; i < getOperators.Count; i++)
                formula = formula.Replace(getOperators[i].Item1, $" ¤{i}¤ ");

        //Debug.WriteLine("Stop: ReplaceOperators");
        return formula;
    }
    #endregion
    #region LaTeX2MML
    public List<string> LaTeX2MML(List<string> LaTeXLines, bool mml2omml, bool IsText)
    {
        #region Universal Instances
        List<string> MML = new();
        #endregion
        #region IsText-Checker
        if (IsText)
            for (int i = 0; i < LaTeXLines.Count; i++)
                MML.Add("<math xmlns='http://www.w3.org/1998/Math/MathML'><mtext>" + LaTeXLines[i] + "</mtext></math>");
        else
        {
            #region Calculate Divide LaTeX Charactors
            int MinLength = LaTeXLines[0].Length;
            int MaxLength = 0;
            for (int i = 1; i < LaTeXLines.Count; i++)
            {
                if (LaTeXLines[i].Length < MinLength)
                    MinLength = LaTeXLines[i].Length;
                if (LaTeXLines[i].Length > MaxLength)
                    MaxLength = LaTeXLines[i].Length;
            }
            //Settings:
            //LaTeX_FormulaDivider = " \\QtCskgE "
            //LaTeX_PlusDivider = " \\fsReGvS "
            //LaTeX_LengthDivider = " \\jDsKeoA "
            //1 Charactor = 463;
            int HowMany = 1000;
            if (HowMany < MaxLength)
            {
                Console.WriteLine("Should be possible to call this, but if you get this message it got called!");
                HowMany = MaxLength;
            }
            #endregion
            #region Universal Values
            string LaTeX = "";
            string LaTeX_FormulaDivider = " \\QtCskgE ";
            string LaTeX_PlusDivider = " \\fsReGvS ";
            string LaTeX_LengthDivider = " \\jDsKeoA ";
            string MML_FormulaDivider = "<mi>\\QtCskgE</mi>";
            string MML_PlusDivider = "<mi>\\fsReGvS</mi>";
            int CharCount = 0; int CharCountTotal = 0;
            #endregion
            #region '+' issue & Charactor length issue
            for (int i = 0; i < LaTeXLines.Count; i++)
            {
                LaTeXLines[i] = LaTeXLines[i].Replace("+", LaTeX_PlusDivider);
                CharCount += LaTeXLines[i].Length;
                CharCountTotal += LaTeXLines[i].Length;
                if (CharCount > HowMany)
                {
                    LaTeXLines[i - 1] = LaTeXLines[i - 1] + LaTeX_LengthDivider;
                    CharCount = 0;
                }
            }
            foreach (string line in LaTeXLines)
            {
                LaTeX += LaTeX_FormulaDivider + line;
            }
            int a = 0;
            foreach (string item in LaTeX.Split(LaTeX_LengthDivider))
            {
                a++;
                HttpClient httpClient = new();
                string LaTeX_input = item[LaTeX_FormulaDivider.Length..];
                HttpRequestMessage request = new(HttpMethod.Get, $"https://www.wiris.net/demo/editor/latex2mathml?latex={LaTeX_input}");
                HttpResponseMessage response = httpClient.Send(request);
                StreamReader reader = new(response.Content.ReadAsStream());
                string responseBody = reader.ReadToEnd();
                string content = responseBody.Replace(MML_PlusDivider, "<mo>+</mo>");
                string MML_Start = "<math xmlns=\"http://www.w3.org/1998/Math/MathML\">";
                string MML_End = "</math>";
                foreach (string line in content.Split(MML_FormulaDivider))
                {
                    string newLine = line;
                    if (!line.Contains(MML_Start))
                    {
                        newLine = MML_Start + line;
                    }
                    if (!line.Contains(MML_End))
                    {
                        newLine += MML_End;
                    }
                    MML.Add(newLine);
                }
            }
            #endregion
        }
        #endregion
        #region MML2OMML switch
        if (mml2omml)
        {
            MML = MML2OMML(MML);
        }
        #endregion
        return MML;
    }
    private string LaTeX2MML(string LaTeX, bool mml2omml, bool IsText)
    {
        #region LaTeX2MML - Main Engine
        string MML;
        if (IsText)
            MML = "<math xmlns='http://www.w3.org/1998/Math/MathML'><mtext>" + LaTeX + "</mtext></math>";
        else
        {
            HttpClient httpClient = new();
            HttpRequestMessage request = new(HttpMethod.Get, $"https://www.wiris.net/demo/editor/latex2mathml?latex={LaTeX}");
            HttpResponseMessage response = httpClient.Send(request);
            StreamReader reader = new(response.Content.ReadAsStream());
            MML = reader.ReadToEnd();
        }
        if (mml2omml)
            MML = MML2OMML(MML);
        #endregion
        return MML;
    }
    public List<string> MML2OMML(List<string> MML)
    {
        #region Universal Instances
        List<string> officeML = new();
        XslCompiledTransform XSLT = new();
        string desktopPathXML = Environment.CurrentDirectory;
        string newFolderPathXML = Path.Combine(desktopPathXML, @"MML2OMML.XSL");
        // The MML2OMML.xsl file is located under "C:\Program Files\Microsoft Office\root\Office16\MML2OMML.XSL"
        // But also in the bin\Debug\net7.0\ folder of this project
        XSLT.Load(@$"{newFolderPathXML}");
        #endregion
        #region XSLT-Settings
        XmlWriterSettings XSLT_Settings = new();
        if (XSLT.OutputSettings != null)
        {
            XSLT_Settings = XSLT.OutputSettings.Clone();
            XSLT_Settings.ConformanceLevel = ConformanceLevel.Fragment;
            XSLT_Settings.OmitXmlDeclaration = true;
        }
        #endregion
        #region Converter
        for (int i = 0; i < MML.Count; i++)
        {
            using MemoryStream ms = new();
            using XmlReader reader = XmlReader.Create(new StringReader(MML[i]));
            using XmlWriter xw = XmlWriter.Create(ms, XSLT_Settings);
            XSLT.Transform(reader, xw);
            ms.Seek(0, SeekOrigin.Begin);
            using StreamReader sr = new(ms, Encoding.UTF8);
            officeML.Add(sr.ReadToEnd());
        }
        #endregion
        return officeML;
    }
    private string MML2OMML(string MML)
    {
        #region Universal Instances
        XslCompiledTransform XSLT = new();
        string desktopPathXML = Environment.CurrentDirectory;
        string newFolderPathXML = Path.Combine(desktopPathXML, @"MML2OMML.XSL");
        // The MML2OMML.xsl file is located under "C:\Program Files\Microsoft Office\root\Office16\MML2OMML.XSL"
        // But also in the bin\Debug\net7.0\ folder of this project
        XSLT.Load(@$"{newFolderPathXML}");
        #endregion
        #region XSLT-Settings
        XmlWriterSettings XSLT_Settings = new();
        if (XSLT.OutputSettings != null)
        {
            XSLT_Settings = XSLT.OutputSettings.Clone();
            XSLT_Settings.ConformanceLevel = ConformanceLevel.Fragment;
            XSLT_Settings.OmitXmlDeclaration = true;
        }
        #endregion
        #region Converter
        using MemoryStream ms = new();
        using XmlReader reader = XmlReader.Create(new StringReader(MML));
        using XmlWriter xw = XmlWriter.Create(ms, XSLT_Settings);
        XSLT.Transform(reader, xw);
        ms.Seek(0, SeekOrigin.Begin);
        using StreamReader sr = new(ms, Encoding.UTF8);
        #endregion
        return sr.ReadToEnd();
    }
    #endregion LaTeX2MML
}
