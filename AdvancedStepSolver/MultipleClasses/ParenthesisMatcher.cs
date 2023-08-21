namespace AdvancedStepSolver.MultipleClasses;

public class ParenthesisMatcher
{
    public List<(int, int)> MatchedParenthesis = new();
    public ParenthesisMatcher(string expression)
    {
        MatchedParenthesis = FindMatchingParentheses(expression);
    }
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
}
