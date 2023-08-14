using System.Globalization;
using System.Text.RegularExpressions;

namespace AdvancedStepSolver;

public class InfoClass
{
    // StringCalculator
    public double Calculator(string Operator, double a, double b, int Decimals, List<(string, string)> CalculationSteps, List<string> ExplanationSteps)
    {
        double Calculated = 0;
        switch (Operator)
        {
            case "^":
                Calculated = System.Math.Round(System.Math.Pow(b, a), Decimals);
                if (b < 0)
                    CalculationSteps.Add(($"({b})^{a}", Calculated.ToString()));
                else
                    CalculationSteps.Add(($"{b}^{a}", Calculated.ToString()));
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
                            Calculated = System.Math.Round(System.Math.Sin(a), Decimals);
                            CalculationSteps.Add(($"sin({a})", Calculated.ToString()));
                            ExplanationSteps.Add($"Sinus udregnes");
                        }
                        //sin^(-1)
                        else if (Operator == "sin^(-1)")
                        {
                            Calculated = System.Math.Round(System.Math.Asin(a), Decimals);
                            CalculationSteps.Add(($"sin^(-1)({a})", Calculated.ToString()));
                            ExplanationSteps.Add($"Sinus udregnes");
                        }
                    }
                    else if (Operator.Contains("cos"))
                    {
                        //cos
                        if (Operator == "cos")
                        {
                            Calculated = System.Math.Round(System.Math.Cos(a), Decimals);
                            CalculationSteps.Add(($"cos({a})", Calculated.ToString()));
                            ExplanationSteps.Add($"Cosinus udregnes");
                        }
                        //cos^(-1)
                        else if (Operator == "cos^(-1)")
                        {
                            Calculated = System.Math.Round(System.Math.Acos(a), Decimals);
                            CalculationSteps.Add(($"cos^(-1)({a})", Calculated.ToString()));
                            ExplanationSteps.Add($"Cosinus udregnes");
                        }
                    }
                    else if (Operator.Contains("tan"))
                    {
                        //tan
                        if (Operator == "tan")
                        {
                            Calculated = System.Math.Round(System.Math.Tan(a), Decimals);
                            CalculationSteps.Add(($"tan^(-1)({a})", Calculated.ToString()));
                            ExplanationSteps.Add($"Tangens udregnes");
                        }
                        //tan^(-1)
                        else if (Operator == "tan^(-1)")
                        {
                            Calculated = System.Math.Round(System.Math.Atan(a), Decimals);
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
    public string InsertParenthesisAround(string input)
    {
        Regex Regex;
        MatchCollection matchCollection;
        List<(int, int)> MatchedParenthesis;
        int where;
        int LeftParenthesis;
        int RightParenthesis;

        //Insert Parenthesis rundt om sqrt, så vi sikrer os at den bliver udregnet før den bliver lagt sammen med andet, til venstre for sqrt
        Regex = SearchOperatorRegex[2];
        matchCollection = Regex.Matches(input);
        foreach (Match match in matchCollection.Cast<Match>())
        {
            where = match.Index;
            LeftParenthesis = match.Index + match.Length;
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

            //If any calculable is inside of the sqrt, we will insert a parenthesis inside.
            foreach ((string, bool, int) item in GetOperators)
                if (input.Substring(where + 5, RightParenthesis - (where + 3)).Contains(item.Item1))
                {
                    input = input.Insert(where + 5, "(");
                    input = input.Insert(RightParenthesis + 2, ")");
                    break;
                }
        }

        //Insert Parenthesis rundt om [-n]^, så vi sikrer os at den bliver udregnet før den bliver lagt sammen med andet, til venstre for ^
        Regex = new(@"(-\d+(\.\d+)?\^)");
        matchCollection = Regex.Matches(input);
        foreach (Match match in matchCollection.Cast<Match>())
        {
            where = match.Index;
            RightParenthesis = where + match.Value[..^1].Length + 1;
            input = input.Insert(where, "(");
            input = input.Insert(RightParenthesis, ")");
        }
        return input;
    }
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

    // Expression2LaTeX
    public string Converter(string Operator, string a, string b)
    {
        string Calculated = "";
        #region Free to edit, but only for adding operators!
        switch (Operator)
        {
            case "^":
                Calculated = $@"{b}^{a}";
                break;
            case "*":
                Calculated = $@"{b} \cdot {a}";
                break;
            case "/":
                Calculated = $@"\frac{{{b}}}{{{a}}}";
                break;
            case "+":
                Calculated = $@"{b} + {a}";
                break;
            case "-":
                Calculated = $@"{b} - {a}";
                break;

            default:
                if (Operator.Contains("sqrt"))
                {
                    //sqrt
                    if (Operator == "sqrt")
                    {
                        Calculated = $@"\sqrt{{{a}}}";
                    }
                    else if (Operator.StartsWith("sqrt[") && Operator.EndsWith("]"))
                    {
                        double baseValue = double.Parse(Operator[5..^1], CultureInfo.InvariantCulture); // Extract the number from "log[...]"
                        Calculated = $@"\sqrt[{baseValue}]{{{a}}}";
                    }
                }
                /*
                else if (Operator.Contains("log"))
                {
                    //log
                    if (Operator == "log")
                    {
                        Calculated = System.Math.Round(System.Math.Log10(a), 3);
                        valueStack.Push(Calculated);
                    }
                    //log[n]
                    else if (Operator.StartsWith("log_(") && Operator.EndsWith(")"))
                    {
                        double baseValue = double.Parse(Operator[5..^1], CultureInfo.InvariantCulture); // Extract the number from "log[...]"
                        Calculated = System.Math.Round(System.Math.Log(a, baseValue), 3);
                        valueStack.Push(Calculated);
                    }
                }
                else if (Operator.Contains("sin") || Operator.Contains("cos") || Operator.Contains("tan"))
                {
                    if (Operator.Contains("sin"))
                    {
                        //sin
                        if (Operator == "sin")
                        {
                            Calculated = System.Math.Round(System.Math.Sin(a), 3);
                            valueStack.Push(Calculated);
                        }
                        //sin^(-1)
                        else if (Operator == "sin^(-1)")
                        {
                            Calculated = System.Math.Round(System.Math.Asin(a), 3);
                            valueStack.Push(Calculated);
                        }
                    }
                    else if (Operator.Contains("cos"))
                    {
                        //cos
                        if (Operator == "cos")
                        {
                            Calculated = System.Math.Round(System.Math.Cos(a), 3);
                            valueStack.Push(Calculated);
                        }
                        //cos^(-1)
                        else if (Operator == "cos^(-1)")
                        {
                            Calculated = System.Math.Round(System.Math.Acos(a), 3);
                            valueStack.Push(Calculated);
                        }
                    }
                    else if (Operator.Contains("tan"))
                    {
                        //tan
                        if (Operator == "tan")
                        {
                            Calculated = System.Math.Round(System.Math.Tan(a), 3);
                            valueStack.Push(Calculated);
                        }
                        //tan^(-1)
                        else if (Operator == "tan^(-1)")
                        {
                            Calculated = System.Math.Round(System.Math.Atan(a), 3);
                            valueStack.Push(Calculated);
                        }
                    }
                }
                */
                else
                    throw new ArgumentException("Invalid operator.");
                break;
        };
        return Calculated;
        #endregion Free to edit, but only for adding operators!
    }
    public List<(string Expr, string LaTeX)> OperatorFormats = new()
    {
        ("sqrt", @"\sqrt"),
        ("log", @"\log"),
        ("sin", @"\sin"),
        ("cos", @"\cos"),
        ("tan", @"\tan"),
        ("^", @"^"),
        ("*", @"\cdot"),
        ("/", @"\frac"),
        ("+", @"+"),
        ("-", @"-"),
    };
}
