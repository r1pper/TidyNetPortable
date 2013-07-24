using System;

/*
* Copyright (c) 2000 World Wide Web Consortium,
* (Massachusetts Institute of Technology, Institut National de
* Recherche en Informatique et en Automatique, Keio University). All
* Rights Reserved. This program is distributed under the W3C's Software
* Intellectual Property License. This program is distributed in the
* hope that it will be useful, but WITHOUT ANY WARRANTY; without even
* the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
* PURPOSE.
* See W3C License http://www.w3.org/Consortium/Legal/ for more details.
*/

namespace Tidy.Dom
{
    /// <summary>
    ///     DOM operations only raise exceptions in "exceptional" circumstances, i.e.,
    ///     when an operation is impossible to perform (either for logical reasons,
    ///     because data is lost, or because the implementation has become unstable).
    ///     In general, DOM methods return specific error values in ordinary
    ///     processing situations, such as out-of-bound errors when using
    ///     <code>NodeList</code>.
    ///     <p>
    ///         Implementations should raise other exceptions under other circumstances.
    ///         For example, implementations should raise an implementation-dependent
    ///         exception if a <code>null</code> argument is passed.
    ///     </p>
    ///     <p>
    ///         Some languages and object systems do not support the concept of
    ///         exceptions. For such systems, error conditions may be indicated using
    ///         native error reporting mechanisms. For some bindings, for example,
    ///         methods may return error codes similar to those listed in the
    ///         corresponding method descriptions.
    ///     </p>
    ///     <p>
    ///         See also the <a href='http://www.w3.org/TR/2000/REC-DOM-Level-2-Core-20001113'>Document Object Model (DOM) Level 2 Core Specification</a>.
    ///     </p>
    /// </summary>
    public class DomException : Exception
    {
        /// <summary>If index or size is negative, or greater than the allowed value</summary>
        public const short INDEX_SIZE = 1;

        /// <summary>If the specified range of text does not fit into a DOMString</summary>
        public const short DOM_STRING_SIZE = 2;

        /// <summary>If any node is inserted somewhere it doesn't belong</summary>
        public const short HIERARCHY_REQUEST = 3;

        /// <summary>
        ///     If a node is used in a different document than the one that created it
        ///     (that doesn't support it)
        /// </summary>
        public const short WRONG_DOCUMENT = 4;

        /// <summary>
        ///     If an invalid or illegal character is specified, such as in a name. See
        ///     production 2 in the XML specification for the definition of a legal
        ///     character, and production 5 for the definition of a legal name
        ///     character.
        /// </summary>
        public const short INVALID_CHARACTER = 5;

        /// <summary>If data is specified for a node which does not support data</summary>
        public const short NO_DATA_ALLOWED = 6;

        /// <summary>
        ///     If an attempt is made to modify an object where modifications are not
        ///     allowed
        /// </summary>
        public const short NO_MODIFICATION_ALLOWED = 7;

        /// <summary>
        ///     If an attempt is made to reference a node in a context where it does
        ///     not exist
        /// </summary>
        public const short NOT_FOUND = 8;

        /// <summary>
        ///     If the implementation does not support the requested type of object or
        ///     operation.
        /// </summary>
        public const short NOT_SUPPORTED = 9;

        /// <summary>
        ///     If an attempt is made to add an attribute that is already in use
        ///     elsewhere
        /// </summary>
        public const short IN_USE_ATTRIBUTE = 10;

        /// <summary>
        ///     If an attempt is made to use an object that is not, or is no longer,
        ///     usable.
        /// </summary>
        public const short INVALID_STATE = 11;

        /// <summary>If an invalid or illegal string is specified.</summary>
        public const short SYNTAX = 12;

        /// <summary>If an attempt is made to modify the type of the underlying object.</summary>
        public const short INVALID_MODIFICATION = 13;

        /// <summary>
        ///     If an attempt is made to create or change an object in a way which is
        ///     incorrect with regard to namespaces.
        /// </summary>
        public const short NAMESPACE = 14;

        /// <summary>
        ///     If a parameter or an operation is not supported by the underlying
        ///     object.
        /// </summary>
        public const short INVALID_ACCESS = 15;

        /// <summary>
        ///     Default constuctor.  Used only for serialisation.
        /// </summary>
        public DomException()
        {
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="message">The message</param>
        public DomException(short code, string message) : base(message)
        {
            Code = code;
        }

        /// <summary>
        ///     The error code.
        /// </summary>
        public short Code { get; set; }
    }
}