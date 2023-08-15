using System;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using static System.Net.Mime.MediaTypeNames;

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

        return expression;
    }
    #region Specific LaTeX_Converters
    private string SinCosTan(string expression, (Regex searchOpera, string replaceOpera) opera)
    {
        int rightParenthesis = 0;
        int leftParenthesis;
        List<(int, int)> parenthesisMatches;
        Regex regex;
        Match match;
        int index;

        regex = opera.searchOpera;
        while ((match = regex.Match(expression)).Success)
        {
            index = match.Index + match.Length;
            parenthesisMatches = new ParenthesisMatcher(expression).MatchedParenthesis;
            if (expression[index] == '^')
                leftParenthesis = index + 5;
            else
                leftParenthesis = index;
            foreach ((int, int) parMatch in parenthesisMatches)
                if (leftParenthesis == parMatch.Item1)
                {
                    rightParenthesis = parMatch.Item2;
                    break;
                }
            expression = expression.Insert(leftParenthesis, "{");
            expression = expression.Insert(rightParenthesis + 2, "}");
            expression = ReplaceAtIndex(expression, (match.Index, match.Length), opera.replaceOpera);
            break;
        }
        return expression;
    }
    private string ConvertCommands(string expression, (Regex searchOpera, string replaceOpera) opera, bool keepParenthesis)
    {
        List<(int, int)> parenthesisMatches;
        int index;
        int rightParenthesis = 0;
        int leftParenthesis;
        int rightParenthesis2 = 0;
        int leftParenthesis2;
        int length;

        Match match;
        while ((match = opera.searchOpera.Match(expression)).Success)
        {
            index = match.Index;
            length = match.Length;
            leftParenthesis2 = match.Index - 1;
            leftParenthesis = index + length - 1;
            parenthesisMatches = new ParenthesisMatcher(expression).MatchedParenthesis;

            foreach ((int left, int right) parMatch in parenthesisMatches)
            {
                if (parMatch.left == leftParenthesis)
                    rightParenthesis = parMatch.right;
                if (parMatch.left == leftParenthesis2)
                    rightParenthesis2 = parMatch.right;
            }
            if (keepParenthesis)
            {
                expression = ReplaceAtIndex(expression, (leftParenthesis, 1), "{(");
                expression = ReplaceAtIndex(expression, (rightParenthesis + 1, 1), ")}");
                expression = expression.Remove(leftParenthesis2, 1);
                expression = expression.Remove(rightParenthesis2 + 1, 1);
            }
            else
            {
                expression = ReplaceAtIndex(expression, (leftParenthesis, 1), "{");
                expression = ReplaceAtIndex(expression, (rightParenthesis, 1), "}");
                expression = expression.Remove(leftParenthesis2, 1);
                expression = expression.Remove(rightParenthesis2 - 1, 1);
            }
            expression = ReplaceAtIndex(expression, (index-1, opera.replaceOpera[1..].Length), opera.replaceOpera);
        }
        return expression;
    }
    #endregion Specific LaTeX_Converters
    private string ReplaceAtIndex(string text, (int index, int length) index, string item)
    {
        return text.Remove(index.index, index.length).Insert(index.index, item);
    }
}
