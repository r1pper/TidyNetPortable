using Tidy.Dom;

namespace Tidy.Core
{
    /// <summary>
    ///     DomDocumentTypeImpl
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
    /// <version>1.7, 1999/12/06 Tidy Release 30 Nov 1999</version>
    /// <version>1.8, 2000/01/22 Tidy Release 13 Jan 2000</version>
    /// <version>1.9, 2000/06/03 Tidy Release 30 Apr 2000</version>
    /// <version>1.10, 2000/07/22 Tidy Release 8 Jul 2000</version>
    /// <version>1.11, 2000/08/16 Tidy Release 4 Aug 2000</version>
    internal class DomDocumentTypeImpl : DomNodeImpl, IDocumentType
    {
        protected internal DomDocumentTypeImpl(Node adaptee) : base(adaptee)
        {
        }

        public override NodeType NodeType
        {
            get { return NodeType.DocumentTypeNode; }
        }

        public override string NodeName
        {
            get { return Name; }
        }

        public virtual string Name
        {
            get
            {
                string val = null;
                if (Adaptee.Type == Node.DOC_TYPE_TAG)
                {
                    if (Adaptee.Textarray != null && Adaptee.Start < Adaptee.End)
                    {
                        val = Lexer.GetString(Adaptee.Textarray, Adaptee.Start, Adaptee.End - Adaptee.Start);
                    }
                }
                return val;
            }
        }

        public virtual INamedNodeMap Entities
        {
            get
            {
                // NOT SUPPORTED
                return null;
            }
        }

        public virtual INamedNodeMap Notations
        {
            get
            {
                // NOT SUPPORTED
                return null;
            }
        }

        /// <summary> DOM2 - not implemented. </summary>
        public virtual string PublicId
        {
            get { return null; }
        }

        /// <summary> DOM2 - not implemented. </summary>
        public virtual string SystemId
        {
            get { return null; }
        }

        /// <summary> DOM2 - not implemented. </summary>
        public virtual string InternalSubset
        {
            get { return null; }
        }
    }
}