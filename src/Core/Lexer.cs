using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Tidy.Core
{
    /// <summary>
    ///     Lexer for html parser
    ///     (c) 1998-2000 (W3C) MIT, INRIA, Keio University
    ///     See Tidy.cs for the copyright notice.
    ///     Derived from
    ///     <a href="http://www.w3.org/People/Raggett/tidy">
    ///         HTML Tidy Release 4 Aug 2000
    ///     </a>
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
    /// <remarks>
    ///     Given a file stream fp it returns a sequence of tokens.
    ///     GetToken(fp) gets the next token
    ///     UngetToken(fp) provides one level undo
    ///     The tags include an attribute list:
    ///     - linked list of attribute/value nodes
    ///     - each node has 2 null-terminated strings.
    ///     - entities are replaced in attribute values
    ///     white space is compacted if not in preformatted mode
    ///     If not in preformatted mode then leading white space
    ///     is discarded and subsequent white space sequences
    ///     compacted to single space chars.
    ///     If XmlTags is no then Tag names are folded to upper
    ///     case and attribute names to lower case.
    ///     Not yet done:
    ///     -   Doctype subset and marked sections
    /// </remarks>
    internal class Lexer
    {
        public const short IGNORE_WHITESPACE = 0;
        public const short MIXED_CONTENT = 1;
        public const short PREFORMATTED = 2;
        public const short IGNORE_MARKUP = 3;
        private const short DIGIT = 1;
        private const short LETTER = 2;
        private const short NAMECHAR = 4;
        private const short WHITE = 8;
        private const short NEWLINE = 16;
        private const short LOWERCASE = 32;
        private const short UPPERCASE = 64;

        /* lexer GetToken states */

        private const short LEX_CONTENT = 0;
        private const short LEX_GT = 1;
        private const short LEX_ENDTAG = 2;
        private const short LEX_STARTTAG = 3;
        private const short LEX_COMMENT = 4;
        private const short LEX_DOCTYPE = 5;
        private const short LEX_PROCINSTR = 6;
        //private const short LEX_ENDCOMMENT = 7;
        private const short LEX_CDATA = 8;
        private const short LEX_SECTION = 9;
        private const short LEX_ASP = 10;
        private const short LEX_JSTE = 11;
        private const short LEX_PHP = 12;
        private const string VOYAGER_LOOSE = "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd";
        private const string VOYAGER_STRICT = "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd";
        private const string VOYAGER_FRAMESET = "http://www.w3.org/TR/xhtml1/DTD/xhtml1-frameset.dtd";

        private const string XHTML_NAMESPACE = "http://www.w3.org/1999/xhtml";

        private static readonly W3CVersionInfo[] W3CVersion = new[]
            {
                new W3CVersionInfo("HTML 4.01", "XHTML 1.0 Strict", VOYAGER_STRICT, HtmlVersion.Html40Strict),
                new W3CVersionInfo("HTML 4.01 Transitional", "XHTML 1.0 Transitional", VOYAGER_LOOSE,
                                   HtmlVersion.Html40Loose),
                new W3CVersionInfo("HTML 4.01 Frameset", "XHTML 1.0 Frameset", VOYAGER_FRAMESET, HtmlVersion.Frames),
                new W3CVersionInfo("HTML 4.0", "XHTML 1.0 Strict", VOYAGER_STRICT, HtmlVersion.Html40Strict),
                new W3CVersionInfo("HTML 4.0 Transitional", "XHTML 1.0 Transitional", VOYAGER_LOOSE,
                                   HtmlVersion.Html40Loose),
                new W3CVersionInfo("HTML 4.0 Frameset", "XHTML 1.0 Frameset", VOYAGER_FRAMESET, HtmlVersion.Frames),
                new W3CVersionInfo("HTML 3.2", "XHTML 1.0 Transitional", VOYAGER_LOOSE, HtmlVersion.Html32),
                new W3CVersionInfo("HTML 2.0", "XHTML 1.0 Strict", VOYAGER_STRICT, HtmlVersion.Html20)
            };

        /* used to classify chars for lexical purposes */
        private static readonly int[] Lexmap = new int[128];
        private readonly List<Node> _nodeList;
        public int BadAccess; /* for accessibility errors */
        public int BadChars; /* for bad char encodings */
        public bool BadDoctype; /* e.g. if html or PUBLIC is missing */
        public int BadForm; /* for mismatched/mispositioned form tags */
        public int BadLayout; /* for bad style errors */
        public int Columns; /* at start of current token */
        public HtmlVersion Doctype; /* version as given by doctype (if any) */
        public bool ExcludeBlocks; /* Netscape compatibility */
        public bool Exiled; /* true if moved out of table */

        /* Inline stack for compatibility with Mosaic */
        public Node Inode; /* for deferring text node */
        public StreamIn Input; /* file stream */
        public int Insert; /* for inferring inline tags */
        public bool Insertspace; /* when space is moved after end tag */
        public Stack<InlineStack> Istack;
        public int Istackbase; /* start of frame */
        public bool Isvoyager; /* true if xmlns attribute on html element */
        public byte[] Lexbuf; /* byte buffer of UTF-8 chars */
        public int Lexlength; /* allocated */
        public int Lexsize; /* used */
        public int Lines; /* lines seen */
        public TidyMessageCollection Messages = new TidyMessageCollection();
        public TidyOptions Options = new TidyOptions();
        public bool Pushed; /* true after token has been pushed back */

        protected internal int SeenBodyEndTag; /* used by parser */
        public short State; /* state of lexer's finite state machine */
        public Style Styles; /* used for cleaning up presentation markup */
        public Node Token;
        public int Txtend; /* end of current node */
        public int Txtstart; /* start of current node */
        public HtmlVersion Versions; /* bit vector of HTML versions */
        public bool Waswhite; /* used to collapse contiguous white space */

        static Lexer()
        {
            MapStr("\r\n\f", (NEWLINE | WHITE));
            MapStr(" \t", WHITE);
            MapStr("-.:_", NAMECHAR);
            MapStr("0123456789", (DIGIT | NAMECHAR));
            MapStr("abcdefghijklmnopqrstuvwxyz", (LOWERCASE | LETTER | NAMECHAR));
            MapStr("ABCDEFGHIJKLMNOPQRSTUVWXYZ", (UPPERCASE | LETTER | NAMECHAR));
        }

        public Lexer(StreamIn input, TidyOptions options)
        {
            Input = input;
            Lines = 1;
            Columns = 1;
            State = LEX_CONTENT;
            BadAccess = 0;
            BadLayout = 0;
            BadChars = 0;
            BadForm = 0;
            Waswhite = false;
            Pushed = false;
            Insertspace = false;
            Exiled = false;
            Isvoyager = false;
            Versions = HtmlVersion.Everything;
            Doctype = HtmlVersion.Unknown;
            BadDoctype = false;
            Txtstart = 0;
            Txtend = 0;
            Token = null;
            Lexbuf = null;
            Lexlength = 0;
            Lexsize = 0;
            Inode = null;
            Insert = - 1;
            Istack = new Stack<InlineStack>();
            Istackbase = 0;
            Styles = null;
            Options = options;
            SeenBodyEndTag = 0;
            _nodeList = new List<Node>();
        }

        public virtual Node NewNode()
        {
            var node = new Node();
            _nodeList.Add(node);
            return node;
        }

        public virtual Node NewNode(short type, byte[] textarray, int start, int end)
        {
            var node = new Node(type, textarray, start, end);
            _nodeList.Add(node);
            return node;
        }

        public virtual Node NewNode(short type, byte[] textarray, int start, int end, string element)
        {
            var node = new Node(type, textarray, start, end, element, Options.TagTable);
            _nodeList.Add(node);
            return node;
        }

        public virtual Node CloneNode(Node node)
        {
            var cnode = (Node) node.Clone();
            _nodeList.Add(cnode);
            for (AttVal att = cnode.Attributes; att != null; att = att.Next)
            {
                if (att.Asp != null)
                    _nodeList.Add(att.Asp);
                if (att.Php != null)
                    _nodeList.Add(att.Php);
            }
            return cnode;
        }

        public virtual AttVal CloneAttributes(AttVal attrs)
        {
            var cattrs = (AttVal) attrs.Clone();
            for (AttVal att = cattrs; att != null; att = att.Next)
            {
                if (att.Asp != null)
                    _nodeList.Add(att.Asp);
                if (att.Php != null)
                    _nodeList.Add(att.Php);
            }
            return cattrs;
        }

        protected internal virtual void UpdateNodeTextArrays(byte[] oldtextarray, byte[] newtextarray)
        {
            foreach (Node node in _nodeList)
            {
                if (node.Textarray == oldtextarray)
                    node.Textarray = newtextarray;
            }
        }

        /* used for creating preformatted text from Word2000 */

        public virtual Node NewLineNode()
        {
            Node node = NewNode();

            node.Textarray = Lexbuf;
            node.Start = Lexsize;
            AddCharToLexer('\n');
            node.End = Lexsize;
            return node;
        }

        // Should always be able convert to/from UTF-8, so encoding exceptions are
        // converted to an Error to avoid adding throws declarations in
        // lots of methods.

        public static byte[] GetBytes(string str)
        {
            try
            {
                return Encoding.UTF8.GetBytes(str);
            }
            catch (IOException e)
            {
                throw new Exception("string to UTF-8 conversion failed: " + e.Message);
            }
        }

        public static string GetString(byte[] bytes, int offset, int length)
        {
            try
            {
                return Encoding.UTF8.GetString(bytes, offset, length);
            }
            catch (IOException e)
            {
                throw new Exception("UTF-8 to string conversion failed: " + e.Message);
            }
        }

        public virtual bool EndOfInput()
        {
            return Input.IsEndOfStream;
        }

        public virtual void AddByte(int c)
        {
            if (Lexsize + 1 >= Lexlength)
            {
                while (Lexsize + 1 >= Lexlength)
                {
                    if (Lexlength == 0)
                        Lexlength = 8192;
                    else
                        Lexlength = Lexlength*2;
                }

                byte[] temp = Lexbuf;
                Lexbuf = new byte[Lexlength];
                if (temp != null)
                {
                    Array.Copy(temp, 0, Lexbuf, 0, temp.Length);
                    UpdateNodeTextArrays(temp, Lexbuf);
                }
            }

            Lexbuf[Lexsize++] = (byte) c;
            Lexbuf[Lexsize] = (byte) '\x0000'; /* debug */
        }

        public virtual void ChangeChar(byte c)
        {
            if (Lexsize > 0)
            {
                Lexbuf[Lexsize - 1] = c;
            }
        }

        /* store char c as UTF-8 encoded byte stream */

        public virtual void AddCharToLexer(int c)
        {
            if (c < 128)
            {
                AddByte(c);
            }
            else if (c <= 0x7FF)
            {
                AddByte(0xC0 | (c >> 6));
                AddByte(0x80 | (c & 0x3F));
            }
            else if (c <= 0xFFFF)
            {
                AddByte(0xE0 | (c >> 12));
                AddByte(0x80 | ((c >> 6) & 0x3F));
                AddByte(0x80 | (c & 0x3F));
            }
            else if (c <= 0x1FFFFF)
            {
                AddByte(0xF0 | (c >> 18));
                AddByte(0x80 | ((c >> 12) & 0x3F));
                AddByte(0x80 | ((c >> 6) & 0x3F));
                AddByte(0x80 | (c & 0x3F));
            }
            else
            {
                AddByte(0xF8 | (c >> 24));
                AddByte(0x80 | ((c >> 18) & 0x3F));
                AddByte(0x80 | ((c >> 12) & 0x3F));
                AddByte(0x80 | ((c >> 6) & 0x3F));
                AddByte(0x80 | (c & 0x3F));
            }
        }

        public virtual void AddStringToLexer(string str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                AddCharToLexer(str[i]);
            }
        }

        /*
		No longer attempts to insert missing ';' for unknown
		enitities unless one was present already, since this
		gives unexpected results.
		
		For example:   <a href="something.htm?foo&bar&fred">
		was tidied to: <a href="something.htm?foo&amp;bar;&amp;fred;">
		rather than:   <a href="something.htm?foo&amp;bar&amp;fred">
		
		My thanks for Maurice Buxton for spotting this.
		*/

        public virtual void ParseEntity(short mode)
        {
            int start;
            bool first = true;
            bool semicolon = false;
            bool numeric = false;
            int c, ch, startcol;

            start = Lexsize - 1; /* to start at "&" */
            startcol = Input.CursorColumn - 1;

            while (true)
            {
                c = Input.ReadChar();
                if (c == StreamIn.END_OF_STREAM)
                {
                    break;
                }
                if (c == ';')
                {
                    semicolon = true;
                    break;
                }

                if (first && c == '#')
                {
                    AddCharToLexer(c);
                    first = false;
                    numeric = true;
                    continue;
                }

                first = false;
                short map = Map((char) c);

                /* AQ: Added flag for numeric entities so that numeric entities
				with missing semi-colons are recognized.
				Eg. "&#114e&#112;..." is recognized as "rep"
				*/
                if (numeric && ((c == 'x') || ((map & DIGIT) != 0)))
                {
                    AddCharToLexer(c);
                    continue;
                }
                if (!numeric && ((map & NAMECHAR) != 0))
                {
                    AddCharToLexer(c);
                    continue;
                }

                /* otherwise put it back */

                Input.UngetChar(c);
                break;
            }

            string str = GetString(Lexbuf, start, Lexsize - start);
            ch = EntityTable.DefaultEntityTable.EntityCode(str);

            /* deal with unrecognized entities */
            if (ch <= 0)
            {
                /* set error position just before offending chararcter */
                Lines = Input.CursorLine;
                Columns = startcol;

                if (Lexsize > start + 1)
                {
                    Report.EntityError(this, Report.UNKNOWN_ENTITY, str, ch);

                    if (semicolon)
                        AddCharToLexer(';');
                }
                    /* naked & */
                else
                {
                    Report.EntityError(this, Report.UNESCAPED_AMPERSAND, str, ch);
                }
            }
            else
            {
                if (c != ';')
                    /* issue warning if not terminated by ';' */
                {
                    /* set error position just before offending chararcter */
                    Lines = Input.CursorLine;
                    Columns = startcol;
                    Report.EntityError(this, Report.MISSING_SEMICOLON, str, c);
                }

                Lexsize = start;

                if (ch == 160 && (mode & PREFORMATTED) != 0)
                    ch = ' ';

                AddCharToLexer(ch);

                if (ch == '&' && !Options.QuoteAmpersand)
                {
                    AddCharToLexer('a');
                    AddCharToLexer('m');
                    AddCharToLexer('p');
                    AddCharToLexer(';');
                }
            }
        }

        public virtual char ParseTagName()
        {
            short map;
            int c;

            /* fold case of first char in buffer */

            c = Lexbuf[Txtstart];
            map = Map((char) c);

            if (!Options.XmlTags && (map & UPPERCASE) != 0)
            {
                c += ('a' - 'A');
                Lexbuf[Txtstart] = (byte) c;
            }

            while (true)
            {
                c = Input.ReadChar();
                if (c == StreamIn.END_OF_STREAM)
                {
                    break;
                }
                map = Map((char) c);

                if ((map & NAMECHAR) == 0)
                {
                    break;
                }

                /* fold case of subsequent chars */

                if (!Options.XmlTags && (map & UPPERCASE) != 0)
                {
                    c += ('a' - 'A');
                }

                AddCharToLexer(c);
            }

            Txtend = Lexsize;
            return (char) c;
        }

        public virtual void AddStringLiteral(string str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                AddCharToLexer(str[i]);
            }
        }

        /* choose what version to use for new doctype */

        public virtual HtmlVersion GetHtmlVersion()
        {
            if ((Versions & HtmlVersion.Html20) != HtmlVersion.Unknown)
            {
                return HtmlVersion.Html20;
            }

            if ((Versions & HtmlVersion.Html32) != HtmlVersion.Unknown)
            {
                return HtmlVersion.Html32;
            }

            if ((Versions & HtmlVersion.Html40Strict) != HtmlVersion.Unknown)
            {
                return HtmlVersion.Html40Strict;
            }

            if ((Versions & HtmlVersion.Html40Loose) != HtmlVersion.Unknown)
            {
                return HtmlVersion.Html40Loose;
            }

            if ((Versions & HtmlVersion.Frames) != HtmlVersion.Unknown)
            {
                return HtmlVersion.Frames;
            }

            return HtmlVersion.Unknown;
        }

        public virtual string HtmlVersionName()
        {
            HtmlVersion guessed = ApparentVersion();
            int j;

            for (j = 0; j < W3CVersion.Length; ++j)
            {
                if (guessed == W3CVersion[j].Version)
                {
                    if (Isvoyager)
                    {
                        return W3CVersion[j].VoyagerName;
                    }

                    return W3CVersion[j].Name;
                }
            }

            return null;
        }

        /* add meta element for Tidy */

        public virtual bool AddGenerator(Node root)
        {
            Node head = root.FindHead(Options.TagTable);

            if (head != null)
            {
                Node node;
                for (node = head.Content; node != null; node = node.Next)
                {
                    if (node.Tag == Options.TagTable.TagMeta)
                    {
                        AttVal attval = node.GetAttrByName("name");

                        if (attval != null && attval.Val != null && String.CompareOrdinal(attval.Val, "generator") == 0)
                        {
                            attval = node.GetAttrByName("content");

                            if (attval != null && attval.Val != null && attval.Val.Length >= 9 &&
                                String.CompareOrdinal(attval.Val.Substring(0, 9), "HTML Tidy") == 0)
                            {
                                return false;
                            }
                        }
                    }
                }

                node = InferredTag("meta");
                node.AddAttribute("content", "HTML Tidy, see www.w3.org");
                node.AddAttribute("name", "generator");
                Node.InsertNodeAtStart(head, node);
                return true;
            }

            return false;
        }

        /* return true if substring s is in p and isn't all in upper case */
        /* this is used to check the case of SYSTEM, PUBLIC, DTD and EN */
        /* len is how many chars to check in p */

        private static bool FindBadSubString(string s, string p, int len)
        {
            int n = s.Length;
            int i = 0;

            while (n < len)
            {
                string ps = p.Substring(i, (i + n) - (i));
                if (String.CompareOrdinal(s, ps) == 0)
                {
                    return !ps.Equals(s.Substring(0, n));
                }

                ++i;
                --len;
            }

            return false;
        }

        public virtual bool CheckDocTypeKeyWords(Node doctype)
        {
            int len = doctype.End - doctype.Start;
            string s = GetString(Lexbuf, doctype.Start, len);

            return
                !(FindBadSubString("SYSTEM", s, len) || FindBadSubString("PUBLIC", s, len) ||
                  FindBadSubString("//DTD", s, len) || FindBadSubString("//W3C", s, len) ||
                  FindBadSubString("//EN", s, len));
        }

        /* examine <!DOCTYPE> to identify version */

        public virtual HtmlVersion FindGivenVersion(Node doctype)
        {
            int i;

            /* if root tag for doctype isn't html give up now */
            string str1 = GetString(Lexbuf, doctype.Start, 5);
            if (String.CompareOrdinal(str1, "html ") != 0)
            {
                return 0;
            }

            if (!CheckDocTypeKeyWords(doctype))
            {
                Report.Warning(this, doctype, null, Report.DTYPE_NOT_UPPER_CASE);
            }

            /* give up if all we are given is the system id for the doctype */
            str1 = GetString(Lexbuf, doctype.Start + 5, 7);
            if (String.CompareOrdinal(str1, "SYSTEM ") == 0)
            {
                /* but at least ensure the case is correct */
                if (!str1.Substring(0, (6) - (0)).Equals("SYSTEM"))
                {
                    Array.Copy(GetBytes("SYSTEM"), 0, Lexbuf, doctype.Start + 5, 6);
                }
                return 0; /* unrecognized */
            }

            if (String.CompareOrdinal(str1, "PUBLIC ") == 0)
            {
                if (!str1.Substring(0, (6) - (0)).Equals("PUBLIC"))
                    Array.Copy(GetBytes("PUBLIC "), 0, Lexbuf, doctype.Start + 5, 6);
            }
            else
            {
                BadDoctype = true;
            }

            for (i = doctype.Start; i < doctype.End; ++i)
            {
                if (Lexbuf[i] != (byte) '"') continue;
                str1 = GetString(Lexbuf, i + 1, 12);
                string str2 = GetString(Lexbuf, i + 1, 13);
                string p;
                string s;
                int j;
                int len;
                if (str1.Equals("-//W3C//DTD "))
                {
                    /* compute length of identifier e.g. "HTML 4.0 Transitional" */
                    //TODO:odd!
                    for (j = i + 13; j < doctype.End && Lexbuf[j] != (byte) '/'; ++j)
                    {
                    }

                    len = j - i - 13;
                    p = GetString(Lexbuf, i + 13, len);

                    for (j = 1; j < W3CVersion.Length; ++j)
                    {
                        s = W3CVersion[j].Name;
                        if (len == s.Length && s.Equals(p))
                        {
                            return W3CVersion[j].Version;
                        }
                    }

                    /* else unrecognized version */
                }
                else if (str2.Equals("-//IETF//DTD "))
                {
                    /* compute length of identifier e.g. "HTML 2.0" */
                    //TODO:odd!
                    for (j = i + 14; j < doctype.End && Lexbuf[j] != (byte) '/'; ++j)
                    {
                    }

                    len = j - i - 14;

                    p = GetString(Lexbuf, i + 14, len);
                    s = W3CVersion[0].Name;
                    if (len == s.Length && s.Equals(p))
                    {
                        return W3CVersion[0].Version;
                    }

                    /* else unrecognized version */
                }
                break;
            }

            return 0;
        }

        public virtual void FixHtmlNameSpace(Node root, string profile)
        {
            Node node;

            //TODO:odd!
            for (node = root.Content; node != null && node.Tag != Options.TagTable.TagHtml; node = node.Next)
            {
            }

            if (node != null)
            {
                AttVal attr;
                for (attr = node.Attributes; attr != null; attr = attr.Next)
                {
                    if (attr.Attribute.Equals("xmlns"))
                    {
                        break;
                    }
                }

                if (attr != null)
                {
                    if (!attr.Val.Equals(profile))
                    {
                        Report.Warning(this, node, null, Report.INCONSISTENT_NAMESPACE);
                        attr.Val = profile;
                    }
                }
                else
                {
                    attr = new AttVal(node.Attributes, null, '"', "xmlns", profile);
                    attr.Dict = AttributeTable.DefaultAttributeTable.FindAttribute(attr);
                    node.Attributes = attr;
                }
            }
        }

        public virtual bool SetXhtmlDocType(Node root)
        {
            string fpi = " ";
            string sysid = "";
            const string namespaceRenamed = XHTML_NAMESPACE;

            Node doctype = root.FindDocType();

            if (Options.DocType == DocType.Omit)
            {
                if (doctype != null)
                    Node.DiscardElement(doctype);
                return true;
            }

            if (Options.DocType == DocType.Auto)
            {
                /* see what flavor of XHTML this document matches */
                if ((Versions & HtmlVersion.Html40Strict) != 0)
                {
                    /* use XHTML strict */
                    fpi = "-//W3C//DTD XHTML 1.0 Strict//EN";
                    sysid = VOYAGER_STRICT;
                }
                else if ((Versions & HtmlVersion.Loose) != 0)
                {
                    fpi = "-//W3C//DTD XHTML 1.0 Transitional//EN";
                    sysid = VOYAGER_LOOSE;
                }
                else if ((Versions & HtmlVersion.Frames) != 0)
                {
                    /* use XHTML frames */
                    fpi = "-//W3C//DTD XHTML 1.0 Frameset//EN";
                    sysid = VOYAGER_FRAMESET;
                }
                else
                {
                    /* lets assume XHTML transitional */
                    fpi = "-//W3C//DTD XHTML 1.0 Transitional//EN";
                    sysid = VOYAGER_LOOSE;
                }
            }
            else if (Options.DocType == DocType.Strict)
            {
                fpi = "-//W3C//DTD XHTML 1.0 Strict//EN";
                sysid = VOYAGER_STRICT;
            }
            else if (Options.DocType == DocType.Loose)
            {
                fpi = "-//W3C//DTD XHTML 1.0 Transitional//EN";
                sysid = VOYAGER_LOOSE;
            }

            FixHtmlNameSpace(root, namespaceRenamed);

            if (doctype == null)
            {
                doctype = NewNode(Node.DOC_TYPE_TAG, Lexbuf, 0, 0);
                doctype.Next = root.Content;
                doctype.Parent = root;
                doctype.Prev = null;
                root.Content = doctype;
            }

            if (Options.DocType == DocType.User && Options.DocTypeStr != null)
            {
                fpi = Options.DocTypeStr;
                sysid = "";
            }

            Txtstart = Lexsize;
            Txtend = Lexsize;

            /* add public identifier */
            AddStringLiteral("html PUBLIC ");

            /* check if the fpi is quoted or not */
            if (fpi[0] == '"')
            {
                AddStringLiteral(fpi);
            }
            else
            {
                AddStringLiteral("\"");
                AddStringLiteral(fpi);
                AddStringLiteral("\"");
            }

            AddStringLiteral(sysid.Length + 6 >= Options.WrapLen ? "\n\"" : "\n    \"");

            /* add system identifier */
            AddStringLiteral(sysid);
            AddStringLiteral("\"");

            Txtend = Lexsize;

            doctype.Start = Txtstart;
            doctype.End = Txtend;

            return false;
        }

        public virtual HtmlVersion ApparentVersion()
        {
            switch (Doctype)
            {
                case HtmlVersion.Unknown:
                    return GetHtmlVersion();

                case HtmlVersion.Html20:
                    if ((Versions & HtmlVersion.Html20) != 0)
                    {
                        return HtmlVersion.Html20;
                    }
                    break;

                case HtmlVersion.Html32:
                    if ((Versions & HtmlVersion.Html32) != 0)
                    {
                        return HtmlVersion.Html32;
                    }
                    break; /* to replace old version by new */

                case HtmlVersion.Html40Strict:
                    if ((Versions & HtmlVersion.Html40Strict) != 0)
                    {
                        return HtmlVersion.Html40Strict;
                    }
                    break;

                case HtmlVersion.Html40Loose:
                    if ((Versions & HtmlVersion.Html40Loose) != 0)
                    {
                        return HtmlVersion.Html40Loose;
                    }
                    break; /* to replace old version by new */

                case HtmlVersion.Frames:
                    if ((Versions & HtmlVersion.Frames) != 0)
                    {
                        return HtmlVersion.Frames;
                    }
                    break;
            }

            Report.Warning(this, null, null, Report.INCONSISTENT_VERSION);
            return GetHtmlVersion();
        }

        /* fixup doctype if missing */

        public virtual bool FixDocType(Node root)
        {
            var guessed = HtmlVersion.Html40Strict;
            int i;

            if (BadDoctype)
            {
                Report.Warning(this, null, null, Report.MALFORMED_DOCTYPE);
            }

            if (Options.XmlOut)
            {
                return true;
            }

            Node doctype = root.FindDocType();

            if (Options.DocType == DocType.Omit)
            {
                if (doctype != null)
                {
                    Node.DiscardElement(doctype);
                }
                return true;
            }

            if (Options.DocType == DocType.Strict)
            {
                Node.DiscardElement(doctype);
                doctype = null;
                guessed = HtmlVersion.Html40Strict;
            }
            else if (Options.DocType == DocType.Loose)
            {
                Node.DiscardElement(doctype);
                doctype = null;
                guessed = HtmlVersion.Html40Loose;
            }
            else if (Options.DocType == DocType.Auto)
            {
                if (doctype != null)
                {
                    if (Doctype == HtmlVersion.Unknown)
                    {
                        return false;
                    }

                    switch (Doctype)
                    {
                        case HtmlVersion.Unknown:
                            return false;

                        case HtmlVersion.Html20:
                            if ((Versions & HtmlVersion.Html20) != 0)
                            {
                                return true;
                            }
                            break; /* to replace old version by new */


                        case HtmlVersion.Html32:
                            if ((Versions & HtmlVersion.Html32) != 0)
                            {
                                return true;
                            }
                            break; /* to replace old version by new */


                        case HtmlVersion.Html40Strict:
                            if ((Versions & HtmlVersion.Html40Strict) != 0)
                            {
                                return true;
                            }
                            break; /* to replace old version by new */


                        case HtmlVersion.Html40Loose:
                            if ((Versions & HtmlVersion.Html40Loose) != 0)
                            {
                                return true;
                            }
                            break; /* to replace old version by new */


                        case HtmlVersion.Frames:
                            if ((Versions & HtmlVersion.Frames) != 0)
                            {
                                return true;
                            }
                            break; /* to replace old version by new */
                    }

                    /* INCONSISTENT_VERSION warning is now issued by ApparentVersion() */
                }

                /* choose new doctype */
                guessed = GetHtmlVersion();
            }

            if (guessed == HtmlVersion.Unknown)
            {
                return false;
            }

            /* for XML use the Voyager system identifier */
            if (Options.XmlOut || Options.XmlTags || Isvoyager)
            {
                if (doctype != null)
                    Node.DiscardElement(doctype);

                for (i = 0; i < W3CVersion.Length; ++i)
                {
                    if (guessed == W3CVersion[i].Version)
                    {
                        FixHtmlNameSpace(root, W3CVersion[i].Profile);
                        break;
                    }
                }

                return true;
            }

            if (doctype == null)
            {
                doctype = NewNode(Node.DOC_TYPE_TAG, Lexbuf, 0, 0);
                doctype.Next = root.Content;
                doctype.Parent = root;
                doctype.Prev = null;
                root.Content = doctype;
            }

            Txtstart = Lexsize;
            Txtend = Lexsize;

            /* use the appropriate public identifier */
            AddStringLiteral("html PUBLIC ");

            if (Options.DocType == DocType.User && Options.DocTypeStr != null)
            {
                AddStringLiteral(Options.DocTypeStr);
            }
            else if (guessed == HtmlVersion.Html20)
            {
                AddStringLiteral("\"-//IETF//DTD HTML 2.0//EN\"");
            }
            else
            {
                AddStringLiteral("\"-//W3C//DTD ");

                for (i = 0; i < W3CVersion.Length; ++i)
                {
                    if (guessed == W3CVersion[i].Version)
                    {
                        AddStringLiteral(W3CVersion[i].Name);
                        break;
                    }
                }

                AddStringLiteral("//EN\"");
            }

            Txtend = Lexsize;

            doctype.Start = Txtstart;
            doctype.End = Txtend;

            return true;
        }

        /* ensure XML document starts with <?XML version="1.0"?> */

        public virtual bool FixXmlPi(Node root)
        {
            if (root.Content != null && root.Content.Type == Node.PROC_INS_TAG)
            {
                int s = root.Content.Start;

                if (Lexbuf[s] == (byte) 'x' && Lexbuf[s + 1] == (byte) 'm' && Lexbuf[s + 2] == (byte) 'l')
                {
                    return true;
                }
            }

            Node xml = NewNode(Node.PROC_INS_TAG, Lexbuf, 0, 0);
            xml.Next = root.Content;

            if (root.Content != null)
            {
                root.Content.Prev = xml;
                xml.Next = root.Content;
            }

            root.Content = xml;

            Txtstart = Lexsize;
            Txtend = Lexsize;
            AddStringLiteral("xml version=\"1.0\"");
            if (Options.CharEncoding == CharEncoding.Latin1)
            {
                AddStringLiteral(" encoding=\"ISO-8859-1\"");
            }
            Txtend = Lexsize;

            xml.Start = Txtstart;
            xml.End = Txtend;
            return false;
        }

        public virtual Node InferredTag(string name)
        {
            Node node = NewNode(Node.START_TAG, Lexbuf, Txtstart, Txtend, name);
            node.Isimplicit = true;
            return node;
        }

        public static bool ExpectsContent(Node node)
        {
            if (node.Type != Node.START_TAG)
            {
                return false;
            }

            /* unknown element? */
            if (node.Tag == null)
            {
                return true;
            }

            if ((node.Tag.Model & ContentModel.EMPTY) != 0)
            {
                return false;
            }

            return true;
        }

        /*
		create a text node for the contents of
		a CDATA element like style or script
		which ends with </foo> for some foo.
		*/

        public virtual Node GetCdata(Node container)
        {
            int c, lastc, start;
            bool endtag = false;

            Lines = Input.CursorLine;
            Columns = Input.CursorColumn;
            Waswhite = false;
            Txtstart = Lexsize;
            Txtend = Lexsize;

            lastc = '\x0000';
            start = - 1;

            while (true)
            {
                c = Input.ReadChar();
                if (c == StreamIn.END_OF_STREAM)
                {
                    break;
                }
                /* treat \r\n as \n and \r as \n */

                if (c == '/' && lastc == '<')
                {
                    if (endtag)
                    {
                        Lines = Input.CursorLine;
                        Columns = Input.CursorColumn - 3;

                        Report.Warning(this, null, null, Report.BAD_CDATA_CONTENT);
                    }

                    start = Lexsize + 1; /* to first letter */
                    endtag = true;
                }
                else if (c == '>' && start >= 0)
                {
                    int len = Lexsize - start;
                    if (len == container.Element.Length)
                    {
                        string str = GetString(Lexbuf, start, len);
                        if (String.CompareOrdinal(str, container.Element) == 0)
                        {
                            Txtend = start - 2;
                            break;
                        }
                    }

                    Lines = Input.CursorLine;
                    Columns = Input.CursorColumn - 3;

                    Report.Warning(this, null, null, Report.BAD_CDATA_CONTENT);

                    /* if javascript insert backslash before / */

                    if (ParserImpl.IsJavaScript(container))
                    {
                        int i;
                        for (i = Lexsize; i > start - 1; --i)
                        {
                            Lexbuf[i] = Lexbuf[i - 1];
                        }

                        Lexbuf[start - 1] = (byte) '\\';
                        Lexsize++;
                    }

                    start = - 1;
                }
                else if (c == '\r')
                {
                    c = Input.ReadChar();

                    if (c != '\n')
                    {
                        Input.UngetChar(c);
                    }

                    c = '\n';
                }

                AddCharToLexer(c);
                Txtend = Lexsize;
                lastc = c;
            }

            if (c == StreamIn.END_OF_STREAM)
            {
                Report.Warning(this, container, null, Report.MISSING_ENDTAG_FOR);
            }

            if (Txtend > Txtstart)
            {
                Token = NewNode(Node.TEXT_NODE, Lexbuf, Txtstart, Txtend);
                return Token;
            }

            return null;
        }

        public virtual void UngetToken()
        {
            Pushed = true;
        }

        /*
		modes for GetToken()
		
		MixedContent   -- for elements which don't accept PCDATA
		Preformatted       -- white space preserved as is
		IgnoreMarkup       -- for CDATA elements such as script, style
		*/

        public virtual Node GetToken(short mode)
        {
            int c;
            int badcomment = 0;
            var isempty = new MutableBoolean();

            if (Pushed)
            {
                /* duplicate inlines in preference to pushed text nodes when appropriate */
                if (Token.Type != Node.TEXT_NODE || (Insert == - 1 && Inode == null))
                {
                    Pushed = false;
                    return Token;
                }
            }

            /* at start of block elements, unclosed inline
			elements are inserted into the token stream */

            if (Insert != - 1 || Inode != null)
            {
                return InsertedToken();
            }

            Lines = Input.CursorLine;
            Columns = Input.CursorColumn;
            Waswhite = false;

            Txtstart = Lexsize;
            Txtend = Lexsize;

            while (true)
            {
                c = Input.ReadChar();
                if (c == StreamIn.END_OF_STREAM)
                {
                    break;
                }

                if (Insertspace && mode != IGNORE_WHITESPACE)
                {
                    AddCharToLexer(' ');
                    Waswhite = true;
                    Insertspace = false;
                }

                /* treat \r\n as \n and \r as \n */

                if (c == '\r')
                {
                    c = Input.ReadChar();

                    if (c != '\n')
                    {
                        Input.UngetChar(c);
                    }

                    c = '\n';
                }

                AddCharToLexer(c);

                short map;
                switch (State)
                {
                    case LEX_CONTENT:
                        map = Map((char) c);

                        /*
						Discard white space if appropriate. Its cheaper
						to do this here rather than in parser methods
						for elements that don't have mixed content.
						*/
                        if (((map & WHITE) != 0) && (mode == IGNORE_WHITESPACE) && Lexsize == Txtstart + 1)
                        {
                            --Lexsize;
                            Waswhite = false;
                            Lines = Input.CursorLine;
                            Columns = Input.CursorColumn;
                            continue;
                        }

                        if (c == '<')
                        {
                            State = LEX_GT;
                            continue;
                        }

                        if ((map & WHITE) != 0)
                        {
                            /* was previous char white? */
                            if (Waswhite)
                            {
                                if (mode != PREFORMATTED && mode != IGNORE_MARKUP)
                                {
                                    --Lexsize;
                                    Lines = Input.CursorLine;
                                    Columns = Input.CursorColumn;
                                }
                            }
                                /* prev char wasn't white */
                            else
                            {
                                Waswhite = true;

                                if (mode != PREFORMATTED && mode != IGNORE_MARKUP && c != ' ')
                                {
                                    ChangeChar((byte) ' ');
                                }
                            }

                            continue;
                        }
                        if (c == '&' && mode != IGNORE_MARKUP)
                        {
                            ParseEntity(mode);
                        }

                        /* this is needed to avoid trimming trailing whitespace */
                        if (mode == IGNORE_WHITESPACE)
                            mode = MIXED_CONTENT;

                        Waswhite = false;
                        continue;


                    case LEX_GT:
                        if (c == '/')
                        {
                            c = Input.ReadChar();
                            if (c == StreamIn.END_OF_STREAM)
                            {
                                Input.UngetChar(c);
                                continue;
                            }

                            AddCharToLexer(c);
                            map = Map((char) c);

                            if ((map & LETTER) != 0)
                            {
                                Lexsize -= 3;
                                Txtend = Lexsize;
                                Input.UngetChar(c);
                                State = LEX_ENDTAG;
                                Lexbuf[Lexsize] = (byte) '\x0000'; /* debug */
                                Input.CursorColumn -= 2;

                                /* if some text before the </ return it now */
                                if (Txtend > Txtstart)
                                {
                                    /* trim space char before end tag */
                                    if (mode == IGNORE_WHITESPACE && Lexbuf[Lexsize - 1] == (byte) ' ')
                                    {
                                        Lexsize -= 1;
                                        Txtend = Lexsize;
                                    }

                                    Token = NewNode(Node.TEXT_NODE, Lexbuf, Txtstart, Txtend);
                                    return Token;
                                }

                                continue; /* no text so keep going */
                            }

                            /* otherwise treat as CDATA */
                            Waswhite = false;
                            State = LEX_CONTENT;
                            continue;
                        }

                        if (mode == IGNORE_MARKUP)
                        {
                            /* otherwise treat as CDATA */
                            Waswhite = false;
                            State = LEX_CONTENT;
                            continue;
                        }

                        /*
						look out for comments, doctype or marked sections
						this isn't quite right, but its getting there ...
						*/
                        if (c == '!')
                        {
                            c = Input.ReadChar();
                            if (c == '-')
                            {
                                c = Input.ReadChar();
                                if (c == '-')
                                {
                                    State = LEX_COMMENT; /* comment */
                                    Lexsize -= 2;
                                    Txtend = Lexsize;

                                    /* if some text before < return it now */
                                    if (Txtend > Txtstart)
                                    {
                                        Token = NewNode(Node.TEXT_NODE, Lexbuf, Txtstart, Txtend);
                                        return Token;
                                    }

                                    Txtstart = Lexsize;
                                    continue;
                                }

                                Report.Warning(this, null, null, Report.MALFORMED_COMMENT);
                            }
                            else if (c == 'd' || c == 'D')
                            {
                                State = LEX_DOCTYPE; /* doctype */
                                Lexsize -= 2;
                                Txtend = Lexsize;
                                mode = IGNORE_WHITESPACE;

                                /* skip until white space or '>' */

                                for (;;)
                                {
                                    c = Input.ReadChar();

                                    if (c == StreamIn.END_OF_STREAM || c == '>')
                                    {
                                        Input.UngetChar(c);
                                        break;
                                    }

                                    map = Map((char) c);
                                    if ((map & WHITE) == 0)
                                    {
                                        continue;
                                    }

                                    /* and skip to end of whitespace */

                                    for (;;)
                                    {
                                        c = Input.ReadChar();

                                        if (c == StreamIn.END_OF_STREAM || c == '>')
                                        {
                                            Input.UngetChar(c);
                                            break;
                                        }

                                        map = Map((char) c);

                                        if ((map & WHITE) != 0)
                                        {
                                            continue;
                                        }

                                        Input.UngetChar(c);
                                        break;
                                    }

                                    break;
                                }

                                /* if some text before < return it now */
                                if (Txtend > Txtstart)
                                {
                                    Token = NewNode(Node.TEXT_NODE, Lexbuf, Txtstart, Txtend);
                                    return Token;
                                }

                                Txtstart = Lexsize;
                                continue;
                            }
                            else if (c == '[')
                            {
                                /* Word 2000 embeds <![if ...]> ... <![endif]> sequences */
                                Lexsize -= 2;
                                State = LEX_SECTION;
                                Txtend = Lexsize;

                                /* if some text before < return it now */
                                if (Txtend > Txtstart)
                                {
                                    Token = NewNode(Node.TEXT_NODE, Lexbuf, Txtstart, Txtend);
                                    return Token;
                                }

                                Txtstart = Lexsize;
                                continue;
                            }

                            /* otherwise swallow chars up to and including next '>' */
                            while (true)
                            {
                                c = Input.ReadChar();
                                if (c == '>')
                                {
                                    break;
                                }
                                if (c == - 1)
                                {
                                    Input.UngetChar(c);
                                    break;
                                }
                            }

                            Lexsize -= 2;
                            Lexbuf[Lexsize] = (byte) '\x0000';
                            State = LEX_CONTENT;
                            continue;
                        }

                        /*
						processing instructions
						*/

                        if (c == '?')
                        {
                            Lexsize -= 2;
                            State = LEX_PROCINSTR;
                            Txtend = Lexsize;

                            /* if some text before < return it now */
                            if (Txtend > Txtstart)
                            {
                                Token = NewNode(Node.TEXT_NODE, Lexbuf, Txtstart, Txtend);
                                return Token;
                            }

                            Txtstart = Lexsize;
                            continue;
                        }

                        /* Microsoft ASP's e.g. <% ... server-code ... %> */
                        if (c == '%')
                        {
                            Lexsize -= 2;
                            State = LEX_ASP;
                            Txtend = Lexsize;

                            /* if some text before < return it now */
                            if (Txtend > Txtstart)
                            {
                                Token = NewNode(Node.TEXT_NODE, Lexbuf, Txtstart, Txtend);
                                return Token;
                            }

                            Txtstart = Lexsize;
                            continue;
                        }

                        /* Netscapes JSTE e.g. <# ... server-code ... #> */
                        if (c == '#')
                        {
                            Lexsize -= 2;
                            State = LEX_JSTE;
                            Txtend = Lexsize;

                            /* if some text before < return it now */
                            if (Txtend > Txtstart)
                            {
                                Token = NewNode(Node.TEXT_NODE, Lexbuf, Txtstart, Txtend);
                                return Token;
                            }

                            Txtstart = Lexsize;
                            continue;
                        }

                        map = Map((char) c);

                        /* check for start tag */
                        if ((map & LETTER) != 0)
                        {
                            Input.UngetChar(c); /* push back letter */
                            Lexsize -= 2; /* discard "<" + letter */
                            Txtend = Lexsize;
                            State = LEX_STARTTAG; /* ready to read tag name */

                            /* if some text before < return it now */
                            if (Txtend > Txtstart)
                            {
                                Token = NewNode(Node.TEXT_NODE, Lexbuf, Txtstart, Txtend);
                                return Token;
                            }

                            continue; /* no text so keep going */
                        }

                        /* otherwise treat as CDATA */
                        State = LEX_CONTENT;
                        Waswhite = false;
                        continue;


                    case LEX_ENDTAG:
                        Txtstart = Lexsize - 1;
                        Input.CursorColumn += 2;
                        c = ParseTagName();
                        Token = NewNode(Node.END_TAG, Lexbuf, Txtstart, Txtend,
                                        GetString(Lexbuf, Txtstart, Txtend - Txtstart));
                        Lexsize = Txtstart;
                        Txtend = Txtstart;

                        /* skip to '>' */
                        while (c != '>')
                        {
                            c = Input.ReadChar();
                            if (c == StreamIn.END_OF_STREAM)
                            {
                                break;
                            }
                        }

                        if (c == StreamIn.END_OF_STREAM)
                        {
                            Input.UngetChar(c);
                            continue;
                        }

                        State = LEX_CONTENT;
                        Waswhite = false;
                        return Token; /* the endtag token */


                    case LEX_STARTTAG:
                        Txtstart = Lexsize - 1; /* set txtstart to first letter */
                        c = ParseTagName();
                        isempty.Val = false;
                        AttVal attributes = null;
                        Token = NewNode((isempty.Val ? Node.START_END_TAG : Node.START_TAG), Lexbuf, Txtstart, Txtend,
                                        GetString(Lexbuf, Txtstart, Txtend - Txtstart));

                        /* parse attributes, consuming closing ">" */
                        if (c != '>')
                        {
                            if (c == '/')
                            {
                                Input.UngetChar(c);
                            }

                            attributes = ParseAttrs(isempty);
                        }

                        if (isempty.Val)
                        {
                            Token.Type = Node.START_END_TAG;
                        }

                        Token.Attributes = attributes;
                        Lexsize = Txtstart;
                        Txtend = Txtstart;

                        /* swallow newline following start tag */
                        /* special check needed for CRLF sequence */
                        /* this doesn't apply to empty elements */

                        if (ExpectsContent(Token) || Token.Tag == Options.TagTable.TagBr)
                        {
                            c = Input.ReadChar();
                            if (c == '\r')
                            {
                                c = Input.ReadChar();

                                if (c != '\n')
                                {
                                    Input.UngetChar(c);
                                }
                            }
                            else if (c != '\n' && c != '\f')
                            {
                                Input.UngetChar(c);
                            }

                            Waswhite = true; /* to swallow leading whitespace */
                        }
                        else
                        {
                            Waswhite = false;
                        }

                        State = LEX_CONTENT;

                        if (Token.Tag == null)
                        {
                            Report.Error(this, null, Token, Report.UNKNOWN_ELEMENT);
                        }
                        else if (!Options.XmlTags)
                        {
                            Versions &= Token.Tag.Versions;

                            if ((Token.Tag.Versions & HtmlVersion.Proprietary) != 0)
                            {
                                if (!Options.MakeClean &&
                                    (Token.Tag == Options.TagTable.TagNobr || Token.Tag == Options.TagTable.TagWbr))
                                {
                                    Report.Warning(this, null, Token, Report.PROPRIETARY_ELEMENT);
                                }
                            }

                            if (Token.Tag.CheckAttribs != null)
                            {
                                Token.CheckUniqueAttributes(this);
                                Token.Tag.CheckAttribs.Check(this, Token);
                            }
                            else
                            {
                                Token.CheckAttributes(this);
                            }
                        }
                        return Token; /* return start tag */

                    case LEX_COMMENT:
                        if (c != '-')
                        {
                            continue;
                        }

                        c = Input.ReadChar();
                        AddCharToLexer(c);
                        if (c != '-')
                        {
                            continue;
                        }

                        while (true)
                        {
                            c = Input.ReadChar();

                            if (c == '>')
                            {
                                if (badcomment != 0)
                                {
                                    Report.Warning(this, null, null, Report.MALFORMED_COMMENT);
                                }

                                Txtend = Lexsize - 2; // AQ 8Jul2000
                                Lexbuf[Lexsize] = (byte) '\x0000';
                                State = LEX_CONTENT;
                                Waswhite = false;
                                Token = NewNode(Node.COMMENT_TAG, Lexbuf, Txtstart, Txtend);

                                /* now look for a line break */

                                c = Input.ReadChar();

                                if (c == '\r')
                                {
                                    c = Input.ReadChar();

                                    if (c != '\n')
                                    {
                                        Token.Linebreak = true;
                                    }
                                }

                                if (c == '\n')
                                {
                                    Token.Linebreak = true;
                                }
                                else
                                {
                                    Input.UngetChar(c);
                                }

                                return Token;
                            }

                            /* note position of first such error in the comment */
                            if (badcomment == 0)
                            {
                                Lines = Input.CursorLine;
                                Columns = Input.CursorColumn - 3;
                            }

                            badcomment++;
                            if (Options.FixComments)
                            {
                                Lexbuf[Lexsize - 2] = (byte) '=';
                            }

                            AddCharToLexer(c);

                            /* if '-' then look for '>' to end the comment */
                            if (c != '-')
                            {
                                break;
                            }
                        }

                        /* otherwise continue to look for --> */
                        Lexbuf[Lexsize - 2] = (byte) '=';
                        continue;


                    case LEX_DOCTYPE:
                        map = Map((char) c);

                        if ((map & WHITE) != 0)
                        {
                            if (Waswhite)
                            {
                                Lexsize -= 1;
                            }

                            Waswhite = true;
                        }
                        else
                        {
                            Waswhite = false;
                        }

                        if (c != '>')
                        {
                            continue;
                        }

                        Lexsize -= 1;
                        Txtend = Lexsize;
                        Lexbuf[Lexsize] = (byte) '\x0000';
                        State = LEX_CONTENT;
                        Waswhite = false;
                        Token = NewNode(Node.DOC_TYPE_TAG, Lexbuf, Txtstart, Txtend);
                        /* make a note of the version named by the doctype */
                        Doctype = FindGivenVersion(Token);
                        return Token;


                    case LEX_PROCINSTR:

                        if (Lexsize - Txtstart == 3)
                        {
                            if ((GetString(Lexbuf, Txtstart, 3)).Equals("php"))
                            {
                                State = LEX_PHP;
                                continue;
                            }
                        }

                        if (Options.XmlPIs)
                        {
                            /* insist on ?> as terminator */
                            if (c != '?')
                            {
                                continue;
                            }

                            /* now look for '>' */
                            c = Input.ReadChar();

                            if (c == StreamIn.END_OF_STREAM)
                            {
                                Report.Warning(this, null, null, Report.UNEXPECTED_END_OF_FILE);
                                Input.UngetChar(c);
                                continue;
                            }

                            AddCharToLexer(c);
                        }

                        if (c != '>')
                        {
                            continue;
                        }

                        Lexsize -= 1;
                        Txtend = Lexsize;
                        Lexbuf[Lexsize] = (byte) '\x0000';
                        State = LEX_CONTENT;
                        Waswhite = false;
                        Token = NewNode(Node.PROC_INS_TAG, Lexbuf, Txtstart, Txtend);
                        return Token;


                    case LEX_ASP:
                        if (c != '%')
                        {
                            continue;
                        }

                        /* now look for '>' */
                        c = Input.ReadChar();

                        if (c != '>')
                        {
                            Input.UngetChar(c);
                            continue;
                        }

                        Lexsize -= 1;
                        Txtend = Lexsize;
                        Lexbuf[Lexsize] = (byte) '\x0000';
                        State = LEX_CONTENT;
                        Waswhite = false;
                        Token = NewNode(Node.ASP_TAG, Lexbuf, Txtstart, Txtend);
                        return Token;

                    case LEX_JSTE:
                        if (c != '#')
                        {
                            continue;
                        }

                        /* now look for '>' */
                        c = Input.ReadChar();
                        if (c != '>')
                        {
                            Input.UngetChar(c);
                            continue;
                        }

                        Lexsize -= 1;
                        Txtend = Lexsize;
                        Lexbuf[Lexsize] = (byte) '\x0000';
                        State = LEX_CONTENT;
                        Waswhite = false;
                        Token = NewNode(Node.JSTE_TAG, Lexbuf, Txtstart, Txtend);
                        return Token;

                    case LEX_PHP:
                        if (c != '?')
                        {
                            continue;
                        }

                        /* now look for '>' */
                        c = Input.ReadChar();
                        if (c != '>')
                        {
                            Input.UngetChar(c);
                            continue;
                        }

                        Lexsize -= 1;
                        Txtend = Lexsize;
                        Lexbuf[Lexsize] = (byte) '\x0000';
                        State = LEX_CONTENT;
                        Waswhite = false;
                        Token = NewNode(Node.PHP_TAG, Lexbuf, Txtstart, Txtend);
                        return Token;


                    case LEX_SECTION:
                        if (c == '[')
                        {
                            if (Lexsize == (Txtstart + 6) && (GetString(Lexbuf, Txtstart, 6)).Equals("CDATA["))
                            {
                                State = LEX_CDATA;
                                Lexsize -= 6;
                                continue;
                            }
                        }

                        if (c != ']')
                        {
                            continue;
                        }

                        /* now look for '>' */
                        c = Input.ReadChar();
                        if (c != '>')
                        {
                            Input.UngetChar(c);
                            continue;
                        }

                        Lexsize -= 1;
                        Txtend = Lexsize;
                        Lexbuf[Lexsize] = (byte) '\x0000';
                        State = LEX_CONTENT;
                        Waswhite = false;
                        Token = NewNode(Node.SECTION_TAG, Lexbuf, Txtstart, Txtend);
                        return Token;

                    case LEX_CDATA:
                        if (c != ']')
                        {
                            continue;
                        }

                        /* now look for ']' */
                        c = Input.ReadChar();
                        if (c != ']')
                        {
                            Input.UngetChar(c);
                            continue;
                        }

                        /* now look for '>' */
                        c = Input.ReadChar();
                        if (c != '>')
                        {
                            Input.UngetChar(c);
                            continue;
                        }

                        Lexsize -= 1;
                        Txtend = Lexsize;
                        Lexbuf[Lexsize] = (byte) '\x0000';
                        State = LEX_CONTENT;
                        Waswhite = false;
                        Token = NewNode(Node.CDATA_TAG, Lexbuf, Txtstart, Txtend);
                        return Token;
                }
            }

            if (State == LEX_CONTENT)
            {
                /* text string */
                Txtend = Lexsize;
                if (Txtend > Txtstart)
                {
                    Input.UngetChar(c);
                    if (Lexbuf[Lexsize - 1] == (byte) ' ')
                    {
                        Lexsize -= 1;
                        Txtend = Lexsize;
                    }

                    Token = NewNode(Node.TEXT_NODE, Lexbuf, Txtstart, Txtend);
                    return Token;
                }
            }
            else if (State == LEX_COMMENT)
            {
                /* comment */
                if (c == StreamIn.END_OF_STREAM)
                {
                    Report.Warning(this, null, null, Report.MALFORMED_COMMENT);
                }

                Txtend = Lexsize;
                Lexbuf[Lexsize] = (byte) '\x0000';
                State = LEX_CONTENT;
                Waswhite = false;
                Token = NewNode(Node.COMMENT_TAG, Lexbuf, Txtstart, Txtend);
                return Token;
            }

            return null;
        }

        /*
		parser for ASP within start tags
		
		Some people use ASP for to customize attributes
		Tidy isn't really well suited to dealing with ASP
		This is a workaround for attributes, but won't
		deal with the case where the ASP is used to tailor
		the attribute value. Here is an example of a work
		around for using ASP in attribute values:
		
		href="<%=rsSchool.Fields("ID").Value%>"
		
		where the ASP that generates the attribute value
		is masked from Tidy by the quotemarks.
		
		*/

        public virtual Node ParseAsp()
        {
            Node asp = null;

            Txtstart = Lexsize;
            for (;;)
            {
                int c = Input.ReadChar();
                AddCharToLexer(c);

                if (c != '%')
                {
                    continue;
                }

                c = Input.ReadChar();
                AddCharToLexer(c);

                if (c == '>')
                {
                    break;
                }
            }

            Lexsize -= 2;
            Txtend = Lexsize;

            if (Txtend > Txtstart)
            {
                asp = NewNode(Node.ASP_TAG, Lexbuf, Txtstart, Txtend);
            }

            Txtstart = Txtend;
            return asp;
        }

        /*
		PHP is like ASP but is based upon XML
		processing instructions, e.g. <?php ... ?>
		*/

        public virtual Node ParsePhp()
        {
            Node php = null;

            Txtstart = Lexsize;

            for (;;)
            {
                int c = Input.ReadChar();
                AddCharToLexer(c);

                if (c != '?')
                {
                    continue;
                }

                c = Input.ReadChar();
                AddCharToLexer(c);
                if (c == '>')
                {
                    break;
                }
            }

            Lexsize -= 2;
            Txtend = Lexsize;

            if (Txtend > Txtstart)
            {
                php = NewNode(Node.PHP_TAG, Lexbuf, Txtstart, Txtend);
            }

            Txtstart = Txtend;
            return php;
        }

        /* consumes the '>' terminating start tags */

        public virtual string ParseAttribute(MutableBoolean isempty, MutableObject asp, MutableObject php)
        {
            int start;
            // int len = 0;   Removed by BUGFIX for 126265
            short map;
            int c;

            asp.Object = null; /* clear asp pointer */
            php.Object = null; /* clear php pointer */
            /* skip white space before the attribute */

            for (;;)
            {
                c = Input.ReadChar();
                if (c == '/')
                {
                    c = Input.ReadChar();
                    if (c == '>')
                    {
                        isempty.Val = true;
                        return null;
                    }

                    Input.UngetChar(c);
                    c = '/';
                    break;
                }

                if (c == '>')
                {
                    return null;
                }

                if (c == '<')
                {
                    c = Input.ReadChar();

                    if (c == '%')
                    {
                        asp.Object = ParseAsp();
                        return null;
                    }
                    if (c == '?')
                    {
                        php.Object = ParsePhp();
                        return null;
                    }

                    Input.UngetChar(c);
                    Report.AttrError(this, Token, null, Report.UNEXPECTED_GT);
                    return null;
                }

                if (c == '"' || c == '\'')
                {
                    Report.AttrError(this, Token, null, Report.UNEXPECTED_QUOTEMARK);
                    continue;
                }

                if (c == StreamIn.END_OF_STREAM)
                {
                    Report.AttrError(this, Token, null, Report.UNEXPECTED_END_OF_FILE);
                    Input.UngetChar(c);
                    return null;
                }

                map = Map((char) c);

                if ((map & WHITE) == 0)
                {
                    break;
                }
            }

            start = Lexsize;

            for (;;)
            {
                /* but push back '=' for parseValue() */
                if (c == '=' || c == '>')
                {
                    Input.UngetChar(c);
                    break;
                }

                if (c == '<' || c == StreamIn.END_OF_STREAM)
                {
                    Input.UngetChar(c);
                    break;
                }

                map = Map((char) c);

                if ((map & WHITE) != 0)
                    break;

                /* what should be done about non-namechar characters? */
                /* currently these are incorporated into the attr name */

                if (!Options.XmlTags && (map & UPPERCASE) != 0)
                {
                    c += ('a' - 'A');
                }

                //  ++len;    Removed by BUGFIX for 126265 
                AddCharToLexer(c);

                c = Input.ReadChar();
            }

            // Following line added by GLP to fix BUG 126265.  This is a temporary comment
            // and should be removed when Tidy is fixed.
            int len = Lexsize - start;
            string attr = (len > 0 ? GetString(Lexbuf, start, len) : null);
            Lexsize = start;

            return attr;
        }

        /*
		invoked when < is seen in place of attribute value
		but terminates on whitespace if not ASP, PHP or Tango
		this routine recognizes ' and " quoted strings
		*/

        public virtual int ParseServerInstruction()
        {
            int c;
            int delim = '"';
            bool isrule = false;

            c = Input.ReadChar();
            AddCharToLexer(c);

            /* check for ASP, PHP or Tango */
            if (c == '%' || c == '?' || c == '@')
            {
                isrule = true;
            }

            for (;;)
            {
                c = Input.ReadChar();

                if (c == StreamIn.END_OF_STREAM)
                {
                    break;
                }

                if (c == '>')
                {
                    if (isrule)
                    {
                        AddCharToLexer(c);
                    }
                    else
                    {
                        Input.UngetChar(c);
                    }
                    break;
                }

                /* if not recognized as ASP, PHP or Tango */
                /* then also finish value on whitespace */
                if (!isrule)
                {
                    int map = Map((char) c);

                    if ((map & WHITE) != 0)
                    {
                        break;
                    }
                }

                AddCharToLexer(c);

                if (c == '"')
                {
                    do
                    {
                        c = Input.ReadChar();
                        AddCharToLexer(c);
                    } while (c != '"');
                    delim = '\'';
                    continue;
                }

                if (c == '\'')
                {
                    do
                    {
                        c = Input.ReadChar();
                        AddCharToLexer(c);
                    } while (c != '\'');
                }
            }

            return delim;
        }

        /* values start with "=" or " = " etc. */
        /* doesn't consume the ">" at end of start tag */

        public virtual string ParseValue(string name, bool foldCase, MutableBoolean isempty, MutableInteger pdelim)
        {
            int len;
            int start;
            short map;
            bool seenGt = false;
            bool munge = true;
            int c;
            int delim, quotewarning;
            string val;

            delim = 0;
            pdelim.Val = '"';

            /*
			Henry Zrepa reports that some folk are using the
			embed element with script attributes where newlines
			are significant and must be preserved
			*/
            if (Options.LiteralAttribs)
                munge = false;

            /* skip white space before the '=' */

            for (;;)
            {
                c = Input.ReadChar();

                if (c == StreamIn.END_OF_STREAM)
                {
                    Input.UngetChar(c);
                    break;
                }

                map = Map((char) c);

                if ((map & WHITE) == 0)
                {
                    break;
                }
            }

            /*
			c should be '=' if there is a value
			other legal possibilities are white
			space, '/' and '>'
			*/

            if (c != '=')
            {
                Input.UngetChar(c);
                return null;
            }

            /* skip white space after '=' */

            for (;;)
            {
                c = Input.ReadChar();
                if (c == StreamIn.END_OF_STREAM)
                {
                    Input.UngetChar(c);
                    break;
                }

                map = Map((char) c);

                if ((map & WHITE) == 0)
                    break;
            }

            /* check for quote marks */

            if (c == '"' || c == '\'')
                delim = c;
            else if (c == '<')
            {
                start = Lexsize;
                AddCharToLexer(c);
                pdelim.Val = ParseServerInstruction();
                len = Lexsize - start;
                Lexsize = start;
                return (len > 0 ? GetString(Lexbuf, start, len) : null);
            }
            else
            {
                Input.UngetChar(c);
            }

            /*
			and read the value string
			check for quote mark if needed
			*/

            quotewarning = 0;
            start = Lexsize;
            c = '\x0000';

            for (;;)
            {
                int lastc = c;
                c = Input.ReadChar();

                if (c == StreamIn.END_OF_STREAM)
                {
                    Report.AttrError(this, Token, null, Report.UNEXPECTED_END_OF_FILE);
                    Input.UngetChar(c);
                    break;
                }

                if (delim == (char) 0)
                {
                    if (c == '>')
                    {
                        Input.UngetChar(c);
                        break;
                    }

                    if (c == '"' || c == '\'')
                    {
                        Report.AttrError(this, Token, null, Report.UNEXPECTED_QUOTEMARK);
                        break;
                    }

                    if (c == '<')
                    {
                        /* in.UngetChar(c); */
                        Report.AttrError(this, Token, null, Report.UNEXPECTED_GT);
                        /* break; */
                    }

                    /*
					For cases like <br clear=all/> need to avoid treating /> as
					part of the attribute value, however care is needed to avoid
					so treating <a href=http://www.acme.com/> in this way, which
					would map the <a> tag to <a href="http://www.acme.com"/>
					*/
                    if (c == '/')
                    {
                        /* peek ahead in case of /> */
                        c = Input.ReadChar();
                        if (c == '>' && !AttributeTable.DefaultAttributeTable.IsUrl(name))
                        {
                            isempty.Val = true;
                            Input.UngetChar(c);
                            break;
                        }

                        /* unget peeked char */
                        Input.UngetChar(c);
                        c = '/';
                    }
                }
                    /* delim is '\'' or '"' */
                else
                {
                    if (c == delim)
                    {
                        break;
                    }

                    /* treat CRLF, CR and LF as single line break */

                    if (c == '\r')
                    {
                        c = Input.ReadChar();
                        if (c != '\n')
                        {
                            Input.UngetChar(c);
                        }

                        c = '\n';
                    }

                    if (c == '\n' || c == '<' || c == '>')
                        ++quotewarning;

                    if (c == '>')
                        seenGt = true;
                }

                if (c == '&')
                {
                    AddCharToLexer(c);
                    ParseEntity(0);
                    continue;
                }

                /*
				kludge for JavaScript attribute values
				with line continuations in string literals
				*/
                if (c == '\\')
                {
                    c = Input.ReadChar();

                    if (c != '\n')
                    {
                        Input.UngetChar(c);
                        c = '\\';
                    }
                }

                map = Map((char) c);

                if ((map & WHITE) != 0)
                {
                    if (delim == (char) 0)
                        break;

                    if (munge)
                    {
                        c = ' ';

                        if (lastc == ' ')
                            continue;
                    }
                }
                else if (foldCase && (map & UPPERCASE) != 0)
                    c += ('a' - 'A');

                AddCharToLexer(c);
            }

            if (quotewarning > 10 && seenGt && munge)
            {
                /*
				there is almost certainly a missing trailling quote mark
				as we have see too many newlines, < or > characters.
				
				an exception is made for Javascript attributes and the
				javascript URL scheme which may legitimately include < and >
				*/
                if (!AttributeTable.DefaultAttributeTable.IsScript(name) &&
                    !(AttributeTable.DefaultAttributeTable.IsUrl(name) &&
                      (GetString(Lexbuf, start, 11)).Equals("javascript:")))
                    Report.Error(this, null, null, Report.SUSPECTED_MISSING_QUOTE);
            }

            len = Lexsize - start;
            Lexsize = start;

            if (len > 0 || delim != 0)
            {
                val = GetString(Lexbuf, start, len);
            }
            else
            {
                val = null;
            }

            /* note delimiter if given */
            pdelim.Val = delim != 0 ? delim : '"';

            return val;
        }

        /* attr must be non-null */

        public static bool IsValidAttrName(string attr)
        {
            short map;
            char c;
            int i;

            /* first character should be a letter */
            c = attr[0];
            map = Map(c);

            if ((map & LETTER) == 0)
                return false;

            /* remaining characters should be namechars */
            for (i = 1; i < attr.Length; i++)
            {
                c = attr[i];
                map = Map(c);

                if ((map & NAMECHAR) != 0)
                    continue;

                return false;
            }

            return true;
        }

        /* swallows closing '>' */

        public virtual AttVal ParseAttrs(MutableBoolean isempty)
        {
            var delim = new MutableInteger();
            var asp = new MutableObject();
            var php = new MutableObject();

            AttVal list = null;

            while (!EndOfInput())
            {
                string attribute = ParseAttribute(isempty, asp, php);

                AttVal av;
                if (attribute == null)
                {
                    /* check if attributes are created by ASP markup */
                    if (asp.Object != null)
                    {
                        av = new AttVal(list, null, (Node) asp.Object, null, '\x0000', null, null);
                        list = av;
                        continue;
                    }

                    /* check if attributes are created by PHP markup */
                    if (php.Object != null)
                    {
                        av = new AttVal(list, null, null, (Node) php.Object, '\x0000', null, null);
                        list = av;
                        continue;
                    }

                    break;
                }

                string val = ParseValue(attribute, false, isempty, delim);

                if (IsValidAttrName(attribute))
                {
                    av = new AttVal(list, null, null, null, delim.Val, attribute, val);
                    av.Dict = AttributeTable.DefaultAttributeTable.FindAttribute(av);
                    list = av;
                }
                else
                {
                    //av = new AttVal(null, null, null, null, 0, attribute, val);
                    Report.AttrError(this, Token, val, Report.BAD_ATTRIBUTE_VALUE);
                }
            }

            return list;
        }

        /*
		push a copy of an inline node onto stack
		but don't push if implicit or OBJECT or APPLET
		(implicit tags are ones generated from the istack)
		
		One issue arises with pushing inlines when
		the tag is already pushed. For instance:
		
		<p><em>text
		<p><em>more text
		
		Shouldn't be mapped to
		
		<p><em>text</em></p>
		<p><em><em>more text</em></em>
		*/

        public virtual void PushInline(Node node)
        {
            if (node.Isimplicit)
                return;

            if (node.Tag == null)
                return;

            if ((node.Tag.Model & ContentModel.INLINE) == 0)
                return;

            if ((node.Tag.Model & ContentModel.OBJECT) != 0)
                return;

            if (node.Tag != Options.TagTable.TagFont && IsPushed(node))
                return;

            // make sure there is enough space for the stack
            var stack = new InlineStack {Tag = node.Tag, Element = node.Element};
            if (node.Attributes != null)
            {
                stack.Attributes = CloneAttributes(node.Attributes);
            }

            Istack.Push(stack);
        }

        /* pop inline stack */

        public virtual void PopInline(Node node)
        {
            if (node != null)
            {
                if (node.Tag == null)
                    return;

                if ((node.Tag.Model & ContentModel.INLINE) == 0)
                    return;

                if ((node.Tag.Model & ContentModel.OBJECT) != 0)
                    return;

                // if node is </a> then pop until we find an <a>
                if (node.Tag == Options.TagTable.TagA)
                {
                    while (Istack.Count > 0)
                    {
                        InlineStack stack = Istack.Pop();
                        if (stack.Tag == Options.TagTable.TagA)
                        {
                            break;
                        }
                    }

                    if (Insert >= Istack.Count)
                    {
                        Insert = - 1;
                    }
                    return;
                }
            }

            if (Istack.Count <= 0) return;

            if (Insert >= Istack.Count)
            {
                Insert = - 1;
            }
        }

        public virtual bool IsPushed(Node node)
        {
            int i;

            for (i = Istack.Count - 1; i >= 0; --i)
            {
                InlineStack stack = (Istack.ToArray())[Istack.Count - (i + 1)];
                if (stack.Tag == node.Tag)
                {
                    return true;
                }
            }

            return false;
        }

        /*
		This has the effect of inserting "missing" inline
		elements around the contents of blocklevel elements
		such as P, TD, TH, DIV, PRE etc. This procedure is
		called at the start of ParseBlock. when the inline
		stack is not empty, as will be the case in:
		
		<i><h1>italic heading</h1></i>
		
		which is then treated as equivalent to
		
		<h1><i>italic heading</i></h1>
		
		This is implemented by setting the lexer into a mode
		where it gets tokens from the inline stack rather than
		from the input stream.
		*/

        public virtual int InlineDup(Node node)
        {
            int n;

            n = Istack.Count - Istackbase;
            if (n > 0)
            {
                Insert = Istackbase;
                Inode = node;
            }

            return n;
        }

        public virtual Node InsertedToken()
        {
            Node node;
            int n;

            // this will only be null if inode != null
            if (Insert == - 1)
            {
                node = Inode;
                Inode = null;
                return node;
            }

            // is this is the "latest" node then update
            // the position, otherwise use current values

            if (Inode == null)
            {
                Lines = Input.CursorLine;
                Columns = Input.CursorColumn;
            }

            node = NewNode(Node.START_TAG, Lexbuf, Txtstart, Txtend); // GLP:  Bugfix 126261.  Remove when this change
            //       is fixed in istack.c in the original Tidy
            node.Isimplicit = true;
            InlineStack stack = (Istack.ToArray())[Istack.Count - (Insert + 1)];
            node.Element = stack.Element;
            node.Tag = stack.Tag;
            if (stack.Attributes != null)
            {
                node.Attributes = CloneAttributes(stack.Attributes);
            }

            // advance lexer to next item on the stack
            n = Insert;

            // and recover state if we have reached the end
            if (++n < Istack.Count)
            {
                Insert = n;
            }
            else
            {
                Insert = - 1;
            }

            return node;
        }

        public virtual bool CanPrune(Node element)
        {
            if (element.Type == Node.TEXT_NODE)
                return true;

            if (element.Content != null)
                return false;

            if (element.Tag == Options.TagTable.TagA && element.Attributes != null)
                return false;

            if (element.Tag == Options.TagTable.TagP && !Options.DropEmptyParas)
                return false;

            if (element.Tag == null)
                return false;

            if ((element.Tag.Model & ContentModel.ROW) != 0)
                return false;

            if (element.Tag == Options.TagTable.TagApplet)
                return false;

            if (element.Tag == Options.TagTable.TagObject)
                return false;

            if (element.Attributes != null &&
                (element.GetAttrByName("id") != null || element.GetAttrByName("name") != null))
                return false;

            return true;
        }

        /* duplicate name attribute as an id */

        public virtual void FixId(Node node)
        {
            AttVal name = node.GetAttrByName("name");
            AttVal id = node.GetAttrByName("id");

            if (name != null)
            {
                if (id != null)
                {
                    if (!id.Val.Equals(name.Val))
                    {
                        Report.AttrError(this, node, "name", Report.ID_NAME_MISMATCH);
                    }
                }
                else if (Options.XmlOut)
                {
                    node.AddAttribute("id", name.Val);
                }
            }
        }

        /*
		defer duplicates when entering a table or other
		element where the inlines shouldn't be duplicated
		*/

        public virtual void DeferDup()
        {
            Insert = - 1;
            Inode = null;
        }

        /* Private methods and fields */

        /* lexer char types */

        private static void MapStr(string str, int code)
        {
            for (int i = 0; i < str.Length; i++)
            {
                int j = str[i];
                Lexmap[j] |= code;
            }
        }


        private static short Map(char c)
        {
            return ((int) c < 128 ? (short) Lexmap[c] : (short) 0);
        }

        //private static bool IsWhite(char c)
        //{
        //    short m = Map(c);

        //    return (m & WHITE) != 0;
        //}

        //private static bool IsDigit(char c)
        //{
        //    short m;

        //    m = Map(c);

        //    return (m & DIGIT) != 0;
        //}

        //private static bool IsLetter(char c)
        //{
        //    short m;

        //    m = Map(c);

        //    return (m & LETTER) != 0;
        //}

        //private static char ToLower(char c)
        //{
        //    short m = Map(c);

        //    if ((m & UPPERCASE) != 0)
        //        c = (char) (c + 'a' - 'A');

        //    return c;
        //}

        //private static char ToUpper(char c)
        //{
        //    short m = Map(c);

        //    if ((m & LOWERCASE) != 0)
        //        c = (char) (c + 'A' - 'a');

        //    return c;
        //}

        public static char FoldCase(char c, bool tocaps, bool xmlTags)
        {
            if (!xmlTags)
            {
                short m = Map(c);

                if (tocaps)
                {
                    if ((m & LOWERCASE) != 0)
                        c = (char) (c + 'A' - 'a');
                }
                    /* force to lower case */
                else
                {
                    if ((m & UPPERCASE) != 0)
                        c = (char) (c + 'a' - 'A');
                }
            }

            return c;
        }


        private class W3CVersionInfo
        {
            internal readonly string Name;
            internal readonly string Profile;
            internal readonly HtmlVersion Version;
            internal readonly string VoyagerName;

            public W3CVersionInfo(string name, string voyagerName, string profile, HtmlVersion version)
            {
                Name = name;
                VoyagerName = voyagerName;
                Profile = profile;
                Version = version;
            }
        }

        /* the 3 URIs  for the XHTML 1.0 DTDs */
    }
}