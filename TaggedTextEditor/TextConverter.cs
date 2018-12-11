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

            return string.Join("\n", sentences);
        }

        private static string ConvertSentenceToDbo(string s)
        {
            var words = s.Split(' ')
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(ConvertWordToDbo);

            return string.Join(", ", words);
        }

        private static string ConvertWordToDbo(string s)
        {
            if (s.Contains(ViewWordDelimiter))
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

            return string.Join("\n", sentences);
        }

        private static string ConvertSentenceToView(string s)
        {
            var words = s.Split(new[] { ", " }, StringSplitOptions.None)
                .Select(ConvertWordToView);

            return string.Join(" ", words);
        }

        private static string ConvertWordToView(string s)
        {
            if (s.Contains(DboWordDelimiter))
            {
                var parts = s.Split(DboWordDelimiter);

                return $"{parts[0]}{ViewWordDelimiter}{parts[1]}";
            }

            return s;
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
