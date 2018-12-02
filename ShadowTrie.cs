using System;
using System.Collections.Generic;

[Serializable]
public class ShadowTrie
{
    [Serializable]
    class Node
    {
        private readonly List<Node> _nodes;
        private bool _isWord;
        private char _value;

        public List<Node> Nodes { get { return _nodes; } }
        public bool IsWord {  get { return _isWord; }  set { _isWord = value; } }
        public char Value { get { return _value; } set { _value = value; } }

        public Node()
        {
            _nodes = new List<Node>();
            _isWord = false;
        }
    }

    private Node _rootNode;

    public ShadowTrie()
    {
        _rootNode = new Node();
    }
    
    public void Insert(string word)
    {
        var currentNode = _rootNode;
        for (var i = 0; i != word.Length; ++i)
        {
            var currentChar = word[i];
            var found = false;
            var nodes = currentNode.Nodes;
            for (var n = 0; n != nodes.Count; ++n)
            {
                var node = nodes[n];
                if (node.Value == currentChar)
                {
                    currentNode = node;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                var newNode = new Node 
                {
                    Value = currentChar
                };
                currentNode.Nodes.Add(newNode);
                currentNode = newNode;
            }
        }
        currentNode.IsWord = true;
    }

    public bool Find(string word)
    {
        var currentNode = _rootNode;
        for (var i = 0; i != word.Length; ++i)
        {
            var currentChar = word[i];
            var found = false;
            var nodes = currentNode.Nodes;
            for (var n = 0; n != nodes.Count; ++n)
            {
                var node = nodes[n];
                if (node.Value == currentChar)
                {
                    currentNode = node;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                return false;
            }
        }
        return currentNode.IsWord;
    }

    private IEnumerator<Node> FindNode(Node entry, List<char> chars)
    {
        foreach (var node in entry.Nodes)
        {
            if (chars.Contains(node.Value)) yield return node;
        }
    }

    public string[] GetWordsWithChars(List<char> chars)
    {
        var result = new List<string>();

        var word = string.Empty;
        var root = _rootNode.Nodes;

        for (var i = 0; i != root.Count; ++i)
        {
            var lastWord = word;
            if (chars.Contains(root[i].Value))
            {
                word += root[i].Value;
                var second = root[i].Nodes;
                
                for (var o = 0; o != second.Count; ++o)
                {
                    if (chars.Contains(second[o].Value))
                    {
                        word += second[o].Value;
                        var third = second[o].Nodes;
                        for (var n = 0; n != third.Count; ++n)
                        {
                            if (chars.Contains(third[n].Value))
                            {
                                var newWord = word + third[n].Value;
                                if (third[n].IsWord)
                                {
                                    Console.Write("{0}, ", newWord);
                                    result.Add(newWord);
                                }
                            }
                        }
                    }
                    word = string.Empty + root[i].Value;
                }
            }
            word = string.Empty; 
        }
        Console.WriteLine();
        Console.WriteLine("Found total {0} ", result.Count);
        return result.ToArray();
    }

    public string GetRandomWord(int seed = 0)
    {
        var result = string.Empty;
        var node = _rootNode;
        var random = new Random(seed);
        while (true)
        {
            var randomIndex = random.Next() % node.Nodes.Count;
            node = node.Nodes[randomIndex];
            result += node.Value;
            if (node.IsWord) break;
        }
        return result;
    }

    

}
