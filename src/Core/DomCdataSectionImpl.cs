using Tidy.Dom;

namespace Tidy.Core
{
    /// <summary>
    ///     DomCdataSectionImpl
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
    /// <author>Gary L Peskin &lt;garyp@firstech.com&gt;</author>
    /// <version>1.11, 2000/08/16 Tidy Release 4 Aug 2000</version>
    internal class DomCdataSectionImpl : DomTextImpl, ICdataSection
    {
        protected internal DomCdataSectionImpl(Node adaptee) : base(adaptee)
        {
        }

        public override string NodeName
        {
            get { return "#cdata-section"; }
        }

        public override NodeType NodeType
        {
            get { return NodeType.CdataSectionNode; }
        }
    }
}