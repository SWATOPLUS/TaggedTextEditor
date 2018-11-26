using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace TaggedTextEditor
{
    public partial class TaggedEditorWindow
    {
        private string _activeFileName;
        private string _activeFilePath;
        private readonly WinkProvider _winkProvider;

        public TaggedEditorWindow()
        {
            InitializeComponent();
            _winkProvider = new WinkProvider();
        }

        private void AutoTag()
        {
            DocumentText.Text = _winkProvider.TagText(DocumentText.Text);
        }

        private void New()
        {
            DocumentText.Text = "";
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
                    DocumentText.Text = tr.ReadToEnd();
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
                    tr.Write(DocumentText.Text);
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
                    tr.Write(DocumentText.Text);
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

                case "SaveAsTaggedMenu":
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
            var caret = DocumentText.CaretIndex;
            var row = DocumentText.GetLineIndexFromCharacterIndex(caret);
            var col = caret - DocumentText.GetFirstVisibleLineIndex();
            StatusBar.Text = $"Ln {row}, Col {col}";
        }

        private void EditContextMenu_OnOpened(object sender, RoutedEventArgs e)
        {
            var caret = DocumentText.CaretIndex;
            var oldText = DocumentText.Text;

            var startPos = FindStartOfWord(oldText, caret);
            var wordSize = FindEndOfWord(oldText, startPos);

            EditingWord.Header = oldText.Substring(startPos, wordSize);
        }

        private void TagMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var caret = DocumentText.CaretIndex;
            var oldText = DocumentText.Text;

            var startPos = FindStartOfWord(oldText, caret);
            var wordSize = FindEndOfWord(oldText, startPos);

            var parts = oldText.Substring(startPos, wordSize).Split('/');

            var newWord = parts[0] + '/' + (sender as MenuItem)?.Tag;
            var newText = oldText.Substring(0, startPos) + newWord + oldText.Substring(startPos + wordSize);
            DocumentText.Text = newText;
        }

        private static int FindStartOfWord(string text, int caret)
        {
            var startPos = FindLast(text.Substring(0, caret), new[] { ", ", "\n" });

            if (startPos == -1)
            {
                startPos = 0;
            }
            else
            {
                if (text[startPos] == '\n')
                {
                    startPos += 1;
                }
                else
                {
                    startPos += 2;
                }
            }

            return startPos;
        }

        private static int FindEndOfWord(string text, int start)
        {
            var index = FindFirst(text.Substring(start), new[] {", ", "\n"});

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
