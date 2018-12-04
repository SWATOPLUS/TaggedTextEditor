using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TaggedTextEditor
{
    public static class TextConverter
    {
        public static string ConvertTextToDbo(string s)
        {
            var sentences = s.Split('\n')
                .Select(ConvertSentenceToDbo);

            return string.Join("", sentences);
        }

        private static string ConvertSentenceToDbo(string s)
        {
            var words = new List<string>();
            var sb = new StringBuilder();

            foreach (var c in s)
            {
                if (c == ' ' || PunctuationChars.Contains(c))
                {
                    var word = sb.ToString().Trim();
                    sb.Clear();

                    if (word != string.Empty)
                    {
                        words.Add(word);
                    }

                    if (c != ' ')
                    {
                        words.Add(c.ToString());
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
        
            return string.Join(", ", words.Where(x => !string.IsNullOrWhiteSpace(x)).Select(ConvertWordToDbo));
        }

        private static string ConvertWordToDbo(string s)
        {
            if (s.Contains("_"))
            {
                var parts = s.Split(ViewWordDelimiter);

                return $"{parts[0]}{DboWordDelimiter}{parts[1]}";
            }

            return $"{s}{DboWordDelimiter}{s}";
        }

        public static string ConvertTextToView(string s)
        {
            var sentences = s.Split('\n')
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(ConvertSentenceToView);

            return string.Join("", sentences);
        }

        private static string ConvertSentenceToView(string s)
        {
            var sentences = s.Split(new[] { ", " }, StringSplitOptions.None)
                .Select(ConvertWordToView);

            return string.Join(" ", sentences);
        }

        private static string ConvertWordToView(string s)
        {
            var parts = s.Split(DboWordDelimiter);

            if (parts[0].Length == 1)
            {
                var c = parts[0].First();

                if (PunctuationChars.Contains(c))
                {
                    return $"{c}";
                }
            }

            return $"{parts[0]}{ViewWordDelimiter}{parts[1]}";
        }

        private const char DboWordDelimiter = '/';
        private const char ViewWordDelimiter = '_';

        private static readonly HashSet<char> WordChars = new HashSet<char>(Enumerable.Range('A', 'Z' - 'A')
            .Concat(Enumerable.Range('a', 'z' - 'a'))
            .Concat(new[] { (int)ViewWordDelimiter })
            .Select(x => (char)x));

        private static readonly HashSet<char> PunctuationChars = new HashSet<char>(new[] { '.', ',', '!', '?', ';', ':', '(', ')' });

    }
}
