using System.Globalization;
using System.Text.RegularExpressions;

namespace AdvancedStepSolver;

public class InfoClass
{
    public decimal? Calculator(string Operator, decimal? aa, decimal? bb, int decimals, List<string> CalcSteps, List<string> TextSteps)
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
    private decimal? Dou2Dec(double check)
    {
        if (double.IsNormal(check))
            return decimal.Parse(check.ToString());
        else
            return null;
    }
    public string PreCalculationFormatting(string input)
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
            MatchedParenthesis = new ParenthesisMatcher(input).MatchedParenthesis;
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
            MatchedParenthesis = new ParenthesisMatcher(input).MatchedParenthesis;
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

        return input;
    }
    private static string AfterCalculationFormatting(string input)
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
            ParenthesisMatches = new ParenthesisMatcher(input).MatchedParenthesis;
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
            ParenthesisMatches = new ParenthesisMatcher(input).MatchedParenthesis;
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

    // Info
    public List<Regex> SearchOperatorRegex = new()
    {
        new(@"[+\-*/^()]"), // +,-,*,/,^,()
        new(@"\d+(\.\d+)?"), // Numbers: 0-9
        new(@"\bsqrt(\[-?\d+(\.\d+)?\])?"), // sqrt[n](x) or sqrt(x)
        new(@"\blog(_\(-?\d+(\.\d+)?\))?"), // log[n](x) or log(x)
        new(@"\bsin(\^\(-1\))?"), // sin^(-1)(x) or sin(x)
        new(@"\bcos(\^\(-1\))?"), // cos^(-1)(x) or cos(x)
        new(@"\btan(\^\(-1\))?"), // tan^(-1)(x) or tan(x)
    };
    public List<(string Operator, bool MultipleFormats, int MathHierarchy)> GetOperators = new()
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
    public List<(string Constant, double Value)> MathConstants = new()
    {
        ("pi", 3.141592653589793),
        ("phi", 1.618033988749895),
        ("varphi", 1.618033988749895),
    };
    public Dictionary<string, (int?, bool?)> Settings = new()
    {
        { "Radians", (null, null) },
    };
}
