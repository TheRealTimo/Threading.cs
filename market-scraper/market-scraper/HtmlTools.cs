using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace market_scraper
{
    public class HtmlTools
    {
        private static readonly Regex TagRegex =
            new Regex(
                @"<(?<tagname>[^\s/>]+)(?<attributes>(?:\s+\w+(?:\s*=\s*(?:""[^""]*""|'[^']*'|[\^'""\s>]+))?)+\s*|\s*)/?>",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex AttributeRegex =
            new Regex(@"(?<name>\w+)\s*=\s*""(?<value>[^""]*)""", RegexOptions.Compiled);

        public async Task<string> GetHtml(string url)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36");
                var html = await httpClient.GetStringAsync(url);
                return html;
            }
        }


        public Node ParseHtml(string html)
        {
            var rootNode = new Node { TagName = "root" };
            var tagStack = new Stack<Node>();
            tagStack.Push(rootNode);

            var textRegex = new Regex(@"(?<text>[^<]+)");

            var currentIndex = 0;
            while (currentIndex < html.Length)
            {
                var match = TagRegex.Match(html, currentIndex);

                if (!match.Success) break;

                var tagName = match.Groups["tagname"].Value.ToLower();
                var attributesString = match.Groups["attributes"].Value;
                var attributes = ParseAttributes(attributesString);

                var currentNode = tagStack.Peek();

                if (tagName.StartsWith("/"))
                {
                    // Closing tag
                    var expectedTagName = tagName.Substring(1);
                    if (tagStack.Count > 0 && currentNode.TagName == expectedTagName) tagStack.Pop();
                }
                else
                {
                    // Opening tag
                    var newNode = new Node { TagName = tagName, Attributes = attributes };
                    currentNode.Children.Add(newNode);
                    if (!match.Value.EndsWith("/>"))
                    {
                        currentNode = newNode;
                        tagStack.Push(newNode);
                    }

                    currentIndex = match.Index + match.Length;

// Handle text nodes
                    var textMatch = textRegex.Match(html, currentIndex);
                    if (textMatch.Success)
                    {
                        var text = textMatch.Groups["text"].Value.Trim();
                        if (!string.IsNullOrEmpty(text))
                        {
                            var textNode = new Node { TagName = "#text", InnerText = text };
                            currentNode.Children.Add(textNode);
                            currentIndex = textMatch.Index + textMatch.Length;
                        }
                    }
                }
            }

            // Set the inner text of all nodes based on their children's inner text
            SetInnerText(rootNode);

            return rootNode;
        }

        private void SetInnerText(Node node)
        {
            node.InnerText = string.Concat(node.Children.Select(c => c.InnerText));
            foreach (var child in node.Children) SetInnerText(child);
        }


        private Dictionary<string, string> ParseAttributes(string attributesString)
        {
            var result = new Dictionary<string, string>();
            foreach (Match match in AttributeRegex.Matches(attributesString))
            {
                var name = match.Groups["name"].Value;
                var value = match.Groups["value"].Value;
                result[name] = value;
            }

            return result;
        }

        public IEnumerable<Node> SelectNodes(Node root, string selector)
        {
            var parts = selector.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return SelectNodesRecursive(root, parts, 0);
        }

        private IEnumerable<Node> SelectNodesRecursive(Node node, string[] selectors, int index)
        {
            if (index >= selectors.Length)
            {
                yield return node;
            }
            else
            {
                var selector = selectors[index];

                var tagName = selector;
                var className = "";
                var idName = "";
                var attributeName = "";
                var attributeValue = "";

                if (selector.StartsWith("."))
                {
                    tagName = "";
                    className = selector.Substring(1);
                }
                else if (selector.StartsWith("#"))
                {
                    tagName = "";
                    idName = selector.Substring(1);
                }
                else
                {
                    var attributeMatch = Regex.Match(selector, @"^\[(\w+)(?:=(\w+))?\]$");
                    if (attributeMatch.Success)
                    {
                        tagName = "";
                        attributeName = attributeMatch.Groups[1].Value;
                        attributeValue = attributeMatch.Groups[2].Value;
                    }
                }

                foreach (var child in node.Children)
                    if ((string.IsNullOrEmpty(tagName) || child.TagName == tagName) &&
                        (string.IsNullOrEmpty(className) ||
                         (child.Attributes != null &&
                          child.Attributes.TryGetValue("class", out var classAttributeValue) &&
                          classAttributeValue.Split(' ').Contains(className))) &&
                        (string.IsNullOrEmpty(idName) ||
                         (child.Attributes != null && child.Attributes.TryGetValue("id", out var idAttributeValue) &&
                          idAttributeValue == idName)) &&
                        (string.IsNullOrEmpty(attributeName) ||
                         (child.Attributes != null &&
                          child.Attributes.TryGetValue(attributeName, out var attributeFoundValue) &&
                          (string.IsNullOrEmpty(attributeValue) || attributeFoundValue == attributeValue))))
                        foreach (var selectedNode in SelectNodesRecursive(child, selectors, index + 1))
                            yield return selectedNode;

                if (index == 0)
                    foreach (var child in node.Children)
                    foreach (var selectedNode in SelectNodesRecursive(child, selectors, index))
                        yield return selectedNode;
            }
        }


        public class Node
        {
            public string TagName { get; set; }
            public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
            public List<Node> Children { get; set; } = new List<Node>();
            public string InnerText { get; set; } = string.Empty;
        }
    }
}