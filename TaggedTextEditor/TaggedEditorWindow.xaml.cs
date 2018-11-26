using System.IO;
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
    }
}
