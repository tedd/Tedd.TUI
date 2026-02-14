using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Tedd.TUI.CodeColoring;

public class PrismTokenizer
{
    public static List<Token> Tokenize(string text, Grammar grammar)
    {
        // 1. Handle 'rest' - Prism merges rest grammar into main grammar.
        // We assume grammar is fully prepared for now.

        // 2. Create LinkedList with dummy head
        var tokenList = new LinkedList<object>();
        var head = new object(); // Dummy head
        tokenList.AddLast(head);
        tokenList.AddLast(text);

        // 3. Match
        MatchGrammar(text, tokenList, grammar, tokenList.First, 0);

        // 4. Convert to List<Token>
        return ToTokenList(tokenList);
    }

    private static List<Token> ToTokenList(LinkedList<object> tokenList)
    {
        var result = new List<Token>();
        bool first = true;
        foreach (var item in tokenList)
        {
            if (first)
            {
                first = false;
                continue; // Skip dummy head
            }

            if (item is Token t)
            {
                result.Add(t);
            }
            else if (item is string s)
            {
                result.Add(new Token("text", s));
            }
        }
        return result;
    }

    private static void MatchGrammar(string text, LinkedList<object> tokenList, Grammar grammar, LinkedListNode<object>? startNode, int startPos)
    {
        // Iterate over tokens in grammar
        // We need to iterate in order.
        // Assuming Grammar implementation preserves order or we iterate correctly.

        foreach (var key in grammar.Keys)
        {
            var patterns = grammar[key];
            if (patterns == null || patterns.Count == 0) continue;

            for (int j = 0; j < patterns.Count; j++)
            {
                var patternObj = patterns[j];
                var pattern = patternObj.Regex;
                var inside = patternObj.Inside;
                var lookbehind = patternObj.Lookbehind;
                var greedy = patternObj.Greedy;
                var alias = patternObj.Alias;

                if (greedy && !pattern.Options.HasFlag(RegexOptions.RightToLeft))
                {
                    // JS global flag is implicit in how we use matches, but greedy logic is specific.
                    // We need to find all matches in the text, then reconcile with tokenList.
                    MatchGreedy(text, tokenList, key, patternObj, startPos);
                    continue;
                }

                // Normal matching
                int pos = startPos;
                var currentNode = startNode!.Next;

                while (currentNode != null)
                {
                    var str = currentNode.Value as string;
                    if (str == null) // It's a Token
                    {
                        // Calculate pos
                        if (currentNode.Value is Token t)
                        {
                            pos += GetLength(t);
                        }
                        currentNode = currentNode.Next;
                        continue;
                    }

                    // Match pattern against str
                    var match = pattern.Match(str);
                    if (!match.Success)
                    {
                        pos += str.Length;
                        currentNode = currentNode.Next;
                        continue;
                    }

                    // Handle lookbehind
                    int index = match.Index;
                    string matchStr = match.Value;

                    if (lookbehind && match.Groups.Count > 1)
                    {
                        // Prism lookbehind: change the match to remove the text matched by the lookbehind group (group 1)
                        var lbGroup = match.Groups[1];
                        int lbLength = lbGroup.Length;
                        index += lbLength;
                        matchStr = matchStr.Substring(lbLength);
                    }

                    var from = index;
                    var to = index + matchStr.Length;

                    var before = str.Substring(0, from);
                    var after = str.Substring(to);

                    var removeFrom = currentNode.Previous;

                    // Modify List
                    // 1. Insert 'before' if not empty
                    if (!string.IsNullOrEmpty(before))
                    {
                        tokenList.AddBefore(currentNode, before);
                        pos += before.Length;
                    }

                    // 2. Create new Token
                    object content = matchStr;
                    if (inside != null)
                    {
                        content = Tokenize(matchStr, inside); // Recursion
                    }

                    var newToken = new Token(key, content, alias);
                    var newNode = tokenList.AddBefore(currentNode, newToken);

                    // 3. Insert 'after' if not empty
                    if (!string.IsNullOrEmpty(after))
                    {
                        tokenList.AddBefore(currentNode, after);
                    }

                    // 4. Remove old node
                    tokenList.Remove(currentNode);

                    // Update currentNode to continue
                    currentNode = newNode.Next;

                    // But Prism iterates carefully.
                    // Since we matched inside this string node, we are done with it.
                    // We continue searching in the 'after' part?
                    // Prism: "currentNode = addAfter(..., after)"
                    // It continues loop.
                }
            }
        }
    }

    private static void MatchGreedy(string text, LinkedList<object> tokenList, string tokenKey, Pattern patternObj, int startPos)
    {
        var pattern = patternObj.Regex;
        var inside = patternObj.Inside;
        var lookbehind = patternObj.Lookbehind;
        var alias = patternObj.Alias;

        var matches = pattern.Matches(text);
        if (matches.Count == 0) return;

        int matchIndex = 0;
        var currentNode = tokenList.First;
        int pos = 0;

        while (currentNode != null && matchIndex < matches.Count)
        {
            var match = matches[matchIndex];

            // Lookbehind adjustment
            int index = match.Index;
            string matchStr = match.Value;
            if (lookbehind && match.Groups.Count > 1)
            {
                var lbGroup = match.Groups[1];
                int lbLength = lbGroup.Length;
                index += lbLength;
                matchStr = matchStr.Substring(lbLength);
            }

            int from = index;
            int to = index + matchStr.Length;

            // Advance currentNode to 'from'
            while (currentNode != null && pos + GetNodeLength(currentNode) <= from)
            {
                pos += GetNodeLength(currentNode);
                currentNode = currentNode.Next;
            }

            if (currentNode == null) break;

            // If currentNode is a Token, we can't match inside it (greedy matches shouldn't split existing tokens?)
            // Prism says: "the current node is a Token, then the match starts inside another Token, which is invalid"
            if (currentNode.Value is Token)
            {
                // Skip this match?
                matchIndex++;
                continue;
            }

            // We are inside a string node (or spanning multiple nodes)
            // But wait, 'pos' is the start of currentNode.
            // 'from' >= pos.

            // Check if match spans multiple nodes
            var nodesToRemove = new List<LinkedListNode<object>>();
            var tempNode = currentNode;
            int p = pos;

            // Consume nodes until we cover 'to'
            while (tempNode != null && p < to)
            {
                nodesToRemove.Add(tempNode);
                p += GetNodeLength(tempNode);
                tempNode = tempNode.Next;
            }

            int rangeStart = pos; // Absolute start of the first node to remove
            int rangeEnd = p;     // Absolute end of the last node removed

            // Calculate before and after based on original text
            // rangeStart <= from <= to <= rangeEnd
            string before = text.Substring(rangeStart, from - rangeStart);
            string after = text.Substring(to, rangeEnd - to);

            // Perform replacement
            // 1. Add 'before'
            if (!string.IsNullOrEmpty(before))
            {
                tokenList.AddBefore(currentNode, before);
            }

            // 2. Add Match Token
            object content = matchStr;
            if (inside != null)
            {
                content = Tokenize(matchStr, inside);
            }
            var newToken = new Token(tokenKey, content, alias);
            var newNode = tokenList.AddBefore(currentNode, newToken);

            // 3. Add 'after'
            if (!string.IsNullOrEmpty(after))
            {
                tokenList.AddBefore(currentNode, after);
            }

            // 4. Remove old nodes
            foreach (var n in nodesToRemove)
            {
                tokenList.Remove(n);
            }

            // 5. Update loop state
            matchIndex++;
            currentNode = newNode.Next; // Continue after new token (or 'after' string if present)
            pos = to; // 'currentNode' starts at 'to'

            // But we need to sync 'currentNode' and 'pos' correctly.
            // 'currentNode' is now the node AFTER our inserted stuff.
            // 'pos' should be the start index of 'currentNode'.
            // Which is exactly 'to' if we did everything right?
            // Yes, because we replaced [from, to) with token.
            // 'before' took [nodeStart, from).
            // 'token' took [from, to).
            // 'after' took [to, ...].
            // Wait, 'after' was inserted before 'strNode', so it is before 'currentNode'.
            // So 'currentNode' is effectively 'tempNode' (the one after the range).
            // So pos should be 'lastNodeEnd'?
            // No.

            // Re-eval pos logic:
            // We processed up to 'to'.
            // 'after' contains text starting at 'to'.
            // We inserted 'after' node.
            // Then we removed original nodes.
            // So 'currentNode' should be the node after 'after'.
            // Which is what 'newNode.Next' gave us IF we didn't insert 'after'.
            // If we inserted 'after', 'newNode.Next' is 'after'.
            // So we want to continue matching from 'after' node? No, we matched that text already.
            // We want to continue from after the match.

            // Actually, `matches` contains all matches in text. We just need to find the next valid spot in tokenList.
        }
    }

    private static int GetNodeLength(LinkedListNode<object> node)
    {
        if (node.Value is string s) return s.Length;
        if (node.Value is Token t) return GetLength(t);
        return 0;
    }

    private static int GetLength(Token t)
    {
        if (t.Content is string s) return s.Length;
        if (t.Content is List<Token> l) return l.Sum(x => GetLength(x));
        return 0; // Should not happen
    }
}
