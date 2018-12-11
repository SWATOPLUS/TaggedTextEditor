using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace TaggedTextEditor
{
    public partial class TaggedEditorWindow
    {
        private string _activeFileName;
        private string _activeFilePath;
        private readonly WinkProvider _winkProvider;

        private const string OtherGroupTag = "";
        private readonly IDictionary<string, string> _tagsDictionary;
        private readonly IDictionary<string, string> _tagsGroupDictionary = new Dictionary<string, string>()
        {
            {"VB", "Verb"},
            {"NN", "Noun"},
            {"JJ", "Adjective"},
            {"RB", "Adverb"},
            {OtherGroupTag, "Other"}
        };

        public TaggedEditorWindow()
        {
            InitializeComponent();
            _winkProvider = new WinkProvider();
            var tagsJson = File.ReadAllText("Database/tags.json");
            var tagsDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(tagsJson);
            _tagsDictionary = new ConcurrentDictionary<string, string>(tagsDict);
            RebuildEditContextMenu();
        }
        
        private void AutoTag()
        {
            var text = GetText();
            var taggedText = _winkProvider.TagText(text);
            var viewText = TextConverter.ConvertTextToView(taggedText);

            DocumentText.Document = GetDocument(viewText);
        }

        private static FlowDocument GetDocument(string viewText)
        {
            var lines = viewText.Split('\n');

            var doc = new FlowDocument();

            foreach (var line in lines)
            {
                var p = new Paragraph();
                p.Inlines.AddRange(BuildInlineItems(line));
                doc.Blocks.Add(p);
            }

            return doc;
        }

        private void New()
        {
            DocumentText.Document = new FlowDocument();
            _activeFileName = "Untitled";
            _activeFilePath = null;
            UpdateTitle();
        }

        private void Open()
        {
            var dlg = new OpenFileDialog
            {
                FileName = _activeFileName,
                DefaultExt = ".txt",
                Filter = "Text documents (.txt)|*.txt"
            };

            var result = dlg.ShowDialog();

            if (result == true)
            {
                _activeFilePath = dlg.FileName;
                _activeFileName = new FileInfo(_activeFilePath).Name;

                using(var tr = new StreamReader(_activeFilePath))
                {
                    var text = tr.ReadToEnd();
                    SetText(text);
                }

                UpdateTitle();
            }
        }

        private void Save()
        {
            if (string.IsNullOrEmpty(_activeFilePath))
            {
                SaveAs();
            }
            else
            {
                using (var tr = new StreamWriter(_activeFilePath))
                {
                    tr.Write(GetText());
                }
            }
        }

        private void SaveAs()
        {
            var dlg = new SaveFileDialog
            {
                FileName = _activeFileName,
                DefaultExt = ".txt",
                Filter = "Text documents (.txt)|*.txt"
            };

            var result = dlg.ShowDialog();

            if (result == true)
            {
                _activeFilePath = dlg.FileName;
                using (TextWriter tr = new StreamWriter(_activeFilePath))
                {
                    tr.Write(GetText());
                }
                UpdateTitle();
            }
        }

        private void MenuHandler_Click(object sender, RoutedEventArgs e)
        {
            var item = sender as MenuItem;
            switch (item?.Name)
            {
                case "AutoTagMenu":
                    AutoTag();
                    break;
                    
                case "NewMenu":
                    New();
                    break;

                case "OpenMenu":
                    Open();
                    break;

                case "SaveMenu":
                    Save();
                    break;

                case "SaveAsMenu":
                    SaveAs();
                    break;

                case "ExitMenu":
                    Close();
                    break;
            }
        }

        private void DocumentText_KeyDown(object sender, KeyEventArgs e)
        {
            UpdateStatus();
        }

        private void DocumentText_IsKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateStatus();
        }

        private void UpdateTitle()
        {
            Window.Title = _activeFileName + " - " + "POS tagger";
        }

        private void UpdateStatus()
        {
            //var caret = DocumentText.CaretIndex;
            //var row = DocumentText.GetLineIndexFromCharacterIndex(caret);
            //var col = caret - DocumentText.GetFirstVisibleLineIndex();
            //StatusBar.Text = $"Ln {row}, Col {col}";
        }

        private void RebuildEditContextMenu()
        {
            var items = DocumentTextContextMenu.Items;
            items.Clear();
            items.Add(DefaultEditMenu);
            items.Add(EditingWordMenu);

            var grouping = _tagsDictionary.GroupBy(x => x.Key.Substring(0, 2)).ToDictionary(x=> x.Key);

            var otherItems = new List<KeyValuePair<string, string>>();

            foreach (var group in grouping)
            {
                if (_tagsGroupDictionary.ContainsKey(group.Key))
                {
                    items.Add(BuildEditContextMenuGroup(group.Key, group.Value));
                }
                else
                {
                    otherItems.AddRange(group.Value);
                }
            }

            items.Add(BuildEditContextMenuGroup(OtherGroupTag, otherItems));
        }

        private MenuItem BuildEditContextMenuGroup(string nameTag, IEnumerable<KeyValuePair<string, string>> items)
        {
            var group = new MenuItem
            {
                Header = _tagsGroupDictionary[nameTag]
            };

            foreach (var x in items)
            {
                var item = new MenuItem
                {
                    Header = $"{x.Value} ({x.Key})",
                    Tag = x.Key
                };

                item.Click += TagMenuItem_OnClick;

                group.Items.Add(item);
            }

            return group;
        }

        private void EditContextMenu_OnOpened(object sender, RoutedEventArgs e)
        {
            var caret = DocumentText.CaretPosition;
            var caretPos = caret.GetTextRunLength(LogicalDirection.Backward);
            var block = DocumentText.CaretPosition.Paragraph ?? new Paragraph();
            var oldText = new TextRange(block.ContentStart, block.ContentEnd).Text;
            
            foreach (var inline in block.Inlines)
            {
                if (inline.ContentStart.GetOffsetToPosition(caret) >= 0
                    && inline.ContentEnd.GetOffsetToPosition(caret) <= 0)
                {
                    break;
                }

                caretPos += inline.ContentStart.GetOffsetToPosition(inline.ContentEnd);
            }

            var startPos = FindStartOfWord(oldText, caretPos);
            var wordSize = FindEndOfWord(oldText, startPos);

            var word = oldText.Substring(startPos, wordSize);

            EditingWordMenu.Header = word.Replace("_", "__");
        }

        private void TagMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var caret = DocumentText.CaretPosition;
            var caretPos = caret.GetTextRunLength(LogicalDirection.Backward);
            var block = DocumentText.CaretPosition.Paragraph ?? new Paragraph();
            var oldText = new TextRange(block.ContentStart, block.ContentEnd).Text;

            foreach (var inline in block.Inlines)
            {
                if (inline.ContentStart.GetOffsetToPosition(caret) >= 0
                    && inline.ContentEnd.GetOffsetToPosition(caret) <= 0)
                {
                    break;
                }

                caretPos += inline.ContentStart.GetOffsetToPosition(inline.ContentEnd);
            }

            var startPos = FindStartOfWord(oldText, caretPos);
            var wordSize = FindEndOfWord(oldText, startPos);

            var parts = oldText.Substring(startPos, wordSize).Split('_');

            var newWord = parts[0] + '_' + (sender as MenuItem)?.Tag;
            var newText = oldText.Substring(0, startPos) + newWord + oldText.Substring(startPos + wordSize);
            block.Inlines.Clear();
            block.Inlines.AddRange(BuildInlineItems(newText));
        }

        private static IEnumerable<Inline> BuildInlineItems(string sentence)
        {
            var list = new List<Inline>();

            var words = sentence.Split(' ');

            foreach (var word in words)
            {
                if (word.Contains("_"))
                {
                    var parts = word.Split('_');

                    list.Add(new Run(parts[0]));
                    list.Add(new Run("_"));
                    var tag = new Run(parts[1]) {Foreground = Brushes.Blue};

                    list.Add(tag);
                }
                else
                {
                    list.Add(new Run(word));
                }

                list.Add(new Run(" "));
            }

            return list;
        }

        private void SetText(string s)
        {
            if (s.Split(new[] {", "}, StringSplitOptions.None).First().Contains("/"))
            {
                s = TextConverter.ConvertTextToView(s);
            }

            DocumentText.Document = GetDocument(s);
        }

        private string GetText()
        {
            var document = DocumentText.Document;
            var text = new TextRange(document.ContentStart, document.ContentEnd).Text;

            if (text.Contains("_"))
            {
                text = TextConverter.ConvertTextToDbo(text);
            }

            return text;
        }

        private static int FindStartOfWord(string text, int caret)
        {
            var startPos = FindLast(text.Substring(0, caret), new[] { " ", "\n" });

            if (startPos == -1)
            {
                startPos = 0;
            }
            else
            {
                startPos += 1;
            }

            return startPos;
        }

        private static int FindEndOfWord(string text, int start)
        {
            var index = FindFirst(text.Substring(start), new[] {" ", "\n"});

            if (index == -1)
            {
                return text.Length - start;
            }

            return index;
        }

        private static int FindFirst(string s, IEnumerable<string> subStrings)
        {
            return subStrings
                .Select(x => s.IndexOf(x, StringComparison.Ordinal))
                .Where(x => x != -1)
                .DefaultIfEmpty(-1)
                .Min();
        }

        private static int FindLast(string s, IEnumerable<string> subStrings)
        {
            return subStrings
                .Select(x => s.LastIndexOf(x, StringComparison.Ordinal))
                .Where(x => x != -1)
                .DefaultIfEmpty(-1)
                .Max();
        }
    }
}
