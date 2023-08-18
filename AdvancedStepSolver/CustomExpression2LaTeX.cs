using System.Text.RegularExpressions;

namespace AdvancedStepSolver;

public class CustomExpression2LaTeX
{
    public string LaTeX = "";
    public CustomExpression2LaTeX(string expression)
    {
        LaTeX = ConvertExpression(expression);
    }
    //the converter
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
                parenthesisMatches = new ParenthesisMatcher(expression).MatchedParenthesis;
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
                parenthesisMatches = new ParenthesisMatcher(expression).MatchedParenthesis;
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
    };
    private static string ConvertCommands(string expression, (Regex searchOpera, string replaceOpera) opera, bool keepParenthesis)
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
            parenthesisMatches = new ParenthesisMatcher(expression).MatchedParenthesis;

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
    private static string ReplaceAtIndex(string text, (int index, int length) index, string item)
    {
        return text.Remove(index.index, index.length).Insert(index.index, item);
    }
}
