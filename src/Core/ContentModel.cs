namespace Tidy.Core
{
    /// <summary>
    ///     Content Model enum.
    ///     (c) 1998-2000 (W3C) MIT, INRIA, Keio University
    ///     See Tidy.cs for the copyright notice.
    ///     Derived from
    ///     <a href="http://www.w3.org/People/Raggett/tidy">
    ///         HTML Tidy Release 4 Aug 2000
    ///     </a>
    /// </summary>
    /// <author>Seth Yates &lt;seth_yates@hotmail.com&gt; (translation to C#)</author>
    internal sealed class ContentModel
    {
        /* content model shortcut encoding */
        public const int UNKNOWN = 0;
        public const int EMPTY = (1 << 0);
        public const int HTML = (1 << 1);
        public const int HEAD = (1 << 2);
        public const int BLOCK = (1 << 3);
        public const int INLINE = (1 << 4);
        public const int LIST = (1 << 5);
        public const int DEFLIST = (1 << 6);
        public const int TABLE = (1 << 7);
        public const int ROWGRP = (1 << 8);
        public const int ROW = (1 << 9);
        public const int FIELD = (1 << 10);
        public const int OBJECT = (1 << 11);
        public const int PARAM = (1 << 12);
        public const int FRAMES = (1 << 13);
        public const int HEADING = (1 << 14);
        public const int OPT = (1 << 15);
        public const int IMG = (1 << 16);
        public const int MIXED = (1 << 17);
        public const int NO_INDENT = (1 << 18);
        public const int OBSOLETE = (1 << 19);
        public const int NEW = (1 << 20);
        public const int OMIT_ST = (1 << 21);
    }
}