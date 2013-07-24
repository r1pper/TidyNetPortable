using System;
using System.Text;
using Tidy.Dom;

namespace Tidy.Core
{
    /// <summary>
    ///     Node
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
    ///     Used for elements and text nodes element name is null for text nodes
    ///     start and end are offsets into lexbuf which contains the textual content of
    ///     all elements in the parse tree.
    ///     parent and content allow traversal of the parse tree in any direction.
    ///     attributes are represented as a linked list of AttVal nodes which hold the
    ///     strings for attribute/value pairs.
    /// </remarks>
    internal class Node
    {
        public const short ROOT_NODE = 0;
        public const short DOC_TYPE_TAG = 1;
        public const short COMMENT_TAG = 2;
        public const short PROC_INS_TAG = 3;
        public const short TEXT_NODE = 4;
        public const short START_TAG = 5;
        public const short END_TAG = 6;
        public const short START_END_TAG = 7;
        public const short CDATA_TAG = 8;
        public const short SECTION_TAG = 9;
        public const short ASP_TAG = 10;
        public const short JSTE_TAG = 11;
        public const short PHP_TAG = 12;

        private static readonly string[] NodeTypeString = new[]
            {
                "RootNode", "DocTypeTag", "CommentTag", "ProcInsTag", "TextNode", "StartTag", "EndTag", "StartEndTag",
                "SectionTag", "AspTag", "PhpTag"
            };

        protected INode adapter = null;
        protected string element; /* name (null for text nodes) */
        protected short type; /* TextNode, StartTag, EndTag etc. */

        public Node() : this(TEXT_NODE, null, 0, 0)
        {
        }

        public Node(short type, byte[] textarray, int start, int end)
        {
            Parent = null;
            Prev = null;
            Next = null;
            Last = null;
            Start = start;
            End = end;
            Textarray = textarray;
            this.type = type;
            Closed = false;
            Isimplicit = false;
            Linebreak = false;
            Was = null;
            Tag = null;
            element = null;
            Attributes = null;
            Content = null;
        }

        public Node(short type, byte[] textarray, int start, int end, string element, TagCollection tt)
        {
            Parent = null;
            Prev = null;
            Next = null;
            Last = null;
            Start = start;
            End = end;
            Textarray = textarray;
            this.type = type;
            Closed = false;
            Isimplicit = false;
            Linebreak = false;
            Was = null;
            Tag = null;
            this.element = element;
            Attributes = null;
            Content = null;
            if (type == START_TAG || type == START_END_TAG || type == END_TAG)
            {
                tt.FindTag(this);
            }
        }

        public virtual bool IsElement
        {
            get { return (type == START_TAG || type == START_END_TAG); }
        }

        protected internal virtual string Element
        {
            get { return element; }
            set { element = value; }
        }

        protected internal virtual INode Adapter
        {
            get
            {
                if (adapter == null)
                {
                    switch (type)
                    {
                        case ROOT_NODE:
                            adapter = new DomDocumentImpl(this);
                            break;

                        case START_TAG:
                        case START_END_TAG:
                            adapter = new DomElementImpl(this);
                            break;

                        case DOC_TYPE_TAG:
                            adapter = new DomDocumentTypeImpl(this);
                            break;

                        case COMMENT_TAG:
                            adapter = new DomCommentImpl(this);
                            break;

                        case TEXT_NODE:
                            adapter = new DomTextImpl(this);
                            break;

                        case CDATA_TAG:
                            adapter = new DomCdataSectionImpl(this);
                            break;

                        case PROC_INS_TAG:
                            adapter = new DomProcessingInstructionImpl(this);
                            break;

                        default:
                            adapter = new DomNodeImpl(this);
                            break;
                    }
                }

                return adapter;
            }
        }

        protected internal virtual short Type
        {
            get { return type; }
            set { type = value; }
        }

        protected internal Node Parent { get; set; }

        protected internal Node Prev { get; set; }

        protected internal Node Next { get; set; }

        protected internal Node Last { get; set; }

        protected internal int Start { get; set; }

        protected internal int End { get; set; }

        protected internal byte[] Textarray { get; set; }

        protected internal bool Closed { get; set; }

        protected internal bool Isimplicit { get; set; }

        protected internal bool Linebreak { get; set; }

        protected internal Dict Was { get; set; }

        protected internal Dict Tag { get; set; }

        protected internal AttVal Attributes { get; set; }

        protected internal Node Content { get; set; }

        /* used to clone heading nodes when split by an <HR> */

        protected internal object Clone()
        {
            var node = new Node {Parent = Parent};

            if (Textarray != null)
            {
                node.Textarray = new byte[End - Start];
                node.Start = 0;
                node.End = End - Start;
                if (node.End > 0)
                {
                    Array.Copy(Textarray, Start, node.Textarray, node.Start, node.End);
                }
            }
            node.Type = type;
            node.Closed = Closed;
            node.Isimplicit = Isimplicit;
            node.Linebreak = Linebreak;
            node.Was = Was;
            node.Tag = Tag;
            if (element != null)
            {
                node.Element = element;
            }
            if (Attributes != null)
            {
                node.Attributes = (AttVal) Attributes.Clone();
            }
            return node;
        }

        public virtual AttVal GetAttrByName(string name)
        {
            AttVal attr;

            for (attr = Attributes; attr != null; attr = attr.Next)
            {
                if (name != null && attr.Attribute != null && attr.Attribute.Equals(name))
                {
                    break;
                }
            }

            return attr;
        }

        /* default method for checking an element's attributes */

        public virtual void CheckAttributes(Lexer lexer)
        {
            AttVal attval;

            for (attval = Attributes; attval != null; attval = attval.Next)
            {
                attval.CheckAttribute(lexer, this);
            }
        }

        public virtual void CheckUniqueAttributes(Lexer lexer)
        {
            AttVal attval;

            for (attval = Attributes; attval != null; attval = attval.Next)
            {
                if (attval.Asp == null && attval.Php == null)
                {
                    attval.CheckUniqueAttribute(lexer, this);
                }
            }
        }

        public virtual void AddAttribute(string name, string val)
        {
            var av = new AttVal(null, null, null, null, '"', name, val);
            av.Dict = AttributeTable.DefaultAttributeTable.FindAttribute(av);

            if (Attributes == null)
            {
                Attributes = av;
                /* append to end of attributes */
            }
            else
            {
                AttVal here = Attributes;

                while (here.Next != null)
                {
                    here = here.Next;
                }

                here.Next = av;
            }
        }

        /* remove attribute from node then free it */

        public virtual void RemoveAttribute(AttVal attr)
        {
            AttVal av;
            AttVal prev = null;
            AttVal next;

            for (av = Attributes; av != null; av = next)
            {
                next = av.Next;

                if (av == attr)
                {
                    if (prev != null)
                    {
                        prev.Next = next;
                    }
                    else
                    {
                        Attributes = next;
                    }
                }
                else
                {
                    prev = av;
                }
            }
        }

        /* find doctype element */

        public virtual Node FindDocType()
        {
            Node node;

            //TODO:odd!
            for (node = Content; node != null && node.Type != DOC_TYPE_TAG; node = node.Next)
            {
            }

            return node;
        }

        public virtual void DiscardDocType()
        {
            Node node = FindDocType();
            if (node == null) return;
            if (node.Prev != null)
            {
                node.Prev.Next = node.Next;
            }
            else
            {
                node.Parent.Content = node.Next;
            }

            if (node.Next != null)
            {
                node.Next.Prev = node.Prev;
            }

            node.Next = null;
        }

        /* remove node from markup tree and discard it */

        public static Node DiscardElement(Node element)
        {
            Node next = null;

            if (element != null)
            {
                next = element.Next;
                RemoveNode(element);
            }

            return next;
        }

        /* insert node into markup tree */

        public static void InsertNodeAtStart(Node element, Node node)
        {
            node.Parent = element;

            if (element.Content == null)
            {
                element.Last = node;
            }
            else
            {
                element.Content.Prev = node;
            }
            // AQ added 13 Apr 2000

            node.Next = element.Content;
            node.Prev = null;
            element.Content = node;
        }

        /* insert node into markup tree */

        public static void InsertNodeAtEnd(Node element, Node node)
        {
            node.Parent = element;
            node.Prev = element.Last;

            if (element.Last != null)
            {
                element.Last.Next = node;
            }
            else
            {
                element.Content = node;
            }

            element.Last = node;
        }

        /*
		insert node into markup tree in pace of element
		which is moved to become the child of the node
		*/

        public static void InsertNodeAsParent(Node element, Node node)
        {
            node.Content = element;
            node.Last = element;
            node.Parent = element.Parent;
            element.Parent = node;

            if (node.Parent.Content == element)
            {
                node.Parent.Content = node;
            }

            if (node.Parent.Last == element)
            {
                node.Parent.Last = node;
            }

            node.Prev = element.Prev;
            element.Prev = null;

            if (node.Prev != null)
            {
                node.Prev.Next = node;
            }

            node.Next = element.Next;
            element.Next = null;

            if (node.Next != null)
            {
                node.Next.Prev = node;
            }
        }

        /* insert node into markup tree before element */

        public static void InsertNodeBeforeElement(Node element, Node node)
        {
            Node parent = element.Parent;
            node.Parent = parent;
            node.Next = element;
            node.Prev = element.Prev;
            element.Prev = node;

            if (node.Prev != null)
            {
                node.Prev.Next = node;
            }

            if (parent.Content == element)
            {
                parent.Content = node;
            }
        }

        /* insert node into markup tree after element */

        public static void InsertNodeAfterElement(Node element, Node node)
        {
            Node parent = element.Parent;
            node.Parent = parent;

            // AQ - 13Jan2000 fix for parent == null
            if (parent != null && parent.Last == element)
            {
                parent.Last = node;
            }
            else
            {
                node.Next = element.Next;
                // AQ - 13Jan2000 fix for node.next == null
                if (node.Next != null)
                {
                    node.Next.Prev = node;
                }
            }

            element.Next = node;
            node.Prev = element;
        }

        public static void TrimEmptyElement(Lexer lexer, Node element)
        {
            TagCollection tt = lexer.Options.TagTable;

            if (lexer.CanPrune(element))
            {
                if (element.Type != TEXT_NODE)
                {
                    Report.Warning(lexer, element, null, Report.TRIM_EMPTY_ELEMENT);
                }

                DiscardElement(element);
            }
            else if (element.Tag == tt.TagP && element.Content == null)
            {
                /* replace <p></p> by <br><br> to preserve formatting */
                Node node = lexer.InferredTag("br");
                CoerceNode(lexer, element, tt.TagBr);
                InsertNodeAfterElement(element, node);
            }
        }

        /*
		This maps 
		<em>hello </em><strong>world</strong>
		to
		<em>hello</em> <strong>world</strong>
		
		If last child of element is a text node
		then trim trailing white space character
		moving it to after element's end tag.
		*/

        public static void TrimTrailingSpace(Lexer lexer, Node element, Node last)
        {
            TagCollection tt = lexer.Options.TagTable;

            if (last == null || last.Type != TEXT_NODE || last.End <= last.Start) return;
            byte c = lexer.Lexbuf[last.End - 1];

            if (c != 160 && c != (byte) ' ') return;
            /* take care with <td>&nbsp;</td> */
            if (element.Tag == tt.TagTd || element.Tag == tt.TagTh)
            {
                if (last.End > last.Start + 1)
                {
                    last.End -= 1;
                }
            }
            else
            {
                last.End -= 1;

                if (((element.Tag.Model & ContentModel.INLINE) != 0) &&
                    (element.Tag.Model & ContentModel.FIELD) == 0)
                {
                    lexer.Insertspace = true;
                }

                /* if empty string then delete from parse tree */
                if (last.Start == last.End)
                {
                    TrimEmptyElement(lexer, last);
                }
            }
        }

        /*
		This maps 
		<p>hello<em> world</em>
		to
		<p>hello <em>world</em>
		
		Trims initial space, by moving it before the
		start tag, or if this element is the first in
		parent's content, then by discarding the space
		*/

        public static void TrimInitialSpace(Lexer lexer, Node element, Node text)
        {
            // GLP: Local fix to Bug 119789. Remove this comment when parser.c is updated.
            //      31-Oct-00. 
            if (text.Type == TEXT_NODE && text.Textarray[text.Start] == (byte) ' ' && (text.Start < text.End))
            {
                if (((element.Tag.Model & ContentModel.INLINE) != 0) && (element.Tag.Model & ContentModel.FIELD) == 0 &&
                    element.Parent.Content != element)
                {
                    Node prev = element.Prev;

                    if (prev != null && prev.Type == TEXT_NODE)
                    {
                        if (prev.Textarray[prev.End - 1] != (byte) ' ')
                        {
                            prev.Textarray[prev.End++] = (byte) ' ';
                        }

                        ++element.Start;
                    }
                        /* create new node */
                    else
                    {
                        Node node = lexer.NewNode();
                        // Local fix for bug 228486 (GLP).  This handles the case
                        // where we need to create a preceeding text node but there are
                        // no "slots" in textarray that we can steal from the current
                        // element.  Therefore, we create a new textarray containing
                        // just the blank.  When Tidy is fixed, this should be removed.
                        if (element.Start >= element.End)
                        {
                            node.Start = 0;
                            node.End = 1;
                            node.Textarray = new byte[1];
                        }
                        else
                        {
                            node.Start = element.Start++;
                            node.End = element.Start;
                            node.Textarray = element.Textarray;
                        }
                        node.Textarray[node.Start] = (byte) ' ';
                        node.Prev = prev;
                        if (prev != null)
                        {
                            prev.Next = node;
                        }
                        node.Next = element;
                        element.Prev = node;
                        node.Parent = element.Parent;
                    }
                }

                /* discard the space  in current node */
                ++text.Start;
            }
        }

        /* 
		Move initial and trailing space out.
		This routine maps:
		
		hello<em> world</em>
		to
		hello <em>world</em>
		and
		<em>hello </em><strong>world</strong>
		to
		<em>hello</em> <strong>world</strong>
		*/

        public static void TrimSpaces(Lexer lexer, Node element)
        {
            Node text = element.Content;
            TagCollection tt = lexer.Options.TagTable;

            if (text != null && text.Type == TEXT_NODE && element.Tag != tt.TagPre)
            {
                TrimInitialSpace(lexer, element, text);
            }

            text = element.Last;

            if (text != null && text.Type == TEXT_NODE)
            {
                TrimTrailingSpace(lexer, element, text);
            }
        }

        public virtual bool IsDescendantOf(Dict tag)
        {
            Node parent;

            for (parent = Parent; parent != null; parent = parent.Parent)
            {
                if (parent.Tag == tag)
                {
                    return true;
                }
            }

            return false;
        }

        /*
		the doctype has been found after other tags,
		and needs moving to before the html element
		*/

        public static void InsertDocType(Lexer lexer, Node element, Node doctype)
        {
            TagCollection tt = lexer.Options.TagTable;

            Report.Warning(lexer, element, doctype, Report.DOCTYPE_AFTER_TAGS);

            while (element.Tag != tt.TagHtml)
            {
                element = element.Parent;
            }

            InsertNodeBeforeElement(element, doctype);
        }

        public virtual Node FindBody(TagCollection tt)
        {
            Node node = Content;

            while (node != null && node.Tag != tt.TagHtml)
            {
                node = node.Next;
            }

            if (node == null)
            {
                return null;
            }

            node = node.Content;

            while (node != null && node.Tag != tt.TagBody)
            {
                node = node.Next;
            }

            return node;
        }


        /*
		unexpected content in table row is moved to just before
		the table in accordance with Netscape and IE. This code
		assumes that node hasn't been inserted into the row.
		*/

        public static void MoveBeforeTable(Node row, Node node, TagCollection tt)
        {
            Node table;

            /* first find the table element */
            for (table = row.Parent; table != null; table = table.Parent)
            {
                if (table.Tag == tt.TagTable)
                {
                    if (table.Parent.Content == table)
                    {
                        table.Parent.Content = node;
                    }

                    node.Prev = table.Prev;
                    node.Next = table;
                    table.Prev = node;
                    node.Parent = table.Parent;

                    if (node.Prev != null)
                    {
                        node.Prev.Next = node;
                    }

                    break;
                }
            }
        }

        /*
		if a table row is empty then insert an empty cell
		this practice is consistent with browser behavior
		and avoids potential problems with row spanning cells
		*/

        public static void FixEmptyRow(Lexer lexer, Node row)
        {
            if (row.Content != null) return;
            Node cell = lexer.InferredTag("td");
            InsertNodeAtEnd(row, cell);
            Report.Warning(lexer, row, cell, Report.MISSING_STARTTAG);
        }

        public static void CoerceNode(Lexer lexer, Node node, Dict tag)
        {
            Node tmp = lexer.InferredTag(tag.Name);
            Report.Warning(lexer, node, tmp, Report.OBSOLETE_ELEMENT);
            node.Was = node.Tag;
            node.Tag = tag;
            node.Type = START_TAG;
            node.Isimplicit = true;
            node.Element = tag.Name;
        }

        /* extract a node and its children from a markup tree */

        public static void RemoveNode(Node node)
        {
            if (node.Prev != null)
            {
                node.Prev.Next = node.Next;
            }

            if (node.Next != null)
            {
                node.Next.Prev = node.Prev;
            }

            if (node.Parent != null)
            {
                if (node.Parent.Content == node)
                {
                    node.Parent.Content = node.Next;
                }

                if (node.Parent.Last == node)
                {
                    node.Parent.Last = node.Prev;
                }
            }

            node.Parent = node.Prev = node.Next = null;
        }

        public static bool InsertMisc(Node element, Node node)
        {
            if (node.Type == COMMENT_TAG || node.Type == PROC_INS_TAG || node.Type == CDATA_TAG ||
                node.Type == SECTION_TAG ||
                node.Type == ASP_TAG || node.Type == JSTE_TAG || node.Type == PHP_TAG)
            {
                InsertNodeAtEnd(element, node);
                return true;
            }

            return false;
        }

        /*
		used to determine how attributes
		without values should be printed
		this was introduced to deal with
		user defined tags e.g. Cold Fusion
		*/

        public static bool IsNewNode(Node node)
        {
            if (node != null && node.Tag != null)
            {
                return ((node.Tag.Model & ContentModel.NEW) != 0);
            }

            return true;
        }

        public virtual bool HasOneChild()
        {
            return (Content != null && Content.Next == null);
        }

        /* find html element */

        public virtual Node FindHtml(TagCollection tt)
        {
            Node node;

            //TODO:odd!
            for (node = Content; node != null && node.Tag != tt.TagHtml; node = node.Next)
            {
            }

            return node;
        }

        public virtual Node FindHead(TagCollection tt)
        {
            Node node = FindHtml(tt);

            if (node != null)
            {
                //TODO:odd!
                for (node = node.Content; node != null && node.Tag != tt.TagHead; node = node.Next)
                {
                }
            }

            return node;
        }

        public virtual bool CheckNodeIntegrity()
        {
            Node child;
            bool found = false;

            if (Prev != null)
            {
                if (Prev.Next != this)
                {
                    return false;
                }
            }

            if (Next != null)
            {
                if (Next.Prev != this)
                {
                    return false;
                }
            }

            if (Parent != null)
            {
                if (Prev == null && Parent.Content != this)
                {
                    return false;
                }

                if (Next == null && Parent.Last != this)
                {
                    return false;
                }

                for (child = Parent.Content; child != null; child = child.Next)
                {
                    if (child == this)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            for (child = Content; child != null; child = child.Next)
            {
                if (!child.CheckNodeIntegrity())
                {
                    return false;
                }
            }

            return true;
        }

        public static void AddClass(Node node, string classname)
        {
            AttVal classattr = node.GetAttrByName("class");

            /*
			if there already is a class attribute
			then append class name after a space
			*/
            if (classattr != null)
            {
                classattr.Val = classattr.Val + " " + classname;
            }
                /* create new class attribute */
            else
            {
                node.AddAttribute("class", classname);
            }
        }

        public override string ToString()
        {
            var s = new StringBuilder();
            Node n = this;

            while (n != null)
            {
                s.AppendFormat("[Node type={0},element=", NodeTypeString[n.Type]);
                s.Append(n.Element ?? "null");
                if (n.Type == TEXT_NODE || n.Type == COMMENT_TAG || n.Type == PROC_INS_TAG)
                {
                    s.Append(",text=");
                    if (n.Textarray != null && n.Start <= n.End)
                    {
                        s.AppendFormat("\"{0}\"", Lexer.GetString(n.Textarray, n.Start, n.End - n.Start));
                    }
                    else
                    {
                        s.Append("null");
                    }
                }
                s.Append(",content=");
                if (n.Content != null)
                {
                    s.Append(n.Content);
                }
                else
                {
                    s.Append("null");
                }
                s.Append("]");
                if (n.Next != null)
                {
                    s.Append(",");
                }
                n = n.Next;
            }

            return s.ToString();
        }

        protected internal virtual Node CloneNode(bool deep)
        {
            var node = (Node) Clone();
            if (deep)
            {
                Node child;
                for (child = Content; child != null; child = child.Next)
                {
                    Node newChild = child.CloneNode(true);
                    InsertNodeAtEnd(node, newChild);
                }
            }
            return node;
        }
    }
}