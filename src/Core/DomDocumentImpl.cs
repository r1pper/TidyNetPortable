using Tidy.Dom;

namespace Tidy.Core
{
    /// <summary>
    ///     DomDocumentImpl
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
    internal class DomDocumentImpl : DomNodeImpl, IDocument
    {
        private TagCollection _tt;

        protected internal DomDocumentImpl(Node adaptee) : base(adaptee)
        {
            _tt = new TagCollection();
        }

        public virtual TagCollection TagTable
        {
            set { _tt = value; }
        }

        public override string NodeName
        {
            get { return "#document"; }
        }

        public override NodeType NodeType
        {
            get { return NodeType.DocumentNode; }
        }

        public virtual IDocumentType Doctype
        {
            get
            {
                Node node = Adaptee.Content;
                while (node != null)
                {
                    if (node.Type == Node.DOC_TYPE_TAG)
                    {
                        break;
                    }
                    node = node.Next;
                }
                if (node != null)
                {
                    return (IDocumentType) node.Adapter;
                }
                return null;
            }
        }

        public virtual IDomImplementation Implementation
        {
            get
            {
                // NOT SUPPORTED
                return null;
            }
        }

        public virtual IElement DocumentElement
        {
            get
            {
                Node node = Adaptee.Content;
                while (node != null)
                {
                    if (node.Type == Node.START_TAG || node.Type == Node.START_END_TAG)
                    {
                        break;
                    }
                    node = node.Next;
                }
                return node != null ? (IElement) node.Adapter : null;
            }
        }

        public virtual IElement CreateElement(string tagName)
        {
            var node = new Node(Node.START_END_TAG, null, 0, 0, tagName, _tt);
            if (node.Tag == null)
                node.Tag = _tt.XmlTags;

            return (IElement) node.Adapter;
        }

        public virtual IDocumentFragment CreateDocumentFragment()
        {
            // NOT SUPPORTED
            return null;
        }

        public virtual IText CreateTextNode(string data)
        {
            byte[] textarray = Lexer.GetBytes(data);
            var node = new Node(Node.TEXT_NODE, textarray, 0, textarray.Length);
            return (IText) node.Adapter;
        }

        public virtual IComment CreateComment(string data)
        {
            byte[] textarray = Lexer.GetBytes(data);
            var node = new Node(Node.COMMENT_TAG, textarray, 0, textarray.Length);
            return (IComment) node.Adapter;
        }

        public virtual ICdataSection CreateCdataSection(string data)
        {
            // NOT SUPPORTED
            return null;
        }

        public virtual IProcessingInstruction CreateProcessingInstruction(string target, string data)
        {
            throw new DomException(DomException.NOT_SUPPORTED, "HTML document");
        }

        public virtual IAttr CreateAttribute(string name)
        {
            var av = new AttVal(null, null, '"', name, null);
            av.Dict = AttributeTable.DefaultAttributeTable.FindAttribute(av);
            return av.Adapter;
        }

        public virtual IEntityReference CreateEntityReference(string name)
        {
            // NOT SUPPORTED
            return null;
        }

        public virtual INodeList GetElementsByTagName(string tagname)
        {
            return new DomNodeListByTagNameImpl(Adaptee, tagname);
        }

        /// <summary> DOM2 - not implemented. </summary>
        public virtual INode ImportNode(INode importedNode, bool deep)
        {
            return null;
        }

        /// <summary> DOM2 - not implemented. </summary>
        public virtual IAttr CreateAttributeNs(string namespaceUri, string qualifiedName)
        {
            return null;
        }

        /// <summary> DOM2 - not implemented. </summary>
        public virtual IElement CreateElementNs(string namespaceUri, string qualifiedName)
        {
            return null;
        }

        /// <summary> DOM2 - not implemented. </summary>
        public virtual INodeList GetElementsByTagNameNs(string namespaceUri, string localName)
        {
            return null;
        }

        /// <summary> DOM2 - not implemented. </summary>
        public virtual IElement GetElementById(string elementId)
        {
            return null;
        }
    }
}