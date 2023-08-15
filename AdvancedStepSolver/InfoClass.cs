using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace AdvancedStepSolver;

public class InfoClass
{
    // StringCalculator
    public double Calculator(string Operator, double a, double b, int Decimals, List<(string, string)> CalculationSteps, List<string> ExplanationSteps)
    {
        double Calculated = 0;
        double ConvertValue = 0;
        string aPar;
        string bPar;
        switch (Operator)
        {
            case "^":
                Calculated = System.Math.Round(System.Math.Pow(b, a), Decimals);
                aPar = $"{a}";
                if (a < 0 || !double.IsInteger(a))
                    aPar = $"({aPar})";
                bPar = $"{b}";
                if (b < 0)
                    bPar = $"({bPar})";

                CalculationSteps.Add(($"{bPar}^{aPar}", Calculated.ToString()));
                ExplanationSteps.Add($"Exponenten udregnes");
                break;
            case "*":
                Calculated = System.Math.Round(b * a, Decimals);
                CalculationSteps.Add(($"{b}*{a}", Calculated.ToString()));
                ExplanationSteps.Add($"Der ganges");
                break;
            case "/":
                Calculated = System.Math.Round(b / a, Decimals);
                CalculationSteps.Add(($"{b}/{a}", Calculated.ToString()));
                ExplanationSteps.Add($"Der divideres");
                break;
            case "+":
                Calculated = System.Math.Round(b + a, Decimals);
                CalculationSteps.Add(($"{b}+{a}", Calculated.ToString()));
                ExplanationSteps.Add($"Der plusses");
                break;
            case "-":
                Calculated = System.Math.Round(b - a, Decimals);
                CalculationSteps.Add(($"{b}-{a}", Calculated.ToString()));
                ExplanationSteps.Add($"Der minuses");
                break;

            default:
                if (Operator.Contains("sqrt"))
                {
                    //sqrt
                    if (Operator == "sqrt")
                    {
                        Calculated = System.Math.Round(System.Math.Sqrt(a), Decimals);
                        CalculationSteps.Add(($"sqrt({a})", Calculated.ToString()));
                        ExplanationSteps.Add($"Kvadratroden udregnes");
                    }
                    //sqrt[n]
                    else if (Operator.StartsWith("sqrt[") && Operator.EndsWith("]"))
                    {
                        double baseValue = double.Parse(Operator[5..^1], CultureInfo.InvariantCulture); // Extract the number from "sqrt[...]"
                        Calculated = System.Math.Round(System.Math.Pow(a, 1.0 / baseValue), Decimals);
                        CalculationSteps.Add(($"sqrt[{baseValue}]({a})", Calculated.ToString()));
                        ExplanationSteps.Add($"Roden til {baseValue} udregnes");
                    }
                }
                else if (Operator.Contains("log"))
                {
                    //log
                    if (Operator == "log")
                    {
                        Calculated = System.Math.Round(System.Math.Log10(a), Decimals);
                        CalculationSteps.Add(($"log({a})", Calculated.ToString()));
                        ExplanationSteps.Add($"Log udregnes");
                    }
                    //log[n]
                    else if (Operator.StartsWith("log_(") && Operator.EndsWith(")"))
                    {
                        double baseValue = double.Parse(Operator[5..^1], CultureInfo.InvariantCulture); // Extract the number from "log[...]"
                        Calculated = System.Math.Round(System.Math.Log(a, baseValue), Decimals);
                        CalculationSteps.Add(($"log_({baseValue})({a})", Calculated.ToString()));
                        ExplanationSteps.Add($"Log med basen {baseValue} udregnes");
                    }
                }
                else if (Operator.Contains("sin") || Operator.Contains("cos") || Operator.Contains("tan"))
                {
                    if (Operator.Contains("sin"))
                    {
                        //sin
                        if (Operator == "sin")
                        {
                            ConvertValue = a;
                            if (Settings.TryGetValue("Radians", out (int?, bool?) value) && value.Item2 == false)
                                ConvertValue = ConvertValue / (180 / System.Math.PI);
                            ConvertValue = System.Math.Sin(ConvertValue);
                            Calculated = System.Math.Round(ConvertValue, Decimals);
                            CalculationSteps.Add(($"sin({a})", Calculated.ToString()));
                            ExplanationSteps.Add($"Sinus udregnes");
                        }
                        //sin^(-1)
                        else if (Operator == "sin^(-1)")
                        {
                            Console.WriteLine(Settings.TryGetValue("Radians", out (int?, bool?) f) && f.Item2 == false);
                            ConvertValue = a;
                            if (Settings.TryGetValue("Radians", out (int?, bool?) value) && value.Item2 == false)
                                ConvertValue = ConvertValue / (180 / System.Math.PI);
                            ConvertValue = System.Math.Asin(ConvertValue);
                            Calculated = System.Math.Round(ConvertValue, Decimals);
                            CalculationSteps.Add(($"sin^(-1)({a})", Calculated.ToString()));
                            ExplanationSteps.Add($"Sinus udregnes");
                        }
                    }
                    else if (Operator.Contains("cos"))
                    {
                        //cos
                        if (Operator == "cos")
                        {
                            ConvertValue = a;
                            if (Settings.TryGetValue("Radians", out (int?, bool?) value) && value.Item2 == false)
                                ConvertValue = ConvertValue / (180 / System.Math.PI);
                            ConvertValue = System.Math.Cos(ConvertValue);
                            Calculated = System.Math.Round(ConvertValue, Decimals);
                            CalculationSteps.Add(($"cos({a})", Calculated.ToString()));
                            ExplanationSteps.Add($"Cosinus udregnes");
                        }
                        //cos^(-1)
                        else if (Operator == "cos^(-1)")
                        {
                            ConvertValue = a;
                            if (Settings.TryGetValue("Radians", out (int?, bool?) value) && value.Item2 == false)
                                ConvertValue = ConvertValue / (180 / System.Math.PI);
                            ConvertValue = System.Math.Acos(ConvertValue);
                            Calculated = System.Math.Round(ConvertValue, Decimals);
                            CalculationSteps.Add(($"cos^(-1)({a})", Calculated.ToString()));
                            ExplanationSteps.Add($"Cosinus udregnes");
                        }
                    }
                    else if (Operator.Contains("tan"))
                    {
                        //tan
                        if (Operator == "tan")
                        {
                            ConvertValue = a;
                            if (Settings.TryGetValue("Radians", out (int?, bool?) value) && value.Item2 == false)
                                ConvertValue = ConvertValue / (180 / System.Math.PI);
                            ConvertValue = System.Math.Tan(ConvertValue);
                            Calculated = System.Math.Round(ConvertValue, Decimals);
                            CalculationSteps.Add(($"tan^(-1)({a})", Calculated.ToString()));
                            ExplanationSteps.Add($"Tangens udregnes");
                        }
                        //tan^(-1)
                        else if (Operator == "tan^(-1)")
                        {
                            ConvertValue = a;
                            if (Settings.TryGetValue("Radians", out (int?, bool?) value) && value.Item2 == false)
                                ConvertValue = ConvertValue / (180 / System.Math.PI);
                            ConvertValue = System.Math.Atan(ConvertValue);
                            Calculated = System.Math.Round(ConvertValue, Decimals);
                            CalculationSteps.Add(($"tan^(-1)({a})", Calculated.ToString()));
                            ExplanationSteps.Add($"Tangens udregnes");
                        }
                    }
                }
                else
                    throw new ArgumentException("Invalid operator.");
                break;
        };
        return Calculated;
    }
    public string PreCalculationFormatting(string input)
    {
        Regex Regex;
        List<(int, int)> MatchedParenthesis;
        int where;
        int LeftParenthesis;
        int RightParenthesis;
        string searchField;
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

        //Insert Parenthesis rundt om sqrt, så vi sikrer os at den bliver udregnet før den bliver lagt sammen med andet, til venstre for sqrt
        Regex = new(@"(?<!\()((\bsqrt(\[-?\d+(\,\d+)?\])?)|(\bsin(\^\(-1\))?)|(\bcos(\^\(-1\))?)|(\btan(\^\(-1\))?))");
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

        //Find ")^" og tjek om der er noget der kan udregnes derinde
        Regex = new(@"\)\^");
        matchCollection = Regex.Matches(input);
        foreach (Match item in matchCollection.Cast<Match>())
        {
            where = item.Index;
            LeftParenthesis = 0;
            RightParenthesis = where;
            MatchedParenthesis = new ParenthesisMatcher(input).MatchedParenthesis;
            foreach ((int, int) item2 in MatchedParenthesis)
                if (item2.Item2 == RightParenthesis)
                {
                    LeftParenthesis = item2.Item1;
                    break;
                }
            searchField = input.Substring(LeftParenthesis, RightParenthesis - 1);
            foreach ((string, bool, int) opera in GetOperators)
                if (searchField.Contains(opera.Item1) && new Regex(@"(\d+(\,\d+)?)").Matches(searchField).Count > 1)
                {
                    input = input.Insert(LeftParenthesis, "(").Insert(RightParenthesis + 1, ")");
                    break;
                }
        }

        return input;
    }
    public string AfterCalculationFormatting(string input)
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

        //Remove UnNeededParenthesis
        Regex = new(@"\(\(");
        while ((match = Regex.Match(input)).Success)
        {
            LeftParenthesis = match.Index;
            LeftParenthesis2 = match.Index + 1;
            ParenthesisMatches = new ParenthesisMatcher(input).MatchedParenthesis;
            foreach ((int, int) parMatch in ParenthesisMatches)
            {
                if (LeftParenthesis == parMatch.Item1)
                    RightParenthesis = parMatch.Item2;
                if (LeftParenthesis2 == parMatch.Item1)
                    RightParenthesis2 = parMatch.Item2;
            }
            if ((RightParenthesis - 1) == RightParenthesis2)
            {
                input = input.Remove(LeftParenthesis2, 1);
                input = input.Remove(RightParenthesis2 - 1, 1);
            }
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
