using System;

namespace Tidy.Core
{
    /// <summary>
    ///     A message from Tidy.
    ///     (c) 1998-2000 (W3C) MIT, INRIA, Keio University
    ///     See Tidy.cs for the copyright notice.
    ///     Derived from
    ///     <a href="http://www.w3.org/People/Raggett/tidy">
    ///         HTML Tidy Release 4 Aug 2000
    ///     </a>
    /// </summary>
    /// <author>Seth Yates &lt;seth_yates@hotmail.com&gt; (translation to C#)</author>
    public class TidyMessage
    {
        private readonly int _column;
        private readonly string _filename;
        private readonly MessageLevel _level;
        private readonly int _line;
        private readonly string _message;

        internal TidyMessage(string filename, int line, int column, string message, MessageLevel level,
                             TidyOptions options)
        {
            Options = options;
            _filename = filename;
            _line = line;
            _column = column;
            _message = message;
            _level = level;
        }

        internal TidyMessage(Lexer lexer, string message, MessageLevel level)
        {
            _filename = String.Empty;
            _line = lexer.Lines;
            _column = lexer.Columns;
            _message = message;
            _level = level;
        }

        public TidyOptions Options { get; set; }

        public string Filename
        {
            get { return _filename; }
        }

        public int Line
        {
            get { return _line; }
        }

        public int Column
        {
            get { return _column; }
        }

        public string Message
        {
            get { return _message; }
        }

        public MessageLevel Level
        {
            get { return _level; }
        }

        public override string ToString()
        {
            string level;

            switch (Level)
            {
                case MessageLevel.Error:
                    level = Report.GetMessage("error");
                    break;

                case MessageLevel.Warning:
                    level = Report.GetMessage("warning");
                    break;

                default:
                    level = Report.GetMessage("info");
                    break;
            }

            return String.Format(Report.GetMessage("message_format"), Line, Column, level, Message);
        }
    }
}