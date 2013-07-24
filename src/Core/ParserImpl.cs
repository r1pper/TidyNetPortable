using System;

namespace Tidy.Core
{
    /// <summary>
    ///     HTML Parser implementation
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
    internal class ParserImpl
    {
        static ParserImpl()
        {
            ParseOptGroup = new ParseOptGroupCheckTable();
            ParseText = new ParseTextCheckTable();
            ParseSelect = new ParseSelectCheckTable();
            ParseNoFrames = new ParseNoFramesCheckTable();
            ParseRow = new ParseRowCheckTable();
            ParseRowGroup = new ParseRowGroupCheckTable();
            ParseColGroup = new ParseColGroupCheckTable();
            ParseTableTag = new ParseTableTagCheckTable();
            ParseBlock = new ParseBlockCheckTable();
            ParsePre = new ParsePreCheckTable();
            ParseDefList = new ParseDefListCheckTable();
            ParseList = new ParseListCheckTable();
            ParseInline = new ParseInlineCheckTable();
            ParseFrameSet = new ParseFrameSetCheckTable();
            ParseBody = new ParseBodyCheckTable();
            ParseScript = new ParseScriptCheckTable();
            ParseTitle = new ParseTitleCheckTable();
            ParseHead = new ParseHeadCheckTable();
            ParseHtml = new ParseHtmlCheckTable();
        }

        public static IParser ParseHtml { get; private set; }
        public static IParser ParseHead { get; private set; }
        public static IParser ParseTitle { get; private set; }
        public static IParser ParseScript { get; private set; }
        public static IParser ParseBody { get; private set; }
        public static IParser ParseFrameSet { get; private set; }
        public static IParser ParseInline { get; private set; }
        public static IParser ParseList { get; private set; }
        public static IParser ParseDefList { get; private set; }
        public static IParser ParsePre { get; private set; }
        public static IParser ParseBlock { get; private set; }
        public static IParser ParseTableTag { get; private set; }
        public static IParser ParseColGroup { get; private set; }
        public static IParser ParseRowGroup { get; private set; }
        public static IParser ParseRow { get; private set; }
        public static IParser ParseNoFrames { get; private set; }
        public static IParser ParseSelect { get; private set; }
        public static IParser ParseText { get; private set; }
        public static IParser ParseOptGroup { get; private set; }

        private static void ParseTag(Lexer lexer, Node node, short mode)
        {
            // Local fix by GLP 2000-12-21.  Need to reset insertspace if this 
            // is both a non-inline and empty tag (base, link, meta, isindex, hr, area).
            // Remove this code once the fix is made in Tidy.

            if ((node.Tag.Model & ContentModel.INLINE) == 0)
            {
                lexer.Insertspace = false;
            }

            if ((node.Tag.Model & ContentModel.EMPTY) != 0)
            {
                lexer.Waswhite = false;
                return;
            }

            if (node.Tag.Parser == null || node.Type == Node.START_END_TAG)
            {
                return;
            }

            node.Tag.Parser.Parse(lexer, node, mode);
        }

        private static void MoveToHead(Lexer lexer, Node element, Node node)
        {
            TagCollection tt = lexer.Options.TagTable;


            if (node.Type == Node.START_TAG || node.Type == Node.START_END_TAG)
            {
                Report.Warning(lexer, element, node, Report.TAG_NOT_ALLOWED_IN);

                while (element.Tag != tt.TagHtml)
                {
                    element = element.Parent;
                }

                Node head;
                for (head = element.Content; head != null; head = head.Next)
                {
                    if (head.Tag != tt.TagHead) continue;
                    Node.InsertNodeAtEnd(head, node);
                    break;
                }

                if (node.Tag.Parser != null)
                {
                    ParseTag(lexer, node, Lexer.IGNORE_WHITESPACE);
                }
            }
            else
            {
                Report.Warning(lexer, element, node, Report.DISCARDING_UNEXPECTED);
            }
        }

        public static Node ParseDocument(Lexer lexer)
        {
            Node doctype = null;
            TagCollection tt = lexer.Options.TagTable;

            Node document = lexer.NewNode();
            document.Type = Node.ROOT_NODE;

            while (true)
            {
                Node node = lexer.GetToken(Lexer.IGNORE_WHITESPACE);
                if (node == null)
                {
                    break;
                }

                /* deal with comments etc. */
                if (Node.InsertMisc(document, node))
                {
                    continue;
                }

                if (node.Type == Node.DOC_TYPE_TAG)
                {
                    if (doctype == null)
                    {
                        Node.InsertNodeAtEnd(document, node);
                        doctype = node;
                    }
                    else
                    {
                        Report.Warning(lexer, document, node, Report.DISCARDING_UNEXPECTED);
                    }
                    continue;
                }

                if (node.Type == Node.END_TAG)
                {
                    Report.Warning(lexer, document, node, Report.DISCARDING_UNEXPECTED); //TODO?
                    continue;
                }

                Node html;
                if (node.Type != Node.START_TAG || node.Tag != tt.TagHtml)
                {
                    lexer.UngetToken();
                    html = lexer.InferredTag("html");
                }
                else
                {
                    html = node;
                }

                Node.InsertNodeAtEnd(document, html);
                ParseHtml.Parse(lexer, html, 0); // TODO?
                break;
            }

            return document;
        }

        /// <summary>
        ///     Indicates whether or not whitespace should be preserved for this element.
        ///     If an <code>xml:space</code> attribute is found, then if the attribute value is
        ///     <code>preserve</code>, returns <code>true</code>.  For any other value, returns
        ///     <code>false</code>.  If an <code>xml:space</code> attribute was <em>not</em>
        ///     found, then the following element names result in a return value of
        ///     <code>true:
        /// pre, script, style,</code>
        ///     and <code>xsl:text</code>.  Finally, if a
        ///     <code>TagTable</code> was passed in and the element appears as the "pre" element
        ///     in the <code>TagTable</code>, then <code>true</code> will be returned.
        ///     Otherwise, <code>false</code> is returned.
        /// </summary>
        /// <param name="element">
        ///     The <code>Node</code> to test to see if whitespace should be
        ///     preserved.
        /// </param>
        /// <param name="tt">
        ///     The <code>TagTable</code> to test for the <code>getNodePre()</code>
        ///     function.  This may be <code>null</code>, in which case this test
        ///     is bypassed.
        /// </param>
        /// <returns>
        ///     <code>true</code> or <code>false</code>, as explained above.
        /// </returns>
        public static bool XmlPreserveWhiteSpace(Node element, TagCollection tt)
        {
            AttVal attribute;

            /* search attributes for xml:space */
            for (attribute = element.Attributes; attribute != null; attribute = attribute.Next)
            {
                if (attribute.Attribute.Equals("xml:space"))
                {
                    if (attribute.Val.Equals("preserve"))
                    {
                        return true;
                    }

                    return false;
                }
            }

            /* kludge for html docs without explicit xml:space attribute */
            if (String.CompareOrdinal(element.Element, "pre") == 0 ||
                String.CompareOrdinal(element.Element, "script") == 0 ||
                String.CompareOrdinal(element.Element, "style") == 0)
            {
                return true;
            }

            if ((tt != null) && (tt.FindParser(element) == ParsePre))
            {
                return true;
            }

            /* kludge for XSL docs */
            if (String.CompareOrdinal(element.Element, "xsl:text") == 0)
            {
                return true;
            }

            return false;
        }

        /*
		XML documents
		*/

        public static void ParseXmlElement(Lexer lexer, Node element, short mode)
        {
            Node node;

            /* Jeff Young's kludge for XSL docs */
            if (String.CompareOrdinal(element.Element, "xsl:text") == 0)
            {
                return;
            }

            /* if node is pre or has xml:space="preserve" then do so */

            if (XmlPreserveWhiteSpace(element, lexer.Options.TagTable))
            {
                mode = Lexer.PREFORMATTED;
            }

            while (true)
            {
                node = lexer.GetToken(mode);
                if (node == null)
                {
                    break;
                }
                if (node.Type == Node.END_TAG && node.Element.Equals(element.Element))
                {
                    element.Closed = true;
                    break;
                }

                /* discard unexpected end tags */
                if (node.Type == Node.END_TAG)
                {
                    Report.Error(lexer, element, node, Report.UNEXPECTED_ENDTAG);
                    continue;
                }

                /* parse content on seeing start tag */
                if (node.Type == Node.START_TAG)
                {
                    ParseXmlElement(lexer, node, mode);
                }

                Node.InsertNodeAtEnd(element, node);
            }

            /*
			if first child is text then trim initial space and
			delete text node if it is empty.
			*/

            node = element.Content;

            if (node != null && node.Type == Node.TEXT_NODE && mode != Lexer.PREFORMATTED)
            {
                if (node.Textarray[node.Start] == (sbyte) ' ')
                {
                    node.Start++;

                    if (node.Start >= node.End)
                    {
                        Node.DiscardElement(node);
                    }
                }
            }

            /*
			if last child is text then trim final space and
			delete the text node if it is empty
			*/

            node = element.Last;

            if (node != null && node.Type == Node.TEXT_NODE && mode != Lexer.PREFORMATTED)
            {
                if (node.Textarray[node.End - 1] == (sbyte) ' ')
                {
                    node.End--;

                    if (node.Start >= node.End)
                    {
                        Node.DiscardElement(node);
                    }
                }
            }
        }

        public static Node ParseXmlDocument(Lexer lexer)
        {
            Node document = lexer.NewNode();
            document.Type = Node.ROOT_NODE;
            Node doctype = null;
            lexer.Options.XmlTags = true;

            while (true)
            {
                Node node = lexer.GetToken(Lexer.IGNORE_WHITESPACE);
                if (node == null)
                {
                    break;
                }

                /* discard unexpected end tags */
                if (node.Type == Node.END_TAG)
                {
                    Report.Warning(lexer, null, node, Report.UNEXPECTED_ENDTAG);
                    continue;
                }

                /* deal with comments etc. */
                if (Node.InsertMisc(document, node))
                {
                    continue;
                }

                if (node.Type == Node.DOC_TYPE_TAG)
                {
                    if (doctype == null)
                    {
                        Node.InsertNodeAtEnd(document, node);
                        doctype = node;
                    }
                    else
                    {
                        Report.Warning(lexer, document, node, Report.DISCARDING_UNEXPECTED);
                    }
                    // TODO
                    continue;
                }

                /* if start tag then parse element's content */
                if (node.Type == Node.START_TAG)
                {
                    Node.InsertNodeAtEnd(document, node);
                    ParseXmlElement(lexer, node, Lexer.IGNORE_WHITESPACE);
                }
            }

            if (doctype != null && !lexer.CheckDocTypeKeyWords(doctype))
            {
                Report.Warning(lexer, doctype, null, Report.DTYPE_NOT_UPPER_CASE);
            }

            /* ensure presence of initial <?XML version="1.0"?> */
            if (lexer.Options.XmlPi)
            {
                lexer.FixXmlPi(document);
            }

            return document;
        }

        public static bool IsJavaScript(Node node)
        {
            bool result = false;
            AttVal attr;

            if (node.Attributes == null)
            {
                return true;
            }

            for (attr = node.Attributes; attr != null; attr = attr.Next)
            {
                if ((String.CompareOrdinal(attr.Attribute, "language") == 0 ||
                     String.CompareOrdinal(attr.Attribute, "type") == 0) &&
                    Wsubstr(attr.Val, "javascript"))
                {
                    result = true;
                }
            }

            return result;
        }

        private static bool Wsubstr(string s1, string s2)
        {
            int i;
            int len1 = s1.Length;
            int len2 = s2.Length;

            for (i = 0; i <= len1 - len2; ++i)
            {
                if (s2.ToUpper().Equals(s1.Substring(i).ToUpper()))
                {
                    return true;
                }
            }

            return false;
        }

        public class ParseBlockCheckTable : IParser
        {
            /*
			element is node created by the lexer
			upon seeing the start tag, or by the
			parser when the start tag is inferred
			*/

            public virtual void Parse(Lexer lexer, Node element, short mode)
            {
                Node node;
                bool checkstack;
                int istackbase = 0;
                TagCollection tt = lexer.Options.TagTable;

                checkstack = true;

                if ((element.Tag.Model & ContentModel.EMPTY) != 0)
                {
                    return;
                }

                if (element.Tag == tt.TagForm && element.IsDescendantOf(tt.TagForm))
                {
                    Report.Warning(lexer, element, null, Report.ILLEGAL_NESTING);
                }

                /*
				InlineDup() asks the lexer to insert inline emphasis tags
				currently pushed on the istack, but take care to avoid
				propagating inline emphasis inside OBJECT or APPLET.
				For these elements a fresh inline stack context is created
				and disposed of upon reaching the end of the element.
				They thus behave like table cells in this respect.
				*/
                if ((element.Tag.Model & ContentModel.OBJECT) != 0)
                {
                    istackbase = lexer.Istackbase;
                    lexer.Istackbase = lexer.Istack.Count;
                }

                if ((element.Tag.Model & ContentModel.MIXED) == 0)
                {
                    lexer.InlineDup(null);
                }

                mode = Lexer.IGNORE_WHITESPACE;

                while (true)
                {
                    node = lexer.GetToken(mode);
                    if (node == null)
                    {
                        break;
                    }

                    /* end tag for this element */
                    if (node.Type == Node.END_TAG && node.Tag != null &&
                        (node.Tag == element.Tag || element.Was == node.Tag))
                    {
                        if ((element.Tag.Model & ContentModel.OBJECT) != 0)
                        {
                            /* pop inline stack */
                            while (lexer.Istack.Count > lexer.Istackbase)
                            {
                                lexer.PopInline(null);
                            }
                            lexer.Istackbase = istackbase;
                        }

                        element.Closed = true;
                        Node.TrimSpaces(lexer, element);
                        Node.TrimEmptyElement(lexer, element);
                        return;
                    }

                    if (node.Tag == tt.TagHtml || node.Tag == tt.TagHead || node.Tag == tt.TagBody)
                    {
                        if (node.Type == Node.START_TAG || node.Type == Node.START_END_TAG)
                        {
                            Report.Warning(lexer, element, node, Report.DISCARDING_UNEXPECTED);
                        }

                        continue;
                    }

                    if (node.Type == Node.END_TAG)
                    {
                        if (node.Tag == null)
                        {
                            Report.Warning(lexer, element, node, Report.DISCARDING_UNEXPECTED);

                            continue;
                        }
                        if (node.Tag == tt.TagBr)
                        {
                            node.Type = Node.START_TAG;
                        }
                        else if (node.Tag == tt.TagP)
                        {
                            Node.CoerceNode(lexer, node, tt.TagBr);
                            Node.InsertNodeAtEnd(element, node);
                            node = lexer.InferredTag("br");
                        }
                        else
                        {
                            /* 
							if this is the end tag for an ancestor element
							then infer end tag for this element
							*/
                            Node parent;
                            for (parent = element.Parent; parent != null; parent = parent.Parent)
                            {
                                if (node.Tag != parent.Tag) continue;
                                if ((element.Tag.Model & ContentModel.OPT) == 0)
                                {
                                    Report.Warning(lexer, element, node, Report.MISSING_ENDTAG_BEFORE);
                                }

                                lexer.UngetToken();

                                if ((element.Tag.Model & ContentModel.OBJECT) != 0)
                                {
                                    /* pop inline stack */
                                    while (lexer.Istack.Count > lexer.Istackbase)
                                    {
                                        lexer.PopInline(null);
                                    }
                                    lexer.Istackbase = istackbase;
                                }

                                Node.TrimSpaces(lexer, element);
                                Node.TrimEmptyElement(lexer, element);
                                return;
                            }
                            /* special case </tr> etc. for stuff moved in front of table */
                            if (lexer.Exiled && node.Tag.Model != 0 && (node.Tag.Model & ContentModel.TABLE) != 0)
                            {
                                lexer.UngetToken();
                                Node.TrimSpaces(lexer, element);
                                Node.TrimEmptyElement(lexer, element);
                                return;
                            }
                        }
                    }

                    /* mixed content model permits text */
                    if (node.Type == Node.TEXT_NODE)
                    {
                        bool iswhitenode = node.Type == Node.TEXT_NODE && node.End <= node.Start + 1 &&
                                           lexer.Lexbuf[node.Start] == (sbyte) ' ';

                        if (lexer.Options.EncloseBlockText && !iswhitenode)
                        {
                            lexer.UngetToken();
                            node = lexer.InferredTag("p");
                            Node.InsertNodeAtEnd(element, node);
                            ParseTag(lexer, node, Lexer.MIXED_CONTENT);
                            continue;
                        }

                        if (checkstack)
                        {
                            checkstack = false;

                            if ((element.Tag.Model & ContentModel.MIXED) == 0)
                            {
                                if (lexer.InlineDup(node) > 0)
                                {
                                    continue;
                                }
                            }
                        }

                        Node.InsertNodeAtEnd(element, node);
                        mode = Lexer.MIXED_CONTENT;
                        /*
						HTML4 strict doesn't allow mixed content for
						elements with %block; as their content model
						*/
                        lexer.Versions &= ~ HtmlVersion.Html40Strict;
                        continue;
                    }

                    if (Node.InsertMisc(element, node))
                    {
                        continue;
                    }

                    /* allow PARAM elements? */
                    if (node.Tag == tt.TagParam)
                    {
                        if (((element.Tag.Model & ContentModel.PARAM) != 0) &&
                            (node.Type == Node.START_TAG || node.Type == Node.START_END_TAG))
                        {
                            Node.InsertNodeAtEnd(element, node);
                            continue;
                        }

                        /* otherwise discard it */
                        Report.Warning(lexer, element, node, Report.DISCARDING_UNEXPECTED);
                        continue;
                    }

                    /* allow AREA elements? */
                    if (node.Tag == tt.TagArea)
                    {
                        if ((element.Tag == tt.TagMap) &&
                            (node.Type == Node.START_TAG || node.Type == Node.START_END_TAG))
                        {
                            Node.InsertNodeAtEnd(element, node);
                            continue;
                        }

                        /* otherwise discard it */
                        Report.Warning(lexer, element, node, Report.DISCARDING_UNEXPECTED);
                        continue;
                    }

                    /* ignore unknown start/end tags */
                    if (node.Tag == null)
                    {
                        Report.Warning(lexer, element, node, Report.DISCARDING_UNEXPECTED);
                        continue;
                    }

                    /*
					Allow ContentModel.INLINE elements here.
					
					Allow ContentModel.BLOCK elements here unless
					lexer.excludeBlocks is yes.
					
					LI and DD are special cased.
					
					Otherwise infer end tag for this element.
					*/

                    if ((node.Tag.Model & ContentModel.INLINE) == 0)
                    {
                        if (node.Type != Node.START_TAG && node.Type != Node.START_END_TAG)
                        {
                            Report.Warning(lexer, element, node, Report.DISCARDING_UNEXPECTED);
                            continue;
                        }

                        if (element.Tag == tt.TagTd || element.Tag == tt.TagTh)
                        {
                            /* if parent is a table cell, avoid inferring the end of the cell */

                            if ((node.Tag.Model & ContentModel.HEAD) != 0)
                            {
                                MoveToHead(lexer, element, node);
                                continue;
                            }

                            if ((node.Tag.Model & ContentModel.LIST) != 0)
                            {
                                lexer.UngetToken();
                                node = lexer.InferredTag("ul");
                                Node.AddClass(node, "noindent");
                                lexer.ExcludeBlocks = true;
                            }
                            else if ((node.Tag.Model & ContentModel.DEFLIST) != 0)
                            {
                                lexer.UngetToken();
                                node = lexer.InferredTag("dl");
                                lexer.ExcludeBlocks = true;
                            }

                            /* infer end of current table cell */
                            if ((node.Tag.Model & ContentModel.BLOCK) == 0)
                            {
                                lexer.UngetToken();
                                Node.TrimSpaces(lexer, element);
                                Node.TrimEmptyElement(lexer, element);
                                return;
                            }
                        }
                        else if ((node.Tag.Model & ContentModel.BLOCK) != 0)
                        {
                            if (lexer.ExcludeBlocks)
                            {
                                if ((element.Tag.Model & ContentModel.OPT) == 0)
                                    Report.Warning(lexer, element, node, Report.MISSING_ENDTAG_BEFORE);

                                lexer.UngetToken();

                                if ((element.Tag.Model & ContentModel.OBJECT) != 0)
                                    lexer.Istackbase = istackbase;

                                Node.TrimSpaces(lexer, element);
                                Node.TrimEmptyElement(lexer, element);
                                return;
                            }
                        }
                            /* things like list items */
                        else
                        {
                            if ((element.Tag.Model & ContentModel.OPT) == 0 && !element.Isimplicit)
                                Report.Warning(lexer, element, node, Report.MISSING_ENDTAG_BEFORE);

                            if ((node.Tag.Model & ContentModel.HEAD) != 0)
                            {
                                MoveToHead(lexer, element, node);
                                continue;
                            }

                            lexer.UngetToken();

                            if ((node.Tag.Model & ContentModel.LIST) != 0)
                            {
                                if (element.Parent != null && element.Parent.Tag != null &&
                                    element.Parent.Tag.Parser == ParseList)
                                {
                                    Node.TrimSpaces(lexer, element);
                                    Node.TrimEmptyElement(lexer, element);
                                    return;
                                }

                                node = lexer.InferredTag("ul");
                                Node.AddClass(node, "noindent");
                            }
                            else if ((node.Tag.Model & ContentModel.DEFLIST) != 0)
                            {
                                if (element.Parent.Tag == tt.TagDl)
                                {
                                    Node.TrimSpaces(lexer, element);
                                    Node.TrimEmptyElement(lexer, element);
                                    return;
                                }

                                node = lexer.InferredTag("dl");
                            }
                            else if ((node.Tag.Model & ContentModel.TABLE) != 0 ||
                                     (node.Tag.Model & ContentModel.ROW) != 0)
                            {
                                node = lexer.InferredTag("table");
                            }
                            else if ((element.Tag.Model & ContentModel.OBJECT) != 0)
                            {
                                /* pop inline stack */
                                while (lexer.Istack.Count > lexer.Istackbase)
                                {
                                    lexer.PopInline(null);
                                }
                                lexer.Istackbase = istackbase;
                                Node.TrimSpaces(lexer, element);
                                Node.TrimEmptyElement(lexer, element);
                                return;
                            }
                            else
                            {
                                Node.TrimSpaces(lexer, element);
                                Node.TrimEmptyElement(lexer, element);
                                return;
                            }
                        }
                    }

                    /* parse known element */
                    if (node.Type == Node.START_TAG || node.Type == Node.START_END_TAG)
                    {
                        if ((node.Tag.Model & ContentModel.INLINE) != 0)
                        {
                            if (checkstack && !node.Isimplicit)
                            {
                                checkstack = false;

                                if (lexer.InlineDup(node) > 0)
                                    continue;
                            }

                            mode = Lexer.MIXED_CONTENT;
                        }
                        else
                        {
                            checkstack = true;
                            mode = Lexer.IGNORE_WHITESPACE;
                        }

                        /* trim white space before <br> */
                        if (node.Tag == tt.TagBr)
                        {
                            Node.TrimSpaces(lexer, element);
                        }

                        Node.InsertNodeAtEnd(element, node);

                        if (node.Isimplicit)
                        {
                            Report.Warning(lexer, element, node, Report.INSERTING_TAG);
                        }

                        ParseTag(lexer, node, Lexer.IGNORE_WHITESPACE);
                        continue;
                    }

                    /* discard unexpected tags */
                    if (node.Type == Node.END_TAG)
                        lexer.PopInline(node);
                    /* if inline end tag */

                    Report.Warning(lexer, element, node, Report.DISCARDING_UNEXPECTED);
                }

                if ((element.Tag.Model & ContentModel.OPT) == 0)
                {
                    Report.Warning(lexer, element, node, Report.MISSING_ENDTAG_FOR);
                }

                if ((element.Tag.Model & ContentModel.OBJECT) != 0)
                {
                    /* pop inline stack */
                    while (lexer.Istack.Count > lexer.Istackbase)
                    {
                        lexer.PopInline(null);
                    }
                    lexer.Istackbase = istackbase;
                }

                Node.TrimSpaces(lexer, element);
                Node.TrimEmptyElement(lexer, element);
            }

            public virtual void Parse(Lexer lexer, Node element)
            {
                Parse(lexer, element, 0);
            }
        }


        public class ParseBodyCheckTable : IParser
        {
            public virtual void Parse(Lexer lexer, Node body, short mode)
            {
                bool checkstack;

                mode = Lexer.IGNORE_WHITESPACE;
                checkstack = true;
                TagCollection tt = lexer.Options.TagTable;

                while (true)
                {
                    Node node = lexer.GetToken(mode);
                    if (node == null)
                    {
                        break;
                    }
                    if (node.Tag == body.Tag && node.Type == Node.END_TAG)
                    {
                        body.Closed = true;
                        Node.TrimSpaces(lexer, body);
                        lexer.SeenBodyEndTag = 1;
                        mode = Lexer.IGNORE_WHITESPACE;

                        if (body.Parent.Tag == tt.TagNoframes)
                        {
                            break;
                        }

                        continue;
                    }

                    if (node.Tag == tt.TagNoframes)
                    {
                        if (node.Type == Node.START_TAG)
                        {
                            Node.InsertNodeAtEnd(body, node);
                            ParseBlock.Parse(lexer, node, mode);
                            continue;
                        }

                        if (node.Type == Node.END_TAG && body.Parent.Tag == tt.TagNoframes)
                        {
                            Node.TrimSpaces(lexer, body);
                            lexer.UngetToken();
                            break;
                        }
                    }

                    if ((node.Tag == tt.TagFrame || node.Tag == tt.TagFrameset) && body.Parent.Tag == tt.TagNoframes)
                    {
                        Node.TrimSpaces(lexer, body);
                        lexer.UngetToken();
                        break;
                    }

                    if (node.Tag == tt.TagHtml)
                    {
                        if (node.Type == Node.START_TAG || node.Type == Node.START_END_TAG)
                        {
                            Report.Warning(lexer, body, node, Report.DISCARDING_UNEXPECTED);
                        }

                        continue;
                    }

                    bool iswhitenode = node.Type == Node.TEXT_NODE && node.End <= node.Start + 1 &&
                                       node.Textarray[node.Start] == (sbyte) ' ';

                    /* deal with comments etc. */
                    if (Node.InsertMisc(body, node))
                    {
                        continue;
                    }

                    if (lexer.SeenBodyEndTag == 1 && !iswhitenode)
                    {
                        ++lexer.SeenBodyEndTag;
                        Report.Warning(lexer, body, node, Report.CONTENT_AFTER_BODY);
                    }

                    /* mixed content model permits text */
                    if (node.Type == Node.TEXT_NODE)
                    {
                        if (iswhitenode && mode == Lexer.IGNORE_WHITESPACE)
                        {
                            continue;
                        }

                        if (lexer.Options.EncloseText && !iswhitenode)
                        {
                            lexer.UngetToken();
                            Node para = lexer.InferredTag("p");
                            Node.InsertNodeAtEnd(body, para);
                            ParseTag(lexer, para, mode);
                            mode = Lexer.MIXED_CONTENT;
                            continue;
                        }
                        /* strict doesn't allow text here */
                        lexer.Versions &= ~ (HtmlVersion.Html40Strict | HtmlVersion.Html20);

                        if (checkstack)
                        {
                            checkstack = false;

                            if (lexer.InlineDup(node) > 0)
                            {
                                continue;
                            }
                        }

                        Node.InsertNodeAtEnd(body, node);
                        mode = Lexer.MIXED_CONTENT;
                        continue;
                    }

                    if (node.Type == Node.DOC_TYPE_TAG)
                    {
                        Node.InsertDocType(lexer, body, node);
                        continue;
                    }
                    /* discard unknown  and PARAM tags */
                    if (node.Tag == null || node.Tag == tt.TagParam)
                    {
                        Report.Warning(lexer, body, node, Report.DISCARDING_UNEXPECTED);
                        continue;
                    }

                    /*
					Netscape allows LI and DD directly in BODY
					We infer UL or DL respectively and use this
					boolean to exclude block-level elements so as
					to match Netscape's observed behaviour.
					*/
                    lexer.ExcludeBlocks = false;

                    if ((node.Tag.Model & ContentModel.BLOCK) == 0 && (node.Tag.Model & ContentModel.INLINE) == 0)
                    {
                        /* avoid this error message being issued twice */
                        if ((node.Tag.Model & ContentModel.HEAD) == 0)
                        {
                            Report.Warning(lexer, body, node, Report.TAG_NOT_ALLOWED_IN);
                        }

                        if ((node.Tag.Model & ContentModel.HTML) != 0)
                        {
                            /* copy body attributes if current body was inferred */
                            if (node.Tag == tt.TagBody && body.Isimplicit && body.Attributes == null)
                            {
                                body.Attributes = node.Attributes;
                                node.Attributes = null;
                            }

                            continue;
                        }

                        if ((node.Tag.Model & ContentModel.HEAD) != 0)
                        {
                            MoveToHead(lexer, body, node);
                            continue;
                        }

                        if ((node.Tag.Model & ContentModel.LIST) != 0)
                        {
                            lexer.UngetToken();
                            node = lexer.InferredTag("ul");
                            Node.AddClass(node, "noindent");
                            lexer.ExcludeBlocks = true;
                        }
                        else if ((node.Tag.Model & ContentModel.DEFLIST) != 0)
                        {
                            lexer.UngetToken();
                            node = lexer.InferredTag("dl");
                            lexer.ExcludeBlocks = true;
                        }
                        else if ((node.Tag.Model & (ContentModel.TABLE | ContentModel.ROWGRP | ContentModel.ROW)) != 0)
                        {
                            lexer.UngetToken();
                            node = lexer.InferredTag("table");
                            lexer.ExcludeBlocks = true;
                        }
                        else
                        {
                            /* AQ: The following line is from the official C
							version of tidy.  It doesn't make sense to me
							because the '!' operator has higher precedence
							than the '&' operator.  It seems to me that the
							expression always evaluates to 0.
							
							if (!node->tag->model & (CM_ROW | CM_FIELD))
							
							AQ: 13Jan2000 fixed in C tidy
							*/
                            if ((node.Tag.Model & (ContentModel.ROW | ContentModel.FIELD)) == 0)
                            {
                                lexer.UngetToken();
                                return;
                            }

                            /* ignore </td> </th> <option> etc. */
                            continue;
                        }
                    }

                    if (node.Type == Node.END_TAG)
                    {
                        if (node.Tag == tt.TagBr)
                        {
                            node.Type = Node.START_TAG;
                        }
                        else if (node.Tag == tt.TagP)
                        {
                            Node.CoerceNode(lexer, node, tt.TagBr);
                            Node.InsertNodeAtEnd(body, node);
                            node = lexer.InferredTag("br");
                        }
                        else if ((node.Tag.Model & ContentModel.INLINE) != 0)
                        {
                            lexer.PopInline(node);
                        }
                    }

                    if (node.Type == Node.START_TAG || node.Type == Node.START_END_TAG)
                    {
                        if (((node.Tag.Model & ContentModel.INLINE) != 0) &&
                            (node.Tag.Model & ContentModel.MIXED) == 0)
                        {
                            /* HTML4 strict doesn't allow inline content here */
                            /* but HTML2 does allow img elements as children of body */
                            if (node.Tag == tt.TagImg)
                            {
                                lexer.Versions &= ~ HtmlVersion.Html40Strict;
                            }
                            else
                            {
                                lexer.Versions &= ~ (HtmlVersion.Html40Strict | HtmlVersion.Html20);
                            }

                            if (checkstack && !node.Isimplicit)
                            {
                                checkstack = false;

                                if (lexer.InlineDup(node) > 0)
                                {
                                    continue;
                                }
                            }

                            mode = Lexer.MIXED_CONTENT;
                        }
                        else
                        {
                            checkstack = true;
                            mode = Lexer.IGNORE_WHITESPACE;
                        }

                        if (node.Isimplicit)
                        {
                            Report.Warning(lexer, body, node, Report.INSERTING_TAG);
                        }

                        Node.InsertNodeAtEnd(body, node);
                        ParseTag(lexer, node, mode);
                        continue;
                    }

                    /* discard unexpected tags */
                    Report.Warning(lexer, body, node, Report.DISCARDING_UNEXPECTED);
                }
            }

            public virtual void Parse(Lexer lexer, Node body)
            {
                Parse(lexer, body, 0);
            }
        }

        public class ParseColGroupCheckTable : IParser
        {
            public virtual void Parse(Lexer lexer, Node colgroup, short mode)
            {
                TagCollection tt = lexer.Options.TagTable;

                if ((colgroup.Tag.Model & ContentModel.EMPTY) != 0)
                    return;

                while (true)
                {
                    Node node = lexer.GetToken(Lexer.IGNORE_WHITESPACE);
                    if (node == null)
                        break;
                    if (node.Tag == colgroup.Tag && node.Type == Node.END_TAG)
                    {
                        colgroup.Closed = true;
                        return;
                    }

                    /* 
					if this is the end tag for an ancestor element
					then infer end tag for this element
					*/
                    if (node.Type == Node.END_TAG)
                    {
                        if (node.Tag == tt.TagForm)
                        {
                            lexer.BadForm = 1;
                            Report.Warning(lexer, colgroup, node, Report.DISCARDING_UNEXPECTED);
                            continue;
                        }

                        Node parent;
                        for (parent = colgroup.Parent; parent != null; parent = parent.Parent)
                        {
                            if (node.Tag == parent.Tag)
                            {
                                lexer.UngetToken();
                                return;
                            }
                        }
                    }

                    if (node.Type == Node.TEXT_NODE)
                    {
                        lexer.UngetToken();
                        return;
                    }

                    /* deal with comments etc. */
                    if (Node.InsertMisc(colgroup, node))
                        continue;

                    /* discard unknown tags */
                    if (node.Tag == null)
                    {
                        Report.Warning(lexer, colgroup, node, Report.DISCARDING_UNEXPECTED);
                        continue;
                    }

                    if (node.Tag != tt.TagCol)
                    {
                        lexer.UngetToken();
                        return;
                    }

                    if (node.Type == Node.END_TAG)
                    {
                        Report.Warning(lexer, colgroup, node, Report.DISCARDING_UNEXPECTED);
                        continue;
                    }

                    /* node should be <COL> */
                    Node.InsertNodeAtEnd(colgroup, node);
                    ParseTag(lexer, node, Lexer.IGNORE_WHITESPACE);
                }
            }
        }

        public class ParseDefListCheckTable : IParser
        {
            public virtual void Parse(Lexer lexer, Node list, short mode)
            {
                TagCollection tt = lexer.Options.TagTable;

                if ((list.Tag.Model & ContentModel.EMPTY) != 0)
                    return;

                lexer.Insert = - 1; /* defer implicit inline start tags */

                while (true)
                {
                    Node node = lexer.GetToken(Lexer.IGNORE_WHITESPACE);
                    if (node == null)
                    {
                        break;
                    }
                    if (node.Tag == list.Tag && node.Type == Node.END_TAG)
                    {
                        list.Closed = true;
                        Node.TrimEmptyElement(lexer, list);
                        return;
                    }

                    /* deal with comments etc. */
                    if (Node.InsertMisc(list, node))
                    {
                        continue;
                    }

                    if (node.Type == Node.TEXT_NODE)
                    {
                        lexer.UngetToken();
                        node = lexer.InferredTag("dt");
                        Report.Warning(lexer, list, node, Report.MISSING_STARTTAG);
                    }

                    if (node.Tag == null)
                    {
                        Report.Warning(lexer, list, node, Report.DISCARDING_UNEXPECTED);
                        continue;
                    }

                    /* 
					if this is the end tag for an ancestor element
					then infer end tag for this element
					*/
                    if (node.Type == Node.END_TAG)
                    {
                        if (node.Tag == tt.TagForm)
                        {
                            lexer.BadForm = 1;
                            Report.Warning(lexer, list, node, Report.DISCARDING_UNEXPECTED);
                            continue;
                        }

                        Node parent;
                        for (parent = list.Parent; parent != null; parent = parent.Parent)
                        {
                            if (node.Tag == parent.Tag)
                            {
                                Report.Warning(lexer, list, node, Report.MISSING_ENDTAG_BEFORE);

                                lexer.UngetToken();
                                Node.TrimEmptyElement(lexer, list);
                                return;
                            }
                        }
                    }

                    /* center in a dt or a dl breaks the dl list in two */
                    if (node.Tag == tt.TagCenter)
                    {
                        if (list.Content != null)
                        {
                            Node.InsertNodeAfterElement(list, node);
                        }
                        else
                        {
                            /* trim empty dl list */
                            Node.InsertNodeBeforeElement(list, node);
                            Node.DiscardElement(list);
                        }

                        /* and parse contents of center */
                        ParseTag(lexer, node, mode);

                        /* now create a new dl element */
                        list = lexer.InferredTag("dl");
                        Node.InsertNodeAfterElement(node, list);
                        continue;
                    }

                    if (!(node.Tag == tt.TagDt || node.Tag == tt.TagDd))
                    {
                        lexer.UngetToken();

                        if ((node.Tag.Model & (ContentModel.BLOCK | ContentModel.INLINE)) == 0)
                        {
                            Report.Warning(lexer, list, node, Report.TAG_NOT_ALLOWED_IN);
                            Node.TrimEmptyElement(lexer, list);
                            return;
                        }

                        /* if DD appeared directly in BODY then exclude blocks */
                        if ((node.Tag.Model & ContentModel.INLINE) == 0 && lexer.ExcludeBlocks)
                        {
                            Node.TrimEmptyElement(lexer, list);
                            return;
                        }

                        node = lexer.InferredTag("dd");
                        Report.Warning(lexer, list, node, Report.MISSING_STARTTAG);
                    }

                    if (node.Type == Node.END_TAG)
                    {
                        Report.Warning(lexer, list, node, Report.DISCARDING_UNEXPECTED);
                        continue;
                    }

                    /* node should be <DT> or <DD>*/
                    Node.InsertNodeAtEnd(list, node);
                    ParseTag(lexer, node, Lexer.IGNORE_WHITESPACE);
                }

                Report.Warning(lexer, list, null, Report.MISSING_ENDTAG_FOR);
                Node.TrimEmptyElement(lexer, list);
            }
        }


        public class ParseFrameSetCheckTable : IParser
        {
            public virtual void Parse(Lexer lexer, Node frameset, short mode)
            {
                TagCollection tt = lexer.Options.TagTable;

                lexer.BadAccess |= Report.USING_FRAMES;

                while (true)
                {
                    Node node = lexer.GetToken(Lexer.IGNORE_WHITESPACE);
                    if (node == null)
                    {
                        break;
                    }
                    if (node.Tag == frameset.Tag && node.Type == Node.END_TAG)
                    {
                        frameset.Closed = true;
                        Node.TrimSpaces(lexer, frameset);
                        return;
                    }

                    /* deal with comments etc. */
                    if (Node.InsertMisc(frameset, node))
                    {
                        continue;
                    }

                    if (node.Tag == null)
                    {
                        Report.Warning(lexer, frameset, node, Report.DISCARDING_UNEXPECTED);
                        continue;
                    }

                    if (node.Type == Node.START_TAG || node.Type == Node.START_END_TAG)
                    {
                        if (node.Tag != null && (node.Tag.Model & ContentModel.HEAD) != 0)
                        {
                            MoveToHead(lexer, frameset, node);
                            continue;
                        }
                    }

                    if (node.Tag == tt.TagBody)
                    {
                        lexer.UngetToken();
                        node = lexer.InferredTag("noframes");
                        Report.Warning(lexer, frameset, node, Report.INSERTING_TAG);
                    }

                    if (node.Type == Node.START_TAG && (node.Tag.Model & ContentModel.FRAMES) != 0)
                    {
                        Node.InsertNodeAtEnd(frameset, node);
                        lexer.ExcludeBlocks = false;
                        ParseTag(lexer, node, Lexer.MIXED_CONTENT);
                        continue;
                    }
                    if (node.Type == Node.START_END_TAG && (node.Tag.Model & ContentModel.FRAMES) != 0)
                    {
                        Node.InsertNodeAtEnd(frameset, node);
                        continue;
                    }

                    /* discard unexpected tags */
                    Report.Warning(lexer, frameset, node, Report.DISCARDING_UNEXPECTED);
                }

                Report.Warning(lexer, frameset, null, Report.MISSING_ENDTAG_FOR);
            }
        }


        public class ParseHeadCheckTable : IParser
        {
            public virtual void Parse(Lexer lexer, Node head, short mode)
            {
                int hasTitle = 0;
                int hasBase = 0;
                TagCollection tt = lexer.Options.TagTable;

                while (true)
                {
                    Node node = lexer.GetToken(Lexer.IGNORE_WHITESPACE);
                    if (node == null)
                    {
                        break;
                    }
                    if (node.Tag == head.Tag && node.Type == Node.END_TAG)
                    {
                        head.Closed = true;
                        break;
                    }

                    if (node.Type == Node.TEXT_NODE)
                    {
                        lexer.UngetToken();
                        break;
                    }

                    /* deal with comments etc. */
                    if (Node.InsertMisc(head, node))
                    {
                        continue;
                    }

                    if (node.Type == Node.DOC_TYPE_TAG)
                    {
                        Node.InsertDocType(lexer, head, node);
                        continue;
                    }

                    /* discard unknown tags */
                    if (node.Tag == null)
                    {
                        Report.Warning(lexer, head, node, Report.DISCARDING_UNEXPECTED);
                        continue;
                    }

                    if ((node.Tag.Model & ContentModel.HEAD) == 0)
                    {
                        lexer.UngetToken();
                        break;
                    }

                    if (node.Type == Node.START_TAG || node.Type == Node.START_END_TAG)
                    {
                        if (node.Tag == tt.TagTitle)
                        {
                            ++hasTitle;

                            if (hasTitle > 1)
                            {
                                Report.Warning(lexer, head, node, Report.TOO_MANY_ELEMENTS);
                            }
                        }
                        else if (node.Tag == tt.TagBase)
                        {
                            ++hasBase;

                            if (hasBase > 1)
                            {
                                Report.Warning(lexer, head, node, Report.TOO_MANY_ELEMENTS);
                            }
                        }
                        else if (node.Tag == tt.TagNoscript)
                        {
                            Report.Warning(lexer, head, node, Report.TAG_NOT_ALLOWED_IN);
                        }

                        Node.InsertNodeAtEnd(head, node);
                        ParseTag(lexer, node, Lexer.IGNORE_WHITESPACE);
                        continue;
                    }

                    /* discard unexpected text nodes and end tags */
                    Report.Warning(lexer, head, node, Report.DISCARDING_UNEXPECTED);
                }

                if (hasTitle == 0)
                {
                    Report.Warning(lexer, head, null, Report.MISSING_TITLE_ELEMENT);
                    Node.InsertNodeAtEnd(head, lexer.InferredTag("title"));
                }
            }
        }

        public class ParseHtmlCheckTable : IParser
        {
            public virtual void Parse(Lexer lexer, Node html, short mode)
            {
                Node node;
                Node frameset = null;
                Node noframes = null;

                lexer.Options.XmlTags = false;
                lexer.SeenBodyEndTag = 0;
                TagCollection tt = lexer.Options.TagTable;

                for (;;)
                {
                    node = lexer.GetToken(Lexer.IGNORE_WHITESPACE);

                    if (node == null)
                    {
                        node = lexer.InferredTag("head");
                        break;
                    }

                    if (node.Tag == tt.TagHead)
                        break;

                    if (node.Tag == html.Tag && node.Type == Node.END_TAG)
                    {
                        Report.Warning(lexer, html, node, Report.DISCARDING_UNEXPECTED);
                        continue;
                    }

                    /* deal with comments etc. */
                    if (Node.InsertMisc(html, node))
                    {
                        continue;
                    }

                    lexer.UngetToken();
                    node = lexer.InferredTag("head");
                    break;
                }

                Node head = node;
                Node.InsertNodeAtEnd(html, head);
                ParseHead.Parse(lexer, head, mode);

                for (;;)
                {
                    node = lexer.GetToken(Lexer.IGNORE_WHITESPACE);

                    if (node == null)
                    {
                        if (frameset == null)
                        {
                            /* create an empty body */
                            //node = lexer.InferredTag("body");
                        }

                        return;
                    }

                    /* robustly handle html tags */
                    if (node.Tag == html.Tag)
                    {
                        if (node.Type != Node.START_TAG && frameset == null)
                        {
                            Report.Warning(lexer, html, node, Report.DISCARDING_UNEXPECTED);
                        }

                        continue;
                    }

                    /* deal with comments etc. */
                    if (Node.InsertMisc(html, node))
                    {
                        continue;
                    }

                    /* if frameset document coerce <body> to <noframes> */
                    if (node.Tag == tt.TagBody)
                    {
                        if (node.Type != Node.START_TAG)
                        {
                            Report.Warning(lexer, html, node, Report.DISCARDING_UNEXPECTED);
                            continue;
                        }

                        if (frameset != null)
                        {
                            lexer.UngetToken();

                            if (noframes == null)
                            {
                                noframes = lexer.InferredTag("noframes");
                                Node.InsertNodeAtEnd(frameset, noframes);
                                Report.Warning(lexer, html, noframes, Report.INSERTING_TAG);
                            }

                            ParseTag(lexer, noframes, mode);
                            continue;
                        }

                        break; /* to parse body */
                    }

                    /* flag an error if we see more than one frameset */
                    if (node.Tag == tt.TagFrameset)
                    {
                        if (node.Type != Node.START_TAG)
                        {
                            Report.Warning(lexer, html, node, Report.DISCARDING_UNEXPECTED);
                            continue;
                        }

                        if (frameset != null)
                        {
                            Report.Error(lexer, html, node, Report.DUPLICATE_FRAMESET);
                        }
                        else
                        {
                            frameset = node;
                        }

                        Node.InsertNodeAtEnd(html, node);
                        ParseTag(lexer, node, mode);

                        /*
						see if it includes a noframes element so
						that we can merge subsequent noframes elements
						*/

                        for (node = frameset.Content; node != null; node = node.Next)
                        {
                            if (node.Tag == tt.TagNoframes)
                            {
                                noframes = node;
                            }
                        }
                        continue;
                    }

                    /* if not a frameset document coerce <noframes> to <body> */
                    if (node.Tag == tt.TagNoframes)
                    {
                        if (node.Type != Node.START_TAG)
                        {
                            Report.Warning(lexer, html, node, Report.DISCARDING_UNEXPECTED);
                            continue;
                        }

                        if (frameset == null)
                        {
                            Report.Warning(lexer, html, node, Report.DISCARDING_UNEXPECTED);
                            node = lexer.InferredTag("body");
                            break;
                        }

                        if (noframes == null)
                        {
                            noframes = node;
                            Node.InsertNodeAtEnd(frameset, noframes);
                        }

                        ParseTag(lexer, noframes, mode);
                        continue;
                    }

                    if (node.Type == Node.START_TAG || node.Type == Node.START_END_TAG)
                    {
                        if (node.Tag != null && (node.Tag.Model & ContentModel.HEAD) != 0)
                        {
                            MoveToHead(lexer, html, node);
                            continue;
                        }
                    }

                    lexer.UngetToken();

                    /* insert other content into noframes element */

                    if (frameset != null)
                    {
                        if (noframes == null)
                        {
                            noframes = lexer.InferredTag("noframes");
                            Node.InsertNodeAtEnd(frameset, noframes);
                        }
                        else
                        {
                            Report.Warning(lexer, html, node, Report.NOFRAMES_CONTENT);
                        }

                        ParseTag(lexer, noframes, mode);
                        continue;
                    }

                    node = lexer.InferredTag("body");
                    break;
                }

                /* node must be body */

                Node.InsertNodeAtEnd(html, node);
                ParseTag(lexer, node, mode);
            }
        }


        public class ParseInlineCheckTable : IParser
        {
            public virtual void Parse(Lexer lexer, Node element, short mode)
            {
                Node node;
                TagCollection tt = lexer.Options.TagTable;

                if ((element.Tag.Model & ContentModel.EMPTY) != 0)
                {
                    return;
                }

                if (element.Tag == tt.TagA)
                {
                    if (element.Attributes == null)
                    {
                        Report.Warning(lexer, element.Parent, element, Report.DISCARDING_UNEXPECTED);
                        Node.DiscardElement(element);
                        return;
                    }
                }

                /*
				ParseInline is used for some block level elements like H1 to H6
				For such elements we need to insert inline emphasis tags currently
				on the inline stack. For Inline elements, we normally push them
				onto the inline stack provided they aren't implicit or OBJECT/APPLET.
				This test is carried out in PushInline and PopInline, see istack.c
				We don't push A or SPAN to replicate current browser behavior
				*/
                if (((element.Tag.Model & ContentModel.BLOCK) != 0) || (element.Tag == tt.TagDt))
                {
                    lexer.InlineDup(null);
                }
                else if ((element.Tag.Model & ContentModel.INLINE) != 0 && element.Tag != tt.TagA &&
                         element.Tag != tt.TagSpan)
                {
                    lexer.PushInline(element);
                }

                if (element.Tag == tt.TagNobr)
                {
                    lexer.BadLayout |= Report.USING_NOBR;
                }
                else if (element.Tag == tt.TagFont)
                {
                    lexer.BadLayout |= Report.USING_FONT;
                }

                /* Inline elements may or may not be within a preformatted element */
                if (mode != Lexer.PREFORMATTED)
                {
                    mode = Lexer.MIXED_CONTENT;
                }

                while (true)
                {
                    node = lexer.GetToken(mode);
                    if (node == null)
                    {
                        break;
                    }
                    /* end tag for current element */
                    if (node.Tag == element.Tag && node.Type == Node.END_TAG)
                    {
                        if ((element.Tag.Model & ContentModel.INLINE) != 0 && element.Tag != tt.TagA)
                        {
                            lexer.PopInline(node);
                        }

                        if ((mode & Lexer.PREFORMATTED) == 0)
                        {
                            Node.TrimSpaces(lexer, element);
                        }
                        /*
						if a font element wraps an anchor and nothing else
						then move the font element inside the anchor since
						otherwise it won't alter the anchor text color
						*/
                        if (element.Tag == tt.TagFont && element.Content != null && element.Content == element.Last)
                        {
                            Node child = element.Content;

                            if (child.Tag == tt.TagA)
                            {
                                child.Parent = element.Parent;
                                child.Next = element.Next;
                                child.Prev = element.Prev;

                                if (child.Prev != null)
                                {
                                    child.Prev.Next = child;
                                }
                                else
                                {
                                    child.Parent.Content = child;
                                }

                                if (child.Next != null)
                                {
                                    child.Next.Prev = child;
                                }
                                else
                                {
                                    child.Parent.Last = child;
                                }

                                element.Next = null;
                                element.Prev = null;
                                element.Parent = child;
                                element.Content = child.Content;
                                element.Last = child.Last;
                                child.Content = element;
                                child.Last = element;
                                for (child = element.Content; child != null; child = child.Next)
                                {
                                    child.Parent = element;
                                }
                            }
                        }
                        element.Closed = true;
                        Node.TrimSpaces(lexer, element);
                        Node.TrimEmptyElement(lexer, element);
                        return;
                    }

                    /* <u>...<u>  map 2nd <u> to </u> if 1st is explicit */
                    /* otherwise emphasis nesting is probably unintentional */
                    /* big and small have cumulative effect to leave them alone */
                    if (node.Type == Node.START_TAG && node.Tag == element.Tag && lexer.IsPushed(node) &&
                        !node.Isimplicit && !element.Isimplicit && node.Tag != null &&
                        ((node.Tag.Model & ContentModel.INLINE) != 0) && node.Tag != tt.TagA && node.Tag != tt.TagFont &&
                        node.Tag != tt.TagBig && node.Tag != tt.TagSmall)
                    {
                        if (element.Content != null && node.Attributes == null)
                        {
                            Report.Warning(lexer, element, node, Report.COERCE_TO_ENDTAG);
                            node.Type = Node.END_TAG;
                            lexer.UngetToken();
                            continue;
                        }

                        Report.Warning(lexer, element, node, Report.NESTED_EMPHASIS);
                    }

                    if (node.Type == Node.TEXT_NODE)
                    {
                        /* only called for 1st child */
                        if (element.Content == null && (mode & Lexer.PREFORMATTED) == 0)
                        {
                            Node.TrimSpaces(lexer, element);
                        }

                        if (node.Start >= node.End)
                        {
                            continue;
                        }

                        Node.InsertNodeAtEnd(element, node);
                        continue;
                    }

                    /* mixed content model so allow text */
                    if (Node.InsertMisc(element, node))
                    {
                        continue;
                    }

                    /* deal with HTML tags */
                    if (node.Tag == tt.TagHtml)
                    {
                        if (node.Type == Node.START_TAG || node.Type == Node.START_END_TAG)
                        {
                            Report.Warning(lexer, element, node, Report.DISCARDING_UNEXPECTED);
                            continue;
                        }

                        /* otherwise infer end of inline element */
                        lexer.UngetToken();
                        if ((mode & Lexer.PREFORMATTED) == 0)
                        {
                            Node.TrimSpaces(lexer, element);
                        }
                        Node.TrimEmptyElement(lexer, element);
                        return;
                    }

                    /* within <dt> or <pre> map <p> to <br> */
                    if (node.Tag == tt.TagP && node.Type == Node.START_TAG &&
                        ((mode & Lexer.PREFORMATTED) != 0 || element.Tag == tt.TagDt || element.IsDescendantOf(tt.TagDt)))
                    {
                        node.Tag = tt.TagBr;
                        node.Element = "br";
                        Node.TrimSpaces(lexer, element);
                        Node.InsertNodeAtEnd(element, node);
                        continue;
                    }

                    /* ignore unknown and PARAM tags */
                    if (node.Tag == null || node.Tag == tt.TagParam)
                    {
                        Report.Warning(lexer, element, node, Report.DISCARDING_UNEXPECTED);
                        continue;
                    }

                    if (node.Tag == tt.TagBr && node.Type == Node.END_TAG)
                    {
                        node.Type = Node.START_TAG;
                    }

                    if (node.Type == Node.END_TAG)
                    {
                        /* coerce </br> to <br> */
                        if (node.Tag == tt.TagBr)
                        {
                            node.Type = Node.START_TAG;
                        }
                        else if (node.Tag == tt.TagP)
                        {
                            /* coerce unmatched </p> to <br><br> */
                            if (!element.IsDescendantOf(tt.TagP))
                            {
                                Node.CoerceNode(lexer, node, tt.TagBr);
                                Node.TrimSpaces(lexer, element);
                                Node.InsertNodeAtEnd(element, node);
                                //node = lexer.InferredTag("br");
                                continue;
                            }
                        }
                        else if ((node.Tag.Model & ContentModel.INLINE) != 0 && node.Tag != tt.TagA &&
                                 (node.Tag.Model & ContentModel.OBJECT) == 0 &&
                                 (element.Tag.Model & ContentModel.INLINE) != 0)
                        {
                            /* allow any inline end tag to end current element */
                            lexer.PopInline(element);

                            if (element.Tag != tt.TagA)
                            {
                                if (node.Tag == tt.TagA && node.Tag != element.Tag)
                                {
                                    Report.Warning(lexer, element, node, Report.MISSING_ENDTAG_BEFORE);
                                    lexer.UngetToken();
                                }
                                else
                                {
                                    Report.Warning(lexer, element, node, Report.NON_MATCHING_ENDTAG);
                                }

                                if ((mode & Lexer.PREFORMATTED) == 0)
                                {
                                    Node.TrimSpaces(lexer, element);
                                }
                                Node.TrimEmptyElement(lexer, element);
                                return;
                            }

                            /* if parent is <a> then discard unexpected inline end tag */
                            Report.Warning(lexer, element, node, Report.DISCARDING_UNEXPECTED);
                            continue;
                        }
                            /* special case </tr> etc. for stuff moved in front of table */
                        else if (lexer.Exiled && node.Tag.Model != 0 && (node.Tag.Model & ContentModel.TABLE) != 0)
                        {
                            lexer.UngetToken();
                            Node.TrimSpaces(lexer, element);
                            Node.TrimEmptyElement(lexer, element);
                            return;
                        }
                    }

                    /* allow any header tag to end current header */
                    if ((node.Tag.Model & ContentModel.HEADING) != 0 && (element.Tag.Model & ContentModel.HEADING) != 0)
                    {
                        if (node.Tag == element.Tag)
                        {
                            Report.Warning(lexer, element, node, Report.NON_MATCHING_ENDTAG);
                        }
                        else
                        {
                            Report.Warning(lexer, element, node, Report.MISSING_ENDTAG_BEFORE);
                            lexer.UngetToken();
                        }
                        if ((mode & Lexer.PREFORMATTED) == 0)
                        {
                            Node.TrimSpaces(lexer, element);
                        }
                        Node.TrimEmptyElement(lexer, element);
                        return;
                    }

                    /*
					an <A> tag to ends any open <A> element
					but <A href=...> is mapped to </A><A href=...>
					*/
                    if (node.Tag == tt.TagA && !node.Isimplicit && lexer.IsPushed(node))
                    {
                        /* coerce <a> to </a> unless it has some attributes */
                        if (node.Attributes == null)
                        {
                            node.Type = Node.END_TAG;
                            Report.Warning(lexer, element, node, Report.COERCE_TO_ENDTAG);
                            lexer.PopInline(node);
                            lexer.UngetToken();
                            continue;
                        }

                        lexer.UngetToken();
                        Report.Warning(lexer, element, node, Report.MISSING_ENDTAG_BEFORE);
                        lexer.PopInline(element);
                        if ((mode & Lexer.PREFORMATTED) == 0)
                        {
                            Node.TrimSpaces(lexer, element);
                        }
                        Node.TrimEmptyElement(lexer, element);
                        return;
                    }

                    if ((element.Tag.Model & ContentModel.HEADING) != 0)
                    {
                        if (node.Tag == tt.TagCenter || node.Tag == tt.TagDiv)
                        {
                            if (node.Type != Node.START_TAG && node.Type != Node.START_END_TAG)
                            {
                                Report.Warning(lexer, element, node, Report.DISCARDING_UNEXPECTED);
                                continue;
                            }

                            Report.Warning(lexer, element, node, Report.TAG_NOT_ALLOWED_IN);

                            /* insert center as parent if heading is empty */
                            if (element.Content == null)
                            {
                                Node.InsertNodeAsParent(element, node);
                                continue;
                            }

                            /* split heading and make center parent of 2nd part */
                            Node.InsertNodeAfterElement(element, node);

                            if ((mode & Lexer.PREFORMATTED) == 0)
                            {
                                Node.TrimSpaces(lexer, element);
                            }

                            element = lexer.CloneNode(element);
                            element.Start = lexer.Lexsize;
                            element.End = lexer.Lexsize;
                            Node.InsertNodeAtEnd(node, element);
                            continue;
                        }

                        if (node.Tag == tt.TagHr)
                        {
                            if (node.Type != Node.START_TAG && node.Type != Node.START_END_TAG)
                            {
                                Report.Warning(lexer, element, node, Report.DISCARDING_UNEXPECTED);
                                continue;
                            }

                            Report.Warning(lexer, element, node, Report.TAG_NOT_ALLOWED_IN);

                            /* insert hr before heading if heading is empty */
                            if (element.Content == null)
                            {
                                Node.InsertNodeBeforeElement(element, node);
                                continue;
                            }

                            /* split heading and insert hr before 2nd part */
                            Node.InsertNodeAfterElement(element, node);

                            if ((mode & Lexer.PREFORMATTED) == 0)
                            {
                                Node.TrimSpaces(lexer, element);
                            }

                            element = lexer.CloneNode(element);
                            element.Start = lexer.Lexsize;
                            element.End = lexer.Lexsize;
                            Node.InsertNodeAfterElement(node, element);
                            continue;
                        }
                    }

                    if (element.Tag == tt.TagDt)
                    {
                        if (node.Tag == tt.TagHr)
                        {
                            if (node.Type != Node.START_TAG && node.Type != Node.START_END_TAG)
                            {
                                Report.Warning(lexer, element, node, Report.DISCARDING_UNEXPECTED);
                                continue;
                            }

                            Report.Warning(lexer, element, node, Report.TAG_NOT_ALLOWED_IN);
                            Node dd = lexer.InferredTag("dd");

                            /* insert hr within dd before dt if dt is empty */
                            if (element.Content == null)
                            {
                                Node.InsertNodeBeforeElement(element, dd);
                                Node.InsertNodeAtEnd(dd, node);
                                continue;
                            }

                            /* split dt and insert hr within dd before 2nd part */
                            Node.InsertNodeAfterElement(element, dd);
                            Node.InsertNodeAtEnd(dd, node);

                            if ((mode & Lexer.PREFORMATTED) == 0)
                            {
                                Node.TrimSpaces(lexer, element);
                            }

                            element = lexer.CloneNode(element);
                            element.Start = lexer.Lexsize;
                            element.End = lexer.Lexsize;
                            Node.InsertNodeAfterElement(dd, element);
                            continue;
                        }
                    }

                    /* 
					if this is the end tag for an ancestor element
					then infer end tag for this element
					*/
                    if (node.Type == Node.END_TAG)
                    {
                        Node parent;
                        for (parent = element.Parent; parent != null; parent = parent.Parent)
                        {
                            if (node.Tag == parent.Tag)
                            {
                                if ((element.Tag.Model & ContentModel.OPT) == 0 && !element.Isimplicit)
                                {
                                    Report.Warning(lexer, element, node, Report.MISSING_ENDTAG_BEFORE);
                                }

                                if (element.Tag == tt.TagA)
                                {
                                    lexer.PopInline(element);
                                }

                                lexer.UngetToken();

                                if ((mode & Lexer.PREFORMATTED) == 0)
                                {
                                    Node.TrimSpaces(lexer, element);
                                }

                                Node.TrimEmptyElement(lexer, element);
                                return;
                            }
                        }
                    }

                    /* block level tags end this element */
                    if ((node.Tag.Model & ContentModel.INLINE) == 0)
                    {
                        if (node.Type != Node.START_TAG)
                        {
                            Report.Warning(lexer, element, node, Report.DISCARDING_UNEXPECTED);
                            continue;
                        }

                        if ((element.Tag.Model & ContentModel.OPT) == 0)
                        {
                            Report.Warning(lexer, element, node, Report.MISSING_ENDTAG_BEFORE);
                        }

                        if ((node.Tag.Model & ContentModel.HEAD) != 0 && (node.Tag.Model & ContentModel.BLOCK) == 0)
                        {
                            MoveToHead(lexer, element, node);
                            continue;
                        }

                        /*
						prevent anchors from propagating into block tags
						except for headings h1 to h6
						*/
                        if (element.Tag == tt.TagA)
                        {
                            if (node.Tag != null && (node.Tag.Model & ContentModel.HEADING) == 0)
                            {
                                lexer.PopInline(element);
                            }
                            else if (element.Content == null)
                            {
                                Node.DiscardElement(element);
                                lexer.UngetToken();
                                return;
                            }
                        }

                        lexer.UngetToken();

                        if ((mode & Lexer.PREFORMATTED) == 0)
                        {
                            Node.TrimSpaces(lexer, element);
                        }

                        Node.TrimEmptyElement(lexer, element);
                        return;
                    }

                    /* parse inline element */
                    if (node.Type == Node.START_TAG || node.Type == Node.START_END_TAG)
                    {
                        if (node.Isimplicit)
                        {
                            Report.Warning(lexer, element, node, Report.INSERTING_TAG);
                        }

                        /* trim white space before <br> */
                        if (node.Tag == tt.TagBr)
                        {
                            Node.TrimSpaces(lexer, element);
                        }

                        Node.InsertNodeAtEnd(element, node);
                        ParseTag(lexer, node, mode);
                        continue;
                    }

                    /* discard unexpected tags */
                    Report.Warning(lexer, element, node, Report.DISCARDING_UNEXPECTED);
                }

                if ((element.Tag.Model & ContentModel.OPT) == 0)
                {
                    Report.Warning(lexer, element, node, Report.MISSING_ENDTAG_FOR);
                }

                Node.TrimEmptyElement(lexer, element);
            }
        }


        public class ParseListCheckTable : IParser
        {
            public virtual void Parse(Lexer lexer, Node list, short mode)
            {
                TagCollection tt = lexer.Options.TagTable;

                if ((list.Tag.Model & ContentModel.EMPTY) != 0)
                {
                    return;
                }

                lexer.Insert = - 1; /* defer implicit inline start tags */

                while (true)
                {
                    Node node = lexer.GetToken(Lexer.IGNORE_WHITESPACE);
                    if (node == null)
                    {
                        break;
                    }

                    if (node.Tag == list.Tag && node.Type == Node.END_TAG)
                    {
                        if ((list.Tag.Model & ContentModel.OBSOLETE) != 0)
                        {
                            Node.CoerceNode(lexer, list, tt.TagUl);
                        }

                        list.Closed = true;
                        Node.TrimEmptyElement(lexer, list);
                        return;
                    }

                    /* deal with comments etc. */
                    if (Node.InsertMisc(list, node))
                    {
                        continue;
                    }

                    if (node.Type != Node.TEXT_NODE && node.Tag == null)
                    {
                        Report.Warning(lexer, list, node, Report.DISCARDING_UNEXPECTED);
                        continue;
                    }

                    /* 
					if this is the end tag for an ancestor element
					then infer end tag for this element
					*/
                    if (node.Type == Node.END_TAG)
                    {
                        if (node.Tag == tt.TagForm)
                        {
                            lexer.BadForm = 1;
                            Report.Warning(lexer, list, node, Report.DISCARDING_UNEXPECTED);
                            continue;
                        }

                        if (node.Tag != null && (node.Tag.Model & ContentModel.INLINE) != 0)
                        {
                            Report.Warning(lexer, list, node, Report.DISCARDING_UNEXPECTED);
                            lexer.PopInline(node);
                            continue;
                        }

                        Node parent;
                        for (parent = list.Parent; parent != null; parent = parent.Parent)
                        {
                            if (node.Tag == parent.Tag)
                            {
                                Report.Warning(lexer, list, node, Report.MISSING_ENDTAG_BEFORE);
                                lexer.UngetToken();

                                if ((list.Tag.Model & ContentModel.OBSOLETE) != 0)
                                {
                                    Node.CoerceNode(lexer, list, tt.TagUl);
                                }

                                Node.TrimEmptyElement(lexer, list);
                                return;
                            }
                        }

                        Report.Warning(lexer, list, node, Report.DISCARDING_UNEXPECTED);
                        continue;
                    }

                    if (node.Tag != tt.TagLi)
                    {
                        lexer.UngetToken();

                        if (node.Tag != null && (node.Tag.Model & ContentModel.BLOCK) != 0 && lexer.ExcludeBlocks)
                        {
                            Report.Warning(lexer, list, node, Report.MISSING_ENDTAG_BEFORE);
                            Node.TrimEmptyElement(lexer, list);
                            return;
                        }

                        node = lexer.InferredTag("li");
                        node.AddAttribute("style", "list-style: none");
                        Report.Warning(lexer, list, node, Report.MISSING_STARTTAG);
                    }

                    /* node should be <LI> */
                    Node.InsertNodeAtEnd(list, node);
                    ParseTag(lexer, node, Lexer.IGNORE_WHITESPACE);
                }

                if ((list.Tag.Model & ContentModel.OBSOLETE) != 0)
                {
                    Node.CoerceNode(lexer, list, tt.TagUl);
                }

                Report.Warning(lexer, list, null, Report.MISSING_ENDTAG_FOR);
                Node.TrimEmptyElement(lexer, list);
            }
        }

        public class ParseNoFramesCheckTable : IParser
        {
            public virtual void Parse(Lexer lexer, Node noframes, short mode)
            {
                TagCollection tt = lexer.Options.TagTable;

                lexer.BadAccess |= Report.USING_NOFRAMES;
                mode = Lexer.IGNORE_WHITESPACE;

                while (true)
                {
                    Node node = lexer.GetToken(mode);
                    if (node == null)
                        break;
                    if (node.Tag == noframes.Tag && node.Type == Node.END_TAG)
                    {
                        noframes.Closed = true;
                        Node.TrimSpaces(lexer, noframes);
                        return;
                    }

                    if ((node.Tag == tt.TagFrame || node.Tag == tt.TagFrameset))
                    {
                        Report.Warning(lexer, noframes, node, Report.MISSING_ENDTAG_BEFORE);
                        Node.TrimSpaces(lexer, noframes);
                        lexer.UngetToken();
                        return;
                    }

                    if (node.Tag == tt.TagHtml)
                    {
                        if (node.Type == Node.START_TAG || node.Type == Node.START_END_TAG)
                            Report.Warning(lexer, noframes, node, Report.DISCARDING_UNEXPECTED);

                        continue;
                    }

                    /* deal with comments etc. */
                    if (Node.InsertMisc(noframes, node))
                        continue;

                    if (node.Tag == tt.TagBody && node.Type == Node.START_TAG)
                    {
                        Node.InsertNodeAtEnd(noframes, node);
                        ParseTag(lexer, node, Lexer.IGNORE_WHITESPACE);
                        continue;
                    }

                    /* implicit body element inferred */
                    if (node.Type == Node.TEXT_NODE || node.Tag != null)
                    {
                        lexer.UngetToken();
                        node = lexer.InferredTag("body");
                        if (lexer.Options.XmlOut)
                            Report.Warning(lexer, noframes, node, Report.INSERTING_TAG);
                        Node.InsertNodeAtEnd(noframes, node);
                        ParseTag(lexer, node, Lexer.IGNORE_WHITESPACE);
                        continue;
                    }
                    /* discard unexpected end tags */
                    Report.Warning(lexer, noframes, node, Report.DISCARDING_UNEXPECTED);
                }

                Report.Warning(lexer, noframes, null, Report.MISSING_ENDTAG_FOR);
            }

            public virtual void Parse(Lexer lexer, Node noframes)
            {
                Parse(lexer, noframes, 0);
            }
        }

        public class ParseOptGroupCheckTable : IParser
        {
            public virtual void Parse(Lexer lexer, Node field, short mode)
            {
                TagCollection tt = lexer.Options.TagTable;

                lexer.Insert = - 1; /* defer implicit inline start tags */

                while (true)
                {
                    Node node = lexer.GetToken(Lexer.IGNORE_WHITESPACE);
                    if (node == null)
                        break;
                    if (node.Tag == field.Tag && node.Type == Node.END_TAG)
                    {
                        field.Closed = true;
                        Node.TrimSpaces(lexer, field);
                        return;
                    }

                    /* deal with comments etc. */
                    if (Node.InsertMisc(field, node))
                        continue;

                    if (node.Type == Node.START_TAG && (node.Tag == tt.TagOption || node.Tag == tt.TagOptgroup))
                    {
                        if (node.Tag == tt.TagOptgroup)
                            Report.Warning(lexer, field, node, Report.CANT_BE_NESTED);

                        Node.InsertNodeAtEnd(field, node);
                        ParseTag(lexer, node, Lexer.MIXED_CONTENT);
                        continue;
                    }

                    /* discard unexpected tags */
                    Report.Warning(lexer, field, node, Report.DISCARDING_UNEXPECTED);
                }
            }
        }


        public class ParsePreCheckTable : IParser
        {
            public virtual void Parse(Lexer lexer, Node pre, short mode)
            {
                TagCollection tt = lexer.Options.TagTable;

                if ((pre.Tag.Model & ContentModel.EMPTY) != 0)
                {
                    return;
                }

                if ((pre.Tag.Model & ContentModel.OBSOLETE) != 0)
                {
                    Node.CoerceNode(lexer, pre, tt.TagPre);
                }

                lexer.InlineDup(null); /* tell lexer to insert inlines if needed */

                while (true)
                {
                    Node node = lexer.GetToken(Lexer.PREFORMATTED);
                    if (node == null)
                    {
                        break;
                    }
                    if (node.Tag == pre.Tag && node.Type == Node.END_TAG)
                    {
                        Node.TrimSpaces(lexer, pre);
                        pre.Closed = true;
                        Node.TrimEmptyElement(lexer, pre);
                        return;
                    }

                    if (node.Tag == tt.TagHtml)
                    {
                        if (node.Type == Node.START_TAG || node.Type == Node.START_END_TAG)
                        {
                            Report.Warning(lexer, pre, node, Report.DISCARDING_UNEXPECTED);
                        }

                        continue;
                    }

                    if (node.Type == Node.TEXT_NODE)
                    {
                        /* if first check for inital newline */
                        if (pre.Content == null)
                        {
                            if (node.Textarray[node.Start] == (sbyte) '\n')
                            {
                                ++node.Start;
                            }

                            if (node.Start >= node.End)
                            {
                                continue;
                            }
                        }

                        Node.InsertNodeAtEnd(pre, node);
                        continue;
                    }

                    /* deal with comments etc. */
                    if (Node.InsertMisc(pre, node))
                    {
                        continue;
                    }

                    /* discard unknown  and PARAM tags */
                    if (node.Tag == null || node.Tag == tt.TagParam)
                    {
                        Report.Warning(lexer, pre, node, Report.DISCARDING_UNEXPECTED);
                        continue;
                    }

                    if (node.Tag == tt.TagP)
                    {
                        if (node.Type == Node.START_TAG)
                        {
                            Report.Warning(lexer, pre, node, Report.USING_BR_INPLACE_OF);

                            /* trim white space before <p> in <pre>*/
                            Node.TrimSpaces(lexer, pre);

                            /* coerce both <p> and </p> to <br> */
                            Node.CoerceNode(lexer, node, tt.TagBr);
                            Node.InsertNodeAtEnd(pre, node);
                        }
                        else
                        {
                            Report.Warning(lexer, pre, node, Report.DISCARDING_UNEXPECTED);
                        }
                        continue;
                    }

                    if ((node.Tag.Model & ContentModel.HEAD) != 0 && (node.Tag.Model & ContentModel.BLOCK) == 0)
                    {
                        MoveToHead(lexer, pre, node);
                        continue;
                    }

                    /* 
					if this is the end tag for an ancestor element
					then infer end tag for this element
					*/
                    if (node.Type == Node.END_TAG)
                    {
                        if (node.Tag == tt.TagForm)
                        {
                            lexer.BadForm = 1;
                            Report.Warning(lexer, pre, node, Report.DISCARDING_UNEXPECTED);
                            continue;
                        }

                        Node parent;
                        for (parent = pre.Parent; parent != null; parent = parent.Parent)
                        {
                            if (node.Tag == parent.Tag)
                            {
                                Report.Warning(lexer, pre, node, Report.MISSING_ENDTAG_BEFORE);

                                lexer.UngetToken();
                                Node.TrimSpaces(lexer, pre);
                                Node.TrimEmptyElement(lexer, pre);
                                return;
                            }
                        }
                    }

                    /* what about head content, HEAD, BODY tags etc? */
                    if ((node.Tag.Model & ContentModel.INLINE) == 0)
                    {
                        if (node.Type != Node.START_TAG)
                        {
                            Report.Warning(lexer, pre, node, Report.DISCARDING_UNEXPECTED);
                            continue;
                        }

                        Report.Warning(lexer, pre, node, Report.MISSING_ENDTAG_BEFORE);
                        lexer.ExcludeBlocks = true;

                        /* check if we need to infer a container */
                        if ((node.Tag.Model & ContentModel.LIST) != 0)
                        {
                            lexer.UngetToken();
                            node = lexer.InferredTag("ul");
                            Node.AddClass(node, "noindent");
                        }
                        else if ((node.Tag.Model & ContentModel.DEFLIST) != 0)
                        {
                            lexer.UngetToken();
                            node = lexer.InferredTag("dl");
                        }
                        else if ((node.Tag.Model & ContentModel.TABLE) != 0)
                        {
                            lexer.UngetToken();
                            node = lexer.InferredTag("table");
                        }

                        Node.InsertNodeAfterElement(pre, node);
                        pre = lexer.InferredTag("pre");
                        Node.InsertNodeAfterElement(node, pre);
                        ParseTag(lexer, node, Lexer.IGNORE_WHITESPACE);
                        lexer.ExcludeBlocks = false;
                        continue;
                    }
                    /*
					if (!((node.Tag.Model & ContentModel.INLINE) != 0))
					{
					Report.Warning(lexer, pre, node, Report.MISSING_ENDTAG_BEFORE);
					lexer.UngetToken();
					return;
					}
					*/
                    if (node.Type == Node.START_TAG || node.Type == Node.START_END_TAG)
                    {
                        /* trim white space before <br> */
                        if (node.Tag == tt.TagBr)
                        {
                            Node.TrimSpaces(lexer, pre);
                        }

                        Node.InsertNodeAtEnd(pre, node);
                        ParseTag(lexer, node, Lexer.PREFORMATTED);
                        continue;
                    }

                    /* discard unexpected tags */
                    Report.Warning(lexer, pre, node, Report.DISCARDING_UNEXPECTED);
                }

                Report.Warning(lexer, pre, null, Report.MISSING_ENDTAG_FOR);
                Node.TrimEmptyElement(lexer, pre);
            }
        }


        public class ParseRowCheckTable : IParser
        {
            public virtual void Parse(Lexer lexer, Node row, short mode)
            {
                TagCollection tt = lexer.Options.TagTable;

                if ((row.Tag.Model & ContentModel.EMPTY) != 0)
                    return;

                while (true)
                {
                    Node node = lexer.GetToken(Lexer.IGNORE_WHITESPACE);
                    if (node == null)
                        break;
                    if (node.Tag == row.Tag)
                    {
                        if (node.Type == Node.END_TAG)
                        {
                            row.Closed = true;
                            Node.FixEmptyRow(lexer, row);
                            return;
                        }

                        lexer.UngetToken();
                        Node.FixEmptyRow(lexer, row);
                        return;
                    }

                    /* 
					if this is the end tag for an ancestor element
					then infer end tag for this element
					*/
                    if (node.Type == Node.END_TAG)
                    {
                        if (node.Tag == tt.TagForm)
                        {
                            lexer.BadForm = 1;
                            Report.Warning(lexer, row, node, Report.DISCARDING_UNEXPECTED);
                            continue;
                        }

                        if (node.Tag == tt.TagTd || node.Tag == tt.TagTh)
                        {
                            Report.Warning(lexer, row, node, Report.DISCARDING_UNEXPECTED);
                            continue;
                        }

                        Node parent;
                        for (parent = row.Parent; parent != null; parent = parent.Parent)
                        {
                            if (node.Tag == parent.Tag)
                            {
                                lexer.UngetToken();
                                Node.TrimEmptyElement(lexer, row);
                                return;
                            }
                        }
                    }

                    /* deal with comments etc. */
                    if (Node.InsertMisc(row, node))
                        continue;

                    /* discard unknown tags */
                    if (node.Tag == null && node.Type != Node.TEXT_NODE)
                    {
                        Report.Warning(lexer, row, node, Report.DISCARDING_UNEXPECTED);
                        continue;
                    }

                    /* discard unexpected <table> element */
                    if (node.Tag == tt.TagTable)
                    {
                        Report.Warning(lexer, row, node, Report.DISCARDING_UNEXPECTED);
                        continue;
                    }

                    /* THEAD, TFOOT or TBODY */
                    if (node.Tag != null && (node.Tag.Model & ContentModel.ROWGRP) != 0)
                    {
                        lexer.UngetToken();
                        Node.TrimEmptyElement(lexer, row);
                        return;
                    }

                    if (node.Type == Node.END_TAG)
                    {
                        Report.Warning(lexer, row, node, Report.DISCARDING_UNEXPECTED);
                        continue;
                    }

                    /*
					if text or inline or block move before table
					if head content move to head
					*/

                    if (node.Type != Node.END_TAG)
                    {
                        if (node.Tag == tt.TagForm)
                        {
                            lexer.UngetToken();
                            node = lexer.InferredTag("td");
                            Report.Warning(lexer, row, node, Report.MISSING_STARTTAG);
                        }
                        else if (node.Tag != null && (node.Type == Node.TEXT_NODE ||
                                                      (node.Tag.Model & (ContentModel.BLOCK | ContentModel.INLINE)) != 0))
                        {
                            Node.MoveBeforeTable(row, node, tt);
                            Report.Warning(lexer, row, node, Report.TAG_NOT_ALLOWED_IN);
                            lexer.Exiled = true;

                            if (node.Type != Node.TEXT_NODE)
                                ParseTag(lexer, node, Lexer.IGNORE_WHITESPACE);

                            lexer.Exiled = false;
                            continue;
                        }
                        else if (node.Tag != null && (node.Tag.Model & ContentModel.HEAD) != 0)
                        {
                            Report.Warning(lexer, row, node, Report.TAG_NOT_ALLOWED_IN);
                            MoveToHead(lexer, row, node);
                            continue;
                        }
                    }

                    if (!(node.Tag == tt.TagTd || node.Tag == tt.TagTh))
                    {
                        Report.Warning(lexer, row, node, Report.TAG_NOT_ALLOWED_IN);
                        continue;
                    }

                    /* node should be <TD> or <TH> */
                    Node.InsertNodeAtEnd(row, node);
                    bool excludeState = lexer.ExcludeBlocks;
                    lexer.ExcludeBlocks = false;
                    ParseTag(lexer, node, Lexer.IGNORE_WHITESPACE);
                    lexer.ExcludeBlocks = excludeState;

                    /* pop inline stack */

                    while (lexer.Istack.Count > lexer.Istackbase)
                        lexer.PopInline(null);
                }

                Node.TrimEmptyElement(lexer, row);
            }
        }

        public class ParseRowGroupCheckTable : IParser
        {
            public virtual void Parse(Lexer lexer, Node rowgroup, short mode)
            {
                TagCollection tt = lexer.Options.TagTable;

                if ((rowgroup.Tag.Model & ContentModel.EMPTY) != 0)
                    return;

                while (true)
                {
                    Node node = lexer.GetToken(Lexer.IGNORE_WHITESPACE);
                    if (node == null)
                        break;
                    if (node.Tag == rowgroup.Tag)
                    {
                        if (node.Type == Node.END_TAG)
                        {
                            rowgroup.Closed = true;
                            Node.TrimEmptyElement(lexer, rowgroup);
                            return;
                        }

                        lexer.UngetToken();
                        return;
                    }

                    /* if </table> infer end tag */
                    if (node.Tag == tt.TagTable && node.Type == Node.END_TAG)
                    {
                        lexer.UngetToken();
                        Node.TrimEmptyElement(lexer, rowgroup);
                        return;
                    }

                    /* deal with comments etc. */
                    if (Node.InsertMisc(rowgroup, node))
                        continue;

                    /* discard unknown tags */
                    if (node.Tag == null && node.Type != Node.TEXT_NODE)
                    {
                        Report.Warning(lexer, rowgroup, node, Report.DISCARDING_UNEXPECTED);
                        continue;
                    }

                    /*
					if TD or TH then infer <TR>
					if text or inline or block move before table
					if head content move to head
					*/

                    if (node.Type != Node.END_TAG)
                    {
                        if (node.Tag == tt.TagTd || node.Tag == tt.TagTh)
                        {
                            lexer.UngetToken();
                            node = lexer.InferredTag("tr");
                            Report.Warning(lexer, rowgroup, node, Report.MISSING_STARTTAG);
                        }
                        else if (node.Tag != null && (node.Type == Node.TEXT_NODE ||
                                                      (node.Tag.Model & (ContentModel.BLOCK | ContentModel.INLINE)) != 0))
                        {
                            Node.MoveBeforeTable(rowgroup, node, tt);
                            Report.Warning(lexer, rowgroup, node, Report.TAG_NOT_ALLOWED_IN);
                            lexer.Exiled = true;

                            if (node.Type != Node.TEXT_NODE)
                                ParseTag(lexer, node, Lexer.IGNORE_WHITESPACE);

                            lexer.Exiled = false;
                            continue;
                        }
                        else if (node.Tag != null && (node.Tag.Model & ContentModel.HEAD) != 0)
                        {
                            Report.Warning(lexer, rowgroup, node, Report.TAG_NOT_ALLOWED_IN);
                            MoveToHead(lexer, rowgroup, node);
                            continue;
                        }
                    }

                    /* 
					if this is the end tag for ancestor element
					then infer end tag for this element
					*/
                    if (node.Type == Node.END_TAG)
                    {
                        if (node.Tag == tt.TagForm)
                        {
                            lexer.BadForm = 1;
                            Report.Warning(lexer, rowgroup, node, Report.DISCARDING_UNEXPECTED);
                            continue;
                        }

                        if (node.Tag == tt.TagTr || node.Tag == tt.TagTd || node.Tag == tt.TagTh)
                        {
                            Report.Warning(lexer, rowgroup, node, Report.DISCARDING_UNEXPECTED);
                            continue;
                        }

                        Node parent;
                        for (parent = rowgroup.Parent; parent != null; parent = parent.Parent)
                        {
                            if (node.Tag == parent.Tag)
                            {
                                lexer.UngetToken();
                                Node.TrimEmptyElement(lexer, rowgroup);
                                return;
                            }
                        }
                    }

                    /*
					if THEAD, TFOOT or TBODY then implied end tag
					
					*/
                    if (node.Tag != null && (node.Tag.Model & ContentModel.ROWGRP) != 0)
                    {
                        if (node.Type != Node.END_TAG)
                            lexer.UngetToken();

                        Node.TrimEmptyElement(lexer, rowgroup);
                        return;
                    }

                    if (node.Type == Node.END_TAG)
                    {
                        Report.Warning(lexer, rowgroup, node, Report.DISCARDING_UNEXPECTED);
                        continue;
                    }

                    if (node.Tag != tt.TagTr)
                    {
                        node = lexer.InferredTag("tr");
                        Report.Warning(lexer, rowgroup, node, Report.MISSING_STARTTAG);
                        lexer.UngetToken();
                    }

                    /* node should be <TR> */
                    Node.InsertNodeAtEnd(rowgroup, node);
                    ParseTag(lexer, node, Lexer.IGNORE_WHITESPACE);
                }

                Node.TrimEmptyElement(lexer, rowgroup);
            }
        }

        public class ParseScriptCheckTable : IParser
        {
            public virtual void Parse(Lexer lexer, Node script, short mode)
            {
                /*
				This isn't quite right for CDATA content as it recognises
				tags within the content and parses them accordingly.
				This will unfortunately screw up scripts which include
				< + letter,  < + !, < + ?  or  < + / + letter
				*/

                Node node = lexer.GetCdata(script);

                if (node != null)
                {
                    Node.InsertNodeAtEnd(script, node);
                }
            }
        }


        public class ParseSelectCheckTable : IParser
        {
            public virtual void Parse(Lexer lexer, Node field, short mode)
            {
                TagCollection tt = lexer.Options.TagTable;

                lexer.Insert = - 1; /* defer implicit inline start tags */

                while (true)
                {
                    Node node = lexer.GetToken(Lexer.IGNORE_WHITESPACE);
                    if (node == null)
                        break;
                    if (node.Tag == field.Tag && node.Type == Node.END_TAG)
                    {
                        field.Closed = true;
                        Node.TrimSpaces(lexer, field);
                        return;
                    }

                    /* deal with comments etc. */
                    if (Node.InsertMisc(field, node))
                        continue;

                    if (node.Type == Node.START_TAG &&
                        (node.Tag == tt.TagOption || node.Tag == tt.TagOptgroup || node.Tag == tt.TagScript))
                    {
                        Node.InsertNodeAtEnd(field, node);
                        ParseTag(lexer, node, Lexer.IGNORE_WHITESPACE);
                        continue;
                    }

                    /* discard unexpected tags */
                    Report.Warning(lexer, field, node, Report.DISCARDING_UNEXPECTED);
                }

                Report.Warning(lexer, field, null, Report.MISSING_ENDTAG_FOR);
            }
        }

        public class ParseTableTagCheckTable : IParser
        {
            public virtual void Parse(Lexer lexer, Node table, short mode)
            {
                int istackbase;
                TagCollection tt = lexer.Options.TagTable;

                lexer.DeferDup();
                istackbase = lexer.Istackbase;
                lexer.Istackbase = lexer.Istack.Count;

                while (true)
                {
                    Node node = lexer.GetToken(Lexer.IGNORE_WHITESPACE);
                    if (node == null)
                        break;
                    if (node.Tag == table.Tag && node.Type == Node.END_TAG)
                    {
                        lexer.Istackbase = istackbase;
                        table.Closed = true;
                        Node.TrimEmptyElement(lexer, table);
                        return;
                    }

                    /* deal with comments etc. */
                    if (Node.InsertMisc(table, node))
                        continue;

                    /* discard unknown tags */
                    if (node.Tag == null && node.Type != Node.TEXT_NODE)
                    {
                        Report.Warning(lexer, table, node, Report.DISCARDING_UNEXPECTED);
                        continue;
                    }

                    /* if TD or TH or text or inline or block then infer <TR> */

                    if (node.Type != Node.END_TAG)
                    {
                        if (node.Tag == tt.TagTd || node.Tag == tt.TagTh || node.Tag == tt.TagTable)
                        {
                            lexer.UngetToken();
                            node = lexer.InferredTag("tr");
                            Report.Warning(lexer, table, node, Report.MISSING_STARTTAG);
                        }
                        else if (node.Tag != null && (node.Type == Node.TEXT_NODE ||
                                                      (node.Tag.Model & (ContentModel.BLOCK | ContentModel.INLINE)) != 0))
                        {
                            Node.InsertNodeBeforeElement(table, node);
                            Report.Warning(lexer, table, node, Report.TAG_NOT_ALLOWED_IN);
                            lexer.Exiled = true;

                            /* AQ: TODO
							Line 2040 of parser.c (13 Jan 2000) reads as follows:
							if (!node->type == TextNode)
							This will always evaluate to false.
							This has been reported to Dave Raggett <dsr@w3.org>
							*/
                            //Should be?: if (!(node.Type == Node.TextNode))
//							if (false)
//								TidyNet.ParserImpl.parseTag(lexer, node, Lexer.IgnoreWhitespace);

                            lexer.Exiled = false;
                            continue;
                        }
                        else if (node.Tag != null && (node.Tag.Model & ContentModel.HEAD) != 0)
                        {
                            MoveToHead(lexer, table, node);
                            continue;
                        }
                    }

                    /* 
					if this is the end tag for an ancestor element
					then infer end tag for this element
					*/
                    if (node.Type == Node.END_TAG)
                    {
                        if (node.Tag == tt.TagForm)
                        {
                            lexer.BadForm = 1;
                            Report.Warning(lexer, table, node, Report.DISCARDING_UNEXPECTED);
                            continue;
                        }

                        if (node.Tag != null && (node.Tag.Model & (ContentModel.TABLE | ContentModel.ROW)) != 0)
                        {
                            Report.Warning(lexer, table, node, Report.DISCARDING_UNEXPECTED);
                            continue;
                        }

                        Node parent;
                        for (parent = table.Parent; parent != null; parent = parent.Parent)
                        {
                            if (node.Tag != parent.Tag) continue;
                            Report.Warning(lexer, table, node, Report.MISSING_ENDTAG_BEFORE);
                            lexer.UngetToken();
                            lexer.Istackbase = istackbase;
                            Node.TrimEmptyElement(lexer, table);
                            return;
                        }
                    }

                    if (node.Tag != null && (node.Tag.Model & ContentModel.TABLE) == 0)
                    {
                        lexer.UngetToken();
                        Report.Warning(lexer, table, node, Report.TAG_NOT_ALLOWED_IN);
                        lexer.Istackbase = istackbase;
                        Node.TrimEmptyElement(lexer, table);
                        return;
                    }

                    if (node.Type == Node.START_TAG || node.Type == Node.START_END_TAG)
                    {
                        Node.InsertNodeAtEnd(table, node);
                        ParseTag(lexer, node, Lexer.IGNORE_WHITESPACE);
                        continue;
                    }

                    /* discard unexpected text nodes and end tags */
                    Report.Warning(lexer, table, node, Report.DISCARDING_UNEXPECTED);
                }

                Report.Warning(lexer, table, null, Report.MISSING_ENDTAG_FOR);
                Node.TrimEmptyElement(lexer, table);
                lexer.Istackbase = istackbase;
            }
        }


        public class ParseTextCheckTable : IParser
        {
            public virtual void Parse(Lexer lexer, Node field, short mode)
            {
                TagCollection tt = lexer.Options.TagTable;

                lexer.Insert = - 1; /* defer implicit inline start tags */

                if (field.Tag == tt.TagTextarea)
                    mode = Lexer.PREFORMATTED;

                while (true)
                {
                    Node node = lexer.GetToken(mode);
                    if (node == null)
                        break;
                    if (node.Tag == field.Tag && node.Type == Node.END_TAG)
                    {
                        field.Closed = true;
                        Node.TrimSpaces(lexer, field);
                        return;
                    }

                    /* deal with comments etc. */
                    if (Node.InsertMisc(field, node))
                        continue;

                    if (node.Type == Node.TEXT_NODE)
                    {
                        /* only called for 1st child */
                        if (field.Content == null && (mode & Lexer.PREFORMATTED) == 0)
                            Node.TrimSpaces(lexer, field);

                        if (node.Start >= node.End)
                        {
                            continue;
                        }

                        Node.InsertNodeAtEnd(field, node);
                        continue;
                    }

                    if (node.Tag == tt.TagFont)
                    {
                        Report.Warning(lexer, field, node, Report.DISCARDING_UNEXPECTED);
                        continue;
                    }

                    /* terminate element on other tags */
                    if ((field.Tag.Model & ContentModel.OPT) == 0)
                        Report.Warning(lexer, field, node, Report.MISSING_ENDTAG_BEFORE);

                    lexer.UngetToken();
                    Node.TrimSpaces(lexer, field);
                    return;
                }

                if ((field.Tag.Model & ContentModel.OPT) == 0)
                    Report.Warning(lexer, field, null, Report.MISSING_ENDTAG_FOR);
            }
        }

        public class ParseTitleCheckTable : IParser
        {
            public virtual void Parse(Lexer lexer, Node title, short mode)
            {
                while (true)
                {
                    Node node = lexer.GetToken(Lexer.MIXED_CONTENT);
                    if (node == null)
                    {
                        break;
                    }
                    if (node.Tag == title.Tag && node.Type == Node.END_TAG)
                    {
                        title.Closed = true;
                        Node.TrimSpaces(lexer, title);
                        return;
                    }

                    if (node.Type == Node.TEXT_NODE)
                    {
                        /* only called for 1st child */
                        if (title.Content == null)
                        {
                            Node.TrimInitialSpace(lexer, title, node);
                        }

                        if (node.Start >= node.End)
                        {
                            continue;
                        }

                        Node.InsertNodeAtEnd(title, node);
                        continue;
                    }

                    /* deal with comments etc. */
                    if (Node.InsertMisc(title, node))
                    {
                        continue;
                    }

                    /* discard unknown tags */
                    if (node.Tag == null)
                    {
                        Report.Warning(lexer, title, node, Report.DISCARDING_UNEXPECTED);
                        continue;
                    }

                    /* pushback unexpected tokens */
                    Report.Warning(lexer, title, node, Report.MISSING_ENDTAG_BEFORE);
                    lexer.UngetToken();
                    Node.TrimSpaces(lexer, title);
                    return;
                }

                Report.Warning(lexer, title, null, Report.MISSING_ENDTAG_FOR);
            }
        }
    }
}