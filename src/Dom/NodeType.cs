namespace Tidy.Dom
{
    public enum NodeType : short
    {
        /// <summary>
        ///     The node is an <code>Element</code>.
        /// </summary>
        ElementNode = 1,

        /// <summary>
        ///     The node is an <code>Attr</code>.
        /// </summary>
        AttributeNode = 2,

        /// <summary>
        ///     The node is a <code>Text</code> node.
        /// </summary>
        TextNode = 3,

        /// <summary>
        ///     The node is a <code>CDATASection</code>.
        /// </summary>
        CdataSectionNode = 4,

        /// <summary>
        ///     The node is an <code>EntityReference</code>.
        /// </summary>
        EntityReferenceNode = 5,

        /// <summary>
        ///     The node is an <code>Entity</code>.
        /// </summary>
        EntityNode = 6,

        /// <summary>
        ///     The node is a <code>ProcessingInstruction</code>.
        /// </summary>
        ProcessingInstructionNode = 7,

        /// <summary>
        ///     The node is a <code>Comment</code>.
        /// </summary>
        CommentNode = 8,

        /// <summary>
        ///     The node is a <code>Document</code>.
        /// </summary>
        DocumentNode = 9,

        /// <summary>
        ///     The node is a <code>DocumentType</code>.
        /// </summary>
        DocumentTypeNode = 10,

        /// <summary>
        ///     The node is a <code>DocumentFragment</code>.
        /// </summary>
        DocumentFragmentNode = 11,

        /// <summary>
        ///     The node is a <code>Notation</code>.
        /// </summary>
        NotationNode = 12
    }
}