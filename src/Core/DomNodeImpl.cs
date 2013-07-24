using System;
using Tidy.Dom;

namespace Tidy.Core
{
    /// <summary>
    ///     DomNodeImpl
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
    /// <version>1.4, 1999/09/04 DOM support</version>
    /// <version>1.5, 1999/10/23 Tidy Release 27 Sep 1999</version>
    /// <version>1.6, 1999/11/01 Tidy Release 22 Oct 1999</version>
    /// <version>1.7, 1999/12/06 Tidy Release 30 Nov 1999</version>
    /// <version>1.8, 2000/01/22 Tidy Release 13 Jan 2000</version>
    /// <version>1.9, 2000/06/03 Tidy Release 30 Apr 2000</version>
    /// <version>1.10, 2000/07/22 Tidy Release 8 Jul 2000</version>
    /// <version>1.11, 2000/08/16 Tidy Release 4 Aug 2000</version>
    internal class DomNodeImpl : INode
    {
        protected internal DomNodeImpl(Node adaptee)
        {
            Adaptee = adaptee;
        }

        public Node Adaptee { get; set; }

        public virtual string NodeValue
        {
            get
            {
                string val = String.Empty; //BAK 10/10/2000 replaced null
                if (Adaptee.Type == Node.TEXT_NODE || Adaptee.Type == Node.CDATA_TAG || Adaptee.Type == Node.COMMENT_TAG ||
                    Adaptee.Type == Node.PROC_INS_TAG)
                {
                    if (Adaptee.Textarray != null && Adaptee.Start < Adaptee.End)
                    {
                        val = Lexer.GetString(Adaptee.Textarray, Adaptee.Start, Adaptee.End - Adaptee.Start);
                    }
                }
                return val;
            }

            set
            {
                if (Adaptee.Type == Node.TEXT_NODE || Adaptee.Type == Node.CDATA_TAG || Adaptee.Type == Node.COMMENT_TAG ||
                    Adaptee.Type == Node.PROC_INS_TAG)
                {
                    byte[] textarray = Lexer.GetBytes(value);
                    Adaptee.Textarray = textarray;
                    Adaptee.Start = 0;
                    Adaptee.End = textarray.Length;
                }
            }
        }

        public virtual string NodeName
        {
            get { return Adaptee.Element; }
        }

        public virtual NodeType NodeType
        {
            get
            {
                switch (Adaptee.Type)
                {
                    case Node.ROOT_NODE:
                        return NodeType.DocumentNode;

                    case Node.DOC_TYPE_TAG:
                        return NodeType.DocumentTypeNode;

                    case Node.COMMENT_TAG:
                        return NodeType.CommentNode;

                    case Node.PROC_INS_TAG:
                        return NodeType.ProcessingInstructionNode;

                    case Node.TEXT_NODE:
                        return NodeType.TextNode;

                    case Node.CDATA_TAG:
                        return NodeType.CdataSectionNode;

                    case Node.START_TAG:
                    case Node.START_END_TAG:
                        return NodeType.ElementNode;

                    default:
                        return NodeType.ElementNode;
                }
            }
        }

        public virtual INode ParentNode
        {
            get { return Adaptee.Parent != null ? Adaptee.Parent.Adapter : null; }
        }

        public virtual INodeList ChildNodes
        {
            get { return new DomNodeListImpl(Adaptee); }
        }

        public virtual INode FirstChild
        {
            get { return Adaptee.Content != null ? Adaptee.Content.Adapter : null; }
        }

        public virtual INode LastChild
        {
            get { return Adaptee.Last != null ? Adaptee.Last.Adapter : null; }
        }

        public virtual INode PreviousSibling
        {
            get { return Adaptee.Prev != null ? Adaptee.Prev.Adapter : null; }
        }

        public virtual INode NextSibling
        {
            get { return Adaptee.Next != null ? Adaptee.Next.Adapter : null; }
        }

        public virtual INamedNodeMap Attributes
        {
            get { return new DomAttrMapImpl(Adaptee.Attributes); }
        }

        public virtual IDocument OwnerDocument
        {
            get
            {
                Node node = Adaptee;
                if (node != null && node.Type == Node.ROOT_NODE)
                    return null;

                //TODO: odd!
                for (node = Adaptee; node != null && node.Type != Node.ROOT_NODE; node = node.Parent)
                {
                }

                if (node != null)
                {
                    return (IDocument) node.Adapter;
                }
                return null;
            }
        }

        /// <summary> DOM2 - not implemented.</summary>
        public virtual string NamespaceUri
        {
            get { return null; }
        }

        /// <summary> DOM2 - not implemented.</summary>
        public virtual string Prefix
        {
            get { return null; }
            set { }
        }

        /// <summary> DOM2 - not implemented.</summary>
        public virtual string LocalName
        {
            get { return null; }
        }

        public virtual INode InsertBefore(INode newChild, INode refChild)
        {
            // TODO - handle newChild already in tree

            if (newChild == null)
            {
                return null;
            }
            if (!(newChild is DomNodeImpl))
            {
                throw new DomException(DomException.WRONG_DOCUMENT, "newChild not instanceof DomNodeImpl");
            }
            var newCh = (DomNodeImpl) newChild;

            if (Adaptee.Type == Node.ROOT_NODE)
            {
                if (newCh.Adaptee.Type != Node.DOC_TYPE_TAG && newCh.Adaptee.Type != Node.PROC_INS_TAG)
                {
                    throw new DomException(DomException.HIERARCHY_REQUEST, "newChild cannot be a child of this node");
                }
            }
            else if (Adaptee.Type == Node.START_TAG)
            {
                if (newCh.Adaptee.Type != Node.START_TAG && newCh.Adaptee.Type != Node.START_END_TAG &&
                    newCh.Adaptee.Type != Node.COMMENT_TAG && newCh.Adaptee.Type != Node.TEXT_NODE &&
                    newCh.Adaptee.Type != Node.CDATA_TAG)
                {
                    throw new DomException(DomException.HIERARCHY_REQUEST, "newChild cannot be a child of this node");
                }
            }
            if (refChild == null)
            {
                Node.InsertNodeAtEnd(Adaptee, newCh.Adaptee);
                if (Adaptee.Type == Node.START_END_TAG)
                {
                    Adaptee.Type = Node.START_TAG;
                }
            }
            else
            {
                Node refNode = Adaptee.Content;
                while (refNode != null)
                {
                    if (refNode.Adapter == refChild)
                    {
                        break;
                    }
                    refNode = refNode.Next;
                }
                if (refNode == null)
                {
                    throw new DomException(DomException.NOT_FOUND, "refChild not found");
                }
                Node.InsertNodeBeforeElement(refNode, newCh.Adaptee);
            }
            return newChild;
        }

        public virtual INode ReplaceChild(INode newChild, INode oldChild)
        {
            // TODO - handle newChild already in tree

            if (newChild == null)
            {
                return null;
            }
            if (!(newChild is DomNodeImpl))
            {
                throw new DomException(DomException.WRONG_DOCUMENT, "newChild not instanceof DomNodeImpl");
            }
            var newCh = (DomNodeImpl) newChild;

            if (Adaptee.Type == Node.ROOT_NODE)
            {
                if (newCh.Adaptee.Type != Node.DOC_TYPE_TAG && newCh.Adaptee.Type != Node.PROC_INS_TAG)
                {
                    throw new DomException(DomException.HIERARCHY_REQUEST, "newChild cannot be a child of this node");
                }
            }
            else if (Adaptee.Type == Node.START_TAG)
            {
                if (newCh.Adaptee.Type != Node.START_TAG && newCh.Adaptee.Type != Node.START_END_TAG &&
                    newCh.Adaptee.Type != Node.COMMENT_TAG && newCh.Adaptee.Type != Node.TEXT_NODE &&
                    newCh.Adaptee.Type != Node.CDATA_TAG)
                {
                    throw new DomException(DomException.HIERARCHY_REQUEST, "newChild cannot be a child of this node");
                }
            }
            if (oldChild == null)
            {
                throw new DomException(DomException.NOT_FOUND, "oldChild not found");
            }
            Node n;
            Node refNode = Adaptee.Content;
            while (refNode != null)
            {
                if (refNode.Adapter == oldChild)
                {
                    break;
                }
                refNode = refNode.Next;
            }
            if (refNode == null)
            {
                throw new DomException(DomException.NOT_FOUND, "oldChild not found");
            }
            newCh.Adaptee.Next = refNode.Next;
            newCh.Adaptee.Prev = refNode.Prev;
            newCh.Adaptee.Last = refNode.Last;
            newCh.Adaptee.Parent = refNode.Parent;
            newCh.Adaptee.Content = refNode.Content;
            if (refNode.Parent != null)
            {
                if (refNode.Parent.Content == refNode)
                {
                    refNode.Parent.Content = newCh.Adaptee;
                }
                if (refNode.Parent.Last == refNode)
                {
                    refNode.Parent.Last = newCh.Adaptee;
                }
            }
            if (refNode.Prev != null)
            {
                refNode.Prev.Next = newCh.Adaptee;
            }
            if (refNode.Next != null)
            {
                refNode.Next.Prev = newCh.Adaptee;
            }
            for (n = refNode.Content; n != null; n = n.Next)
            {
                if (n.Parent == refNode)
                {
                    n.Parent = newCh.Adaptee;
                }
            }
            return oldChild;
        }

        public virtual INode RemoveChild(INode oldChild)
        {
            if (oldChild == null)
            {
                return null;
            }

            Node refNode = Adaptee.Content;
            while (refNode != null)
            {
                if (refNode.Adapter == oldChild)
                {
                    break;
                }
                refNode = refNode.Next;
            }
            if (refNode == null)
            {
                throw new DomException(DomException.NOT_FOUND, "refChild not found");
            }
            Node.DiscardElement(refNode);

            if (Adaptee.Content == null && Adaptee.Type == Node.START_TAG)
            {
                Adaptee.Type = Node.START_END_TAG;
            }

            return oldChild;
        }

        public virtual INode AppendChild(INode newChild)
        {
            // TODO - handle newChild already in tree

            if (newChild == null)
            {
                return null;
            }

            if (!(newChild is DomNodeImpl))
            {
                throw new DomException(DomException.WRONG_DOCUMENT, "newChild not instanceof DomNodeImpl");
            }

            var newCh = (DomNodeImpl) newChild;

            if (Adaptee.Type == Node.ROOT_NODE)
            {
                if (newCh.Adaptee.Type != Node.DOC_TYPE_TAG && newCh.Adaptee.Type != Node.PROC_INS_TAG)
                {
                    throw new DomException(DomException.HIERARCHY_REQUEST, "newChild cannot be a child of this node");
                }
            }
            else if (Adaptee.Type == Node.START_TAG)
            {
                if (newCh.Adaptee.Type != Node.START_TAG && newCh.Adaptee.Type != Node.START_END_TAG &&
                    newCh.Adaptee.Type != Node.COMMENT_TAG && newCh.Adaptee.Type != Node.TEXT_NODE &&
                    newCh.Adaptee.Type != Node.CDATA_TAG)
                {
                    throw new DomException(DomException.HIERARCHY_REQUEST, "newChild cannot be a child of this node");
                }
            }

            Node.InsertNodeAtEnd(Adaptee, newCh.Adaptee);

            if (Adaptee.Type == Node.START_END_TAG)
            {
                Adaptee.Type = Node.START_TAG;
            }

            return newChild;
        }

        public virtual bool HasChildNodes()
        {
            return (Adaptee.Content != null);
        }

        public virtual INode CloneNode(bool deep)
        {
            Node node = Adaptee.CloneNode(deep);
            node.Parent = null;
            return node.Adapter;
        }

        /// <summary> DOM2 - not implemented.</summary>
        public virtual void Normalize()
        {
        }

        /// <summary> DOM2 - not implemented.</summary>
        public virtual bool IsSupported(string feature, string version)
        {
            return false;
        }

        public virtual bool HasAttributes()
        {
            return Adaptee.Attributes != null;
        }

        /// <summary> DOM2 - not implemented.</summary>
        public virtual bool Supports(string feature, string version)
        {
            return IsSupported(feature, version);
        }
    }
}