using System.IO;
using System.Xml.Linq;
using Tidy.Dom;

/*
HTML parser and pretty printer

Copyright (c) 1998-2000 World Wide Web Consortium (Massachusetts
Institute of Technology, Institut National de Recherche en
Informatique et en Automatique, Keio University). All Rights
Reserved.

Contributing Author(s):

Dave Raggett <dsr@w3.org>
Andy Quick <ac.quick@sympatico.ca> (translation to Java)

The contributing author(s) would like to thank all those who
helped with testing, bug fixes, and patience.  This wouldn't
have been possible without all of you.

COPYRIGHT NOTICE:

This software and documentation is provided "as is," and
the copyright holders and contributing author(s) make no
representations or warranties, express or implied, including
but not limited to, warranties of merchantability or fitness
for any particular purpose or that the use of the software or
documentation will not infringe any third party patents,
copyrights, trademarks or other rights. 

The copyright holders and contributing author(s) will not be
liable for any direct, indirect, special or consequential damages
arising out of any use of the software or documentation, even if
advised of the possibility of such damage.

Permission is hereby granted to use, copy, modify, and distribute
this source code, or portions hereof, documentation and executables,
for any purpose, without fee, subject to the following restrictions:

1. The origin of this source code must not be misrepresented.
2. Altered versions must be plainly marked as such and must
not be misrepresented as being the original source.
3. This Copyright notice may not be removed or altered from any
source or altered source distribution.

The copyright holders and contributing author(s) specifically
permit, without fee, and encourage the use of this source code
as a component for supporting the Hypertext Markup Language in
commercial products. If you use this source code in a product,
acknowledgment is not required but would be appreciated.
*/

namespace Tidy.Core
{
    /// <summary>
    ///     <p>HTML parser and pretty printer</p>
    ///     <p>
    ///         (c) 1998-2000 (W3C) MIT, INRIA, Keio University
    ///         See Tidy.cs for the copyright notice.
    ///         Derived from
    ///         <a href="http://www.w3.org/People/Raggett/tidy">
    ///             HTML Tidy Release 4 Aug 2000
    ///         </a>
    ///     </p>
    ///     <p>
    ///         Copyright (c) 1998-2000 World Wide Web Consortium (Massachusetts
    ///         Institute of Technology, Institut National de Recherche en
    ///         Informatique et en Automatique, Keio University). All Rights
    ///         Reserved.
    ///     </p>
    ///     <p>
    ///         Contributing Author(s):
    ///         <br />
    ///         <a href="mailto:dsr@w3.org">Dave Raggett</a>
    ///         <br />
    ///         <a href="mailto:ac.quick@sympatico.ca">Andy Quick</a> (translation to Java)
    ///         <a href="mailto:seth_yates@hotmail.com">Seth Yates</a> (translation to C#)
    ///     </p>
    ///     <p>
    ///         The contributing author(s) would like to thank all those who
    ///         helped with testing, bug fixes, and patience.  This wouldn't
    ///         have been possible without all of you.
    ///     </p>
    ///     <p>
    ///         COPYRIGHT NOTICE:
    ///         <br />
    ///         This software and documentation is provided "as is," and
    ///         the copyright holders and contributing author(s) make no
    ///         representations or warranties, express or implied, including
    ///         but not limited to, warranties of merchantability or fitness
    ///         for any particular purpose or that the use of the software or
    ///         documentation will not infringe any third party patents,
    ///         copyrights, trademarks or other rights.
    ///     </p>
    ///     <p>
    ///         The copyright holders and contributing author(s) will not be
    ///         liable for any direct, indirect, special or consequential damages
    ///         arising out of any use of the software or documentation, even if
    ///         advised of the possibility of such damage.
    ///     </p>
    ///     <p>
    ///         Permission is hereby granted to use, copy, modify, and distribute
    ///         this source code, or portions hereof, documentation and executables,
    ///         for any purpose, without fee, subject to the following restrictions:
    ///     </p>
    ///     <p>
    ///         <ol>
    ///             <li>The origin of this source code must not be misrepresented.</li>
    ///             <li>
    ///                 Altered versions must be plainly marked as such and must
    ///                 not be misrepresented as being the original source.
    ///             </li>
    ///             <li>
    ///                 This Copyright notice may not be removed or altered from any
    ///                 source or altered source distribution.
    ///             </li>
    ///         </ol>
    ///     </p>
    ///     <p>
    ///         The copyright holders and contributing author(s) specifically
    ///         permit, without fee, and encourage the use of this source code
    ///         as a component for supporting the Hypertext Markup Language in
    ///         commercial products. If you use this source code in a product,
    ///         acknowledgment is not required but would be appreciated.
    ///     </p>
    /// </summary>
    /// <author>Dave Raggett &lt;dsr@w3.org&gt;</author>
    /// <author>Andy Quick &lt;ac.quick@sympatico.ca&gt; (translation to Java)</author>
    /// <author>Seth Yates &lt;seth_yates@hotmail.com&gt; (translation to C#)</author>
    /// <version>1.0, 1999/05/22</version>
    /// <version>1.0.1, 1999/05/29</version>
    /// <version>1.1, 1999/06/18 Java Bean</version>
    /// <version>1.2, 1999/07/10 Tidy Release 7 Jul 1999</version>
    /// <version>1.3, 1999/07/30 Tidy Release 26 Jul 1999</version>
    /// <version>1.4, 1999/09/04 DOM support</version>
    /// <version>1.5, 1999/10/23 Tidy Release 27 Sep 1999</version>
    /// <version>1.6, 1999/11/01 Tidy Release 22 Oct 1999</version>
    /// <version>1.7, 1999/12/06 Tidy Release 30 Nov 1999</version>
    /// <version>1.8, 2000/01/22 Tidy Release 13 Jan 2000</version>
    /// <version>1.9, 2000/06/03 Tidy Release 30 Apr 2000</version>
    /// <version>1.10, 2000/07/22 Tidy Release 8 Jul 2000</version>
    /// <version>1.11, 2000/08/16 Tidy Release 4 Aug 2000</version>
    public class HtmlTidy
    {
        private readonly TidyOptions _options = new TidyOptions();

        public HtmlTidy()
        {
            _options = new TidyOptions();

            AttributeTable at = AttributeTable.DefaultAttributeTable;
            if (at == null)
                return;

            var tt = new TagCollection {Options = _options};
            _options.TagTable = tt;
        }

        public TidyOptions Options
        {
            get { return _options; }
        }


        /// <summary>
        ///     Parses the input stream and writes to the output.
        /// </summary>
        /// <param name="input">The input stream</param>
        /// <param name="output">The output stream</param>
        /// <param name="messages">The messages</param>
        public void Parse(Stream input, Stream output, TidyMessageCollection messages=null)
        {
            messages = messages ?? new TidyMessageCollection();
            ParseInternal(input, output, messages);
        }

        public void Parse(string input, Stream output, TidyMessageCollection messages=null)
        {
            var html = Parse(input, messages);
            using (var writer = new StreamWriter(output))
                writer.Write(html);
        }

        public string Parse(Stream input, TidyMessageCollection messages=null)
        {
            var memoryStream = new MemoryStream();
            Parse(input, memoryStream, messages);
            memoryStream.Position = 0;
            using (var reader = new StreamReader(memoryStream))
                return reader.ReadToEnd();
        }

        public string Parse(string input, TidyMessageCollection messages=null)
        {
            var memoryStream = new MemoryStream();
            using (var writer = new StreamWriter(memoryStream))
            {
                writer.Write(input);
                var html = Parse(memoryStream, messages);
                return html;
            }
        }

        public XElement ParseXml(Stream input, TidyMessageCollection messages=null)
        {
            Options.DocType=DocType.Strict;
            Options.QuoteNbsp = false;
            Options.XmlOut = true;
            Options.Xhtml = true;

            var html=Parse(input, messages);
            return XElement.Parse(html);
        }

        /// <summary>
        ///     Internal routine that actually does the parsing.  The caller
        ///     can pass either an InputStream or file name.  If both are passed,
        ///     the file name is preferred.
        /// </summary>
        internal Node ParseInternal(Stream input, Stream output, TidyMessageCollection messages)
        {
            Node document = null;
            Out o = new OutImpl(); /* normal output stream */

            /* ensure config is self-consistent */
            _options.Adjust();


            if (input != null)
            {
                var lexer = new Lexer(new ClsStreamInImpl(input, _options.CharEncoding, _options.TabSize), _options)
                    {
                        Messages = messages
                    };

                /*
				store pointer to lexer in input stream
				to allow character encoding errors to be
				reported
				*/
                lexer.Input.Lexer = lexer;

                /* Tidy doesn't alter the doctype for generic XML docs */
                Node doctype;
                if (_options.XmlTags)
                {
                    document = ParserImpl.ParseXmlDocument(lexer);
                }
                else
                {
                    document = ParserImpl.ParseDocument(lexer);

                    if (!document.CheckNodeIntegrity())
                    {
                        Report.BadTree(lexer);
                        return null;
                    }

                    var cleaner = new Clean(_options.TagTable);

                    /* simplifies <b><b> ... </b> ...</b> etc. */
                    cleaner.NestedEmphasis(document);

                    /* cleans up <dir>indented text</dir> etc. */
                    cleaner.List2Bq(document);
                    cleaner.Bq2Div(document);

                    /* replaces i by em and b by strong */
                    if (_options.LogicalEmphasis)
                    {
                        cleaner.EmFromI(document);
                    }

                    if (_options.Word2000 && cleaner.IsWord2000(document, _options.TagTable))
                    {
                        /* prune Word2000's <![if ...]> ... <![endif]> */
                        cleaner.DropSections(lexer, document);

                        /* drop style & class attributes and empty p, span elements */
                        cleaner.CleanWord2000(lexer, document);
                    }

                    /* replaces presentational markup by style rules */
                    if (_options.MakeClean || _options.DropFontTags)
                    {
                        cleaner.CleanTree(lexer, document);
                    }

                    if (!document.CheckNodeIntegrity())
                    {
                        Report.BadTree(lexer);
                        return null;
                    }
                    doctype = document.FindDocType();
                    if (document.Content != null)
                    {
                        if (_options.Xhtml)
                        {
                            lexer.SetXhtmlDocType(document);
                        }
                        else
                        {
                            lexer.FixDocType(document);
                        }

                        if (_options.TidyMark)
                        {
                            lexer.AddGenerator(document);
                        }
                    }

                    /* ensure presence of initial <?XML version="1.0"?> */
                    if (_options.XmlOut && _options.XmlPi)
                    {
                        lexer.FixXmlPi(document);
                    }

                    if (document.Content != null)
                    {
                        Report.ReportVersion(lexer, doctype);
                        Report.ReportNumWarnings(lexer);
                    }
                }

                if (lexer.Messages.Errors > 0)
                {
                    Report.NeedsAuthorIntervention(lexer);
                }

                o.State = StreamIn.FSM_ASCII;
                o.Encoding = _options.CharEncoding;

                if (lexer.Messages.Errors == 0)
                {
                    PPrint pprint;
                    if (_options.BurstSlides)
                    {
                        /*
						remove doctype to avoid potential clash with
						markup introduced when bursting into slides
						*/
                        /* discard the document type */
                        doctype = document.FindDocType();

                        if (doctype != null)
                        {
                            Node.DiscardElement(doctype);
                        }

                        /* slides use transitional features */
                        lexer.Versions |= HtmlVersion.Html40Loose;

                        /* and patch up doctype to match */
                        if (_options.Xhtml)
                        {
                            lexer.SetXhtmlDocType(document);
                        }
                        else
                        {
                            lexer.FixDocType(document);
                        }

                        /* find the body element which may be implicit */
                        Node body = document.FindBody(_options.TagTable);

                        if (body != null)
                        {
                            pprint = new PPrint(_options);
                            Report.ReportNumberOfSlides(lexer, pprint.CountSlides(body));
                            pprint.CreateSlides(lexer, document);
                        }
                        else
                        {
                            Report.MissingBody(lexer);
                        }
                    }
                    else if (output != null)
                    {
                        pprint = new PPrint(_options);
                        o.Output = output;

                        if (_options.XmlTags)
                        {
                            pprint.PrintXmlTree(o, 0, 0, lexer, document);
                        }
                        else
                        {
                            pprint.PrintTree(o, 0, 0, lexer, document);
                        }

                        pprint.FlushLine(o, 0);
                    }
                }

                Report.ErrorSummary(lexer);
            }

            return document;
        }

        /// <summary>
        ///     Parses InputStream in and returns a DOM Document node.
        ///     If out is non-null, pretty prints to OutputStream out.
        /// </summary>
        internal virtual IDocument ParseDom(Stream input, Stream output, TidyMessageCollection messages)
        {
            Node document = ParseInternal(input, output, messages);
            if (document != null)
                return (IDocument) document.Adapter;
            return null;
        }

        /// <summary> Creates an empty DOM Document.</summary>
        internal static IDocument CreateEmptyDocument()
        {
            var document = new Node(Node.ROOT_NODE, new byte[0], 0, 0);
            var node = new Node(Node.START_TAG, new byte[0], 0, 0, "html", new TagCollection());
            Node.InsertNodeAtStart(document, node);
            return (IDocument) document.Adapter;
        }

        /// <summary>Pretty-prints a DOM Document.</summary>
        internal virtual void PrettyPrint(IDocument doc, Stream output)
        {
            Out o = new OutImpl();

            if (!(doc is DomDocumentImpl))
            {
                return;
            }

            Node document = ((DomDocumentImpl) doc).Adaptee;

            o.State = StreamIn.FSM_ASCII;
            o.Encoding = _options.CharEncoding;

            if (output == null) return;
            var pprint = new PPrint(_options);
            o.Output = output;

            if (_options.XmlTags)
                pprint.PrintXmlTree(o, 0, 0, null, document);
            else
                pprint.PrintTree(o, 0, 0, null, document);

            pprint.FlushLine(o, 0);
        }
    }
}