using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;

namespace NetExt.Shim
{
    public class SourceWindow : Window
    {

        static Dictionary<string, TextEditor> sourceFiles = new Dictionary<string, TextEditor>();

        private static SourceWindow instance = null;
        TabControl tabControl;
        public SourceWindow()
        {
            this.AllowsTransparency = false;
            this.WindowStyle = WindowStyle.SingleBorderWindow;
            this.Background = Brushes.White;
            this.Topmost = true;

            this.Width = 400;
            this.Height = 300;
            this.Title = "Source";

            //sourceFiles = new Dictionary<string, TextEditor>();


            tabControl = new TabControl();

            DockPanel panel = new DockPanel();
            panel.Children.Add(tabControl);

            panel.ClipToBounds = true;


            this.Content = panel;
            this.Closing += SourceWindow_Closing;

        }

        private void SourceWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.Visibility == Visibility.Hidden)
            {
                e.Cancel = false;
                return;
            }
            this.Hide();
            e.Cancel = true;
        }

        public static bool HasInstance
        {
            get
            {
                return instance != null;
            }
        }

        public static bool HasDocument
        {
            get
            {
                return sourceFiles.Count != 0;
            }
        }
        public static SourceWindow GetInstance()
        {
            if (instance == null)
                instance = new SourceWindow();
            return instance;
        }

        public bool SelectEditor(string Path)
        {
            if (sourceFiles.ContainsKey(Path))
            {
                for (int i = 0; i < tabControl.Items.Count; i++)
                {
                    if ((tabControl.Items[i] as TabItem).Tag.ToString() == Path)
                    {
                        tabControl.SelectedIndex = i;
                        return true;
                    }
                }
            }
            return false;
        }

        public bool LoadFile(string Path)
        {

            if (SelectEditor(Path))
                return true;
            TextEditor editor = new TextEditor();

            editor.Name = System.IO.Path.GetFileNameWithoutExtension(Path).Replace('.', '_');
            editor.ClipToBounds = true;

            //editor.Document.Text = "namespace Abc.Def.Gh\n{\n\tclass AB\n\t{\n\t\tAB() {};\n\t}\n}";
            editor.ShowLineNumbers = true;
            editor.IsReadOnly = true;
            editor.Tag = Path;

            try
            {
                try
                {
                    var typeConverter = new HighlightingDefinitionTypeConverter();
                    var csSyntaxHighlighter = (IHighlightingDefinition)typeConverter.ConvertFrom("C#");
                    editor.SyntaxHighlighting = csSyntaxHighlighter;

                }
                catch { };
                try
                {
                    editor.Load(Path);
                    TabItem item = new TabItem();
                    item.Header = System.IO.Path.GetFileName(Path);
                    item.ToolTip = Path;
                    item.Content = editor;
                    item.Tag = Path;
                    tabControl.Items.Add(item);
                    tabControl.SelectedIndex = tabControl.Items.Count - 1;
                    sourceFiles[Path] = editor;
                    //string sourceString = File.ReadAllText(Path);
                    //editor.Document.Text = sourceString;

                }
                catch
                {
                    editor.Document.Text = "namespace Abc.Def.Gh\n{\n\tclass AB\n\t{\n\t\tAB() {};\n\t}\n}";
                    return false;
                }

                this.Title = Path;


                return true;
            }
            catch
            { };
            return false;
        }
        public void SelectLine(string Path, int LineNumber)
        {
            if (!sourceFiles.ContainsKey(Path))
                return;
            var editor = sourceFiles[Path];
            var line = editor.Document.GetLineByNumber(LineNumber);
            if (line == null)
                return;
            editor.SelectionStart = line.Offset;
            editor.SelectionLength = line.Length;

        }

        public void UnselectLine(string Path, int LineNumber)
        {
            if (!sourceFiles.ContainsKey(Path))
                return;
            var editor = sourceFiles[Path];
            var line = editor.Document.GetLineByNumber(LineNumber);
            if (line == null)
                return;
            editor.SelectionStart = line.Offset;
            editor.SelectionLength = 0;
        }


        public void HighLightLine(string Path, int Line)
        {
            if (!sourceFiles.ContainsKey(Path))
                return;
            var editor = sourceFiles[Path];
            IBackgroundRenderer before = null;
            try
            {
                if (editor.TextArea.TextView != null && editor.TextArea.TextView.BackgroundRenderers != null)
                    before = editor.TextArea.TextView.BackgroundRenderers.First(r => r.GetType().ToString().Contains("HighlightCurrentLineBackgroundRenderer"));
            }
            catch
            {

            }
            if (before != null)
            {
                editor.TextArea.TextView.BackgroundRenderers.Remove(before);
            }
            var line = editor.Document.GetLineByNumber(Line);
            var backgroundRenderer = new HighlightCurrentLineBackgroundRenderer(editor, line, Brushes.LightGray);
            editor.TextArea.TextView.BackgroundRenderers.Add(backgroundRenderer);
            editor.InvalidateVisual();

            editor.ScrollToLine(Line);


        }

        public void MoveToLine(String Path, int Line)
        {
            if (!sourceFiles.ContainsKey(Path))
                return;
            var editor = sourceFiles[Path];
            editor.ScrollToLine(Line);
        }
        public void UnHighLightLine(string Path, int Line)
        {
            if (!sourceFiles.ContainsKey(Path))
                return;
            var editor = sourceFiles[Path];
            var before = editor.TextArea.TextView.BackgroundRenderers.First(r => r.GetType().ToString().Contains("HighlightCurrentLineBackgroundRenderer"));

            if (before != null)
            {
                editor.TextArea.TextView.BackgroundRenderers.Remove(before);
            }
        }

    }

    //Adapted from https://github.com/icsharpcode/AvalonEdit/issues/102
    public class HighlightCurrentLineBackgroundRenderer : IBackgroundRenderer
    {
        private TextEditor _editor;
        private IDocumentLine line;
        private Brush brush;
        public HighlightCurrentLineBackgroundRenderer(TextEditor Editor, IDocumentLine Line, Brush Brush)
        {
            _editor = Editor;
            line = Line;
            brush = Brush;
        }

        public KnownLayer Layer
        {
            get { return KnownLayer.Selection; }
        }

        public void Draw(TextView textView, DrawingContext drawingContext)
        {

            if (_editor.Document == null)
                return;

            textView.EnsureVisualLines();
            foreach (var rect1 in BackgroundGeometryBuilder.GetRectsForSegment(textView, line))
            {
                Rect rect = rect1;
                rect.Width = Math.Max(rect1.Width, 5000.0); // _editor.Width-10;

                drawingContext.DrawRectangle(brush, null, new Rect(rect.Location, new Size(rect.Width, rect.Height)));
            }

        }

    }

}
