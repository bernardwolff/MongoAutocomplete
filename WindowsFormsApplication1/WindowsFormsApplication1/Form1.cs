using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MongoDB.Bson;
using MongoDB.Driver;


namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetCaretPos(out Point lpPoint);

        public Form1()
        {
            InitializeComponent();
        }

        #region adapted from https://github.com/paralect/robomongo/blob/master/src/robomongo/gui/widgets/workarea/ScriptWidget.cpp

        // I basically just converted the C++ from the above link to the C# you see in this region (bgw)

        bool isForbiddenChar(char ch)
        {
            return ch == '\"' ||  ch == '\'';
        }

        bool isStopChar(char ch, bool direction)
        {
            if (ch == '='  ||  ch == ';'  ||
                ch == '('  ||  ch == ')'  ||
                ch == '{'  ||  ch == '}'  ||
                ch == '-'  ||  ch == '/'  ||
                ch == '+'  ||  ch == '*'  ||
                ch == '\r' ||  ch == '\n' ||
                ch == ' ' ) {
                    return true;
            }

            if (direction) { // right direction
                if (ch == '.')
                    return true;
            }

            return false;
        }

        string sanitizeForAutocompletion()
        {
            if (string.IsNullOrEmpty(textBox1.Text))
                return "";

            var start = textBox1.SelectionStart;

            int col = textBox1.SelectionStart - textBox1.GetFirstCharIndexOfCurrentLine();
            int row = textBox1.GetLineFromCharIndex(start);
            string line = textBox1.Lines[row];

            int leftStop = -1;
            for (int i = col - 1; i >= 0; --i)
            {
                var ch = line[i];

                if (isForbiddenChar(ch))
                    return "";

                if (isStopChar(ch, false))
                {
                    leftStop = i;
                    break;
                }
            }

            int rightStop = line.Length + 1;
            for (int i = col; i < line.Length; ++i)
            {
                char ch = line[i];

                if (isForbiddenChar(ch))
                    return "";

                if (isStopChar(ch, true))
                {
                    rightStop = i;
                    break;
                }
            }

            leftStop = leftStop + 1;
            //rightStop = rightStop - 1;
            //int len = ondemand ? col - leftStop : rightStop - leftStop + 1;
            int len = col - leftStop;

            return line.Substring(leftStop, len);
        }

        #endregion

        private void AutoComplete(string input)
        {
            var mongoServer = MongoServer.Create("mongodb://localhost");
            var database = mongoServer.GetDatabase("tt2");
            var script = File.ReadAllText("shellAutoComplete.js"); // "function() { return db.surveyTemplates.count(); }"
            var returnObj =
                database.Eval(new EvalArgs()
                {
                    Args = new[] { BsonValue.Create(input) },
                    Code = "function(query) { " + script + "\nshellAutocomplete(query);\nreturn __autocomplete__;}"
                });
            var suggestionsArray = returnObj.AsBsonArray;
            listBox1.Items.Clear();
            foreach (var suggestion in suggestionsArray)
            {
                listBox1.Items.Add(suggestion);
            }

            listBox1.Visible = suggestionsArray.Any();

            Point p;
            GetCaretPos(out p);
            listBox1.Location = new Point(p.X + textBox1.Location.X + 2, p.Y + textBox1.Location.Y + 20);
            listBox1.BringToFront();

        }

        private void button1_Click(object sender, EventArgs e)
        {
            
        }

        private void textBox1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            var sanitized = sanitizeForAutocompletion();
            if (string.IsNullOrEmpty(sanitized))
            {
                listBox1.Visible = false;
                return;
            }
                
            AutoComplete(sanitized);
        }

        private void textBox1_KeyUp(object sender, KeyEventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {

        }

    }
}
