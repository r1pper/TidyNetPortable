using System;
using System.Diagnostics;
using System.IO;

namespace Tidy.Core
{
    /// <summary>
    ///     Pretty print parse tree
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
    ///     Block-level and unknown elements are printed on
    ///     new lines and their contents indented 2 spaces
    ///     Inline elements are printed inline.
    ///     Inline content is wrapped on spaces (except in
    ///     attribute values or preformatted text, after
    ///     start tags and before end tags
    /// </remarks>
    internal class PPrint
    {
        public const short EFFECT_BLEND = - 1;
        public const short EFFECT_BOX_IN = 0;
        public const short EFFECT_BOX_OUT = 1;
        public const short EFFECT_CIRCLE_IN = 2;
        public const short EFFECT_CIRCLE_OUT = 3;
        public const short EFFECT_WIPE_UP = 4;
        public const short EFFECT_WIPE_DOWN = 5;
        public const short EFFECT_WIPE_RIGHT = 6;
        public const short EFFECT_WIPE_LEFT = 7;
        public const short EFFECT_VERT_BLINDS = 8;
        public const short EFFECT_HORZ_BLINDS = 9;
        public const short EFFECT_CHK_ACROSS = 10;
        public const short EFFECT_CHK_DOWN = 11;
        public const short EFFECT_RND_DISSOLVE = 12;
        public const short EFFECT_SPLIT_VIRT_IN = 13;
        public const short EFFECT_SPLIT_VIRT_OUT = 14;
        public const short EFFECT_SPLIT_HORZ_IN = 15;
        public const short EFFECT_SPLIT_HORZ_OUT = 16;
        public const short EFFECT_STRIPS_LEFT_DOWN = 17;
        public const short EFFECT_STRIPS_LEFT_UP = 18;
        public const short EFFECT_STRIPS_RIGHT_DOWN = 19;
        public const short EFFECT_STRIPS_RIGHT_UP = 20;
        public const short EFFECT_RND_BARS_HORZ = 21;
        public const short EFFECT_RND_BARS_VERT = 22;
        public const short EFFECT_RANDOM = 23;
        private const int NORMAL = 0;
        private const int PREFORMATTED = 1;
        private const int COMMENT = 2;
        private const int ATTRIBVALUE = 4;
        private const int NOWRAP = 8;
        private const int CDATA = 16;
        private readonly TidyOptions _options;
        private int _count;
        private bool _inAttVal;
        private bool _inString;
        private int _lbufsize;
        private int[] _linebuf;
        private int _linelen;
        private int _slide;
        private Node _slidecontent;
        private int _wraphere;

        public PPrint(TidyOptions options)
        {
            _options = options;
        }

        /*
		1010  A
		1011  B
		1100  C
		1101  D
		1110  E
		1111  F
		*/

        /* return one less that the number of bytes used by UTF-8 char */
        /* str points to 1st byte, *ch initialized to 1st byte */

        public static int GetUtf8(byte[] str, int start, MutableInteger ch)
        {
            int c, n, i, bytes;

            c = (str[start]) & 0xFF; // Convert to unsigned.

            if ((c & 0xE0) == 0xC0)
            {
                /* 110X XXXX  two bytes */
                n = c & 31;
                bytes = 2;
            }
            else if ((c & 0xF0) == 0xE0)
            {
                /* 1110 XXXX  three bytes */
                n = c & 15;
                bytes = 3;
            }
            else if ((c & 0xF8) == 0xF0)
            {
                /* 1111 0XXX  four bytes */
                n = c & 7;
                bytes = 4;
            }
            else if ((c & 0xFC) == 0xF8)
            {
                /* 1111 10XX  five bytes */
                n = c & 3;
                bytes = 5;
            }
            else if ((c & 0xFE) == 0xFC)
            {
                /* 1111 110X  six bytes */
                n = c & 1;
                bytes = 6;
            }
            else
            {
                /* 0XXX XXXX one byte */
                ch.Val = c;
                return 0;
            }

            /* successor bytes should have the form 10XX XXXX */
            for (i = 1; i < bytes; ++i)
            {
                c = (str[start + i]) & 0xFF; // Convert to unsigned.
                n = (n << 6) | (c & 0x3F);
            }

            ch.Val = n;
            return bytes - 1;
        }

        /* store char c as UTF-8 encoded byte stream */

        public static int PutUtf8(byte[] buf, int start, int c)
        {
            if (c < 128)
            {
                buf[start++] = (byte) c;
            }
            else if (c <= 0x7FF)
            {
                buf[start++] = (byte) (0xC0 | (c >> 6));
                buf[start++] = (byte) (0x80 | (c & 0x3F));
            }
            else if (c <= 0xFFFF)
            {
                buf[start++] = (byte) (0xE0 | (c >> 12));
                buf[start++] = (byte) (0x80 | ((c >> 6) & 0x3F));
                buf[start++] = (byte) (0x80 | (c & 0x3F));
            }
            else if (c <= 0x1FFFFF)
            {
                buf[start++] = (byte) (0xF0 | (c >> 18));
                buf[start++] = (byte) (0x80 | ((c >> 12) & 0x3F));
                buf[start++] = (byte) (0x80 | ((c >> 6) & 0x3F));
                buf[start++] = (byte) (0x80 | (c & 0x3F));
            }
            else
            {
                buf[start++] = (byte) (0xF8 | (c >> 24));
                buf[start++] = (byte) (0x80 | ((c >> 18) & 0x3F));
                buf[start++] = (byte) (0x80 | ((c >> 12) & 0x3F));
                buf[start++] = (byte) (0x80 | ((c >> 6) & 0x3F));
                buf[start++] = (byte) (0x80 | (c & 0x3F));
            }

            return start;
        }

        private void AddC(int c, int index)
        {
            if (index + 1 >= _lbufsize)
            {
                while (index + 1 >= _lbufsize)
                {
                    if (_lbufsize == 0)
                        _lbufsize = 256;
                    else
                        _lbufsize = _lbufsize*2;
                }

                var temp = new int[_lbufsize];
                if (_linebuf != null)
                    Array.Copy(_linebuf, 0, temp, 0, index);
                _linebuf = temp;
            }

            _linebuf[index] = c;
        }

        private void WrapLine(Out fout, int indent)
        {
            int i;

            if (_wraphere == 0)
            {
                return;
            }

            for (i = 0; i < indent; ++i)
            {
                fout.Outc(' ');
            }

            for (i = 0; i < _wraphere; ++i)
            {
                fout.Outc(_linebuf[i]);
            }

            if (_inString)
            {
                fout.Outc(' ');
                fout.Outc('\\');
            }

            fout.Newline();

            if (_linelen > _wraphere)
            {
                int p = 0;

                if (_linebuf[_wraphere] == ' ')
                {
                    ++_wraphere;
                }

                int q = _wraphere;
                AddC('\x0000', _linelen);

                while (true)
                {
                    _linebuf[p] = _linebuf[q];
                    if (_linebuf[q] == 0)
                    {
                        break;
                    }
                    p++;
                    q++;
                }
                _linelen -= _wraphere;
            }
            else
            {
                _linelen = 0;
            }

            _wraphere = 0;
        }

        private void WrapAttrVal(Out fout, int indent, bool inString)
        {
            int i;

            for (i = 0; i < indent; ++i)
            {
                fout.Outc(' ');
            }

            for (i = 0; i < _wraphere; ++i)
            {
                fout.Outc(_linebuf[i]);
            }

            fout.Outc(' ');

            if (inString)
            {
                fout.Outc('\\');
            }

            fout.Newline();

            if (_linelen > _wraphere)
            {
                int p = 0;

                if (_linebuf[_wraphere] == ' ')
                {
                    ++_wraphere;
                }

                int q = _wraphere;
                AddC('\x0000', _linelen);

                while (true)
                {
                    _linebuf[p] = _linebuf[q];
                    if (_linebuf[q] == 0)
                    {
                        break;
                    }
                    p++;
                    q++;
                }
                _linelen -= _wraphere;
            }
            else
            {
                _linelen = 0;
            }

            _wraphere = 0;
        }

        public virtual void FlushLine(Out fout, int indent)
        {
            if (_linelen > 0)
            {
                if (indent + _linelen >= _options.WrapLen)
                {
                    WrapLine(fout, indent);
                }

                int i;
                if (!_inAttVal || _options.IndentAttributes)
                {
                    for (i = 0; i < indent; ++i)
                    {
                        fout.Outc(' ');
                    }
                }

                for (i = 0; i < _linelen; ++i)
                {
                    fout.Outc(_linebuf[i]);
                }
            }

            fout.Newline();
            _linelen = 0;
            _wraphere = 0;
            _inAttVal = false;
        }

        public virtual void CondFlushLine(Out fout, int indent)
        {
            if (_linelen <= 0) return;
            if (indent + _linelen >= _options.WrapLen)
            {
                WrapLine(fout, indent);
            }

            int i;
            if (!_inAttVal || _options.IndentAttributes)
            {
                for (i = 0; i < indent; ++i)
                {
                    fout.Outc(' ');
                }
            }

            for (i = 0; i < _linelen; ++i)
            {
                fout.Outc(_linebuf[i]);
            }

            fout.Newline();
            _linelen = 0;
            _wraphere = 0;
            _inAttVal = false;
        }

        private void PrintChar(int c, int mode)
        {
            string entity;

            if (c == ' ' && (mode & (PREFORMATTED | COMMENT | ATTRIBVALUE)) == 0)
            {
                /* coerce a space character to a non-breaking space */
                if ((mode & NOWRAP) != 0)
                {
                    /* by default XML doesn't define &nbsp; */
                    if (_options.NumEntities || _options.XmlTags)
                    {
                        AddC('&', _linelen++);
                        AddC('#', _linelen++);
                        AddC('1', _linelen++);
                        AddC('6', _linelen++);
                        AddC('0', _linelen++);
                        AddC(';', _linelen++);
                    }
                        /* otherwise use named entity */
                    else
                    {
                        AddC('&', _linelen++);
                        AddC('n', _linelen++);
                        AddC('b', _linelen++);
                        AddC('s', _linelen++);
                        AddC('p', _linelen++);
                        AddC(';', _linelen++);
                    }
                    return;
                }
                _wraphere = _linelen;
            }

            /* comment characters are passed raw */
            if ((mode & COMMENT) != 0)
            {
                AddC(c, _linelen++);
                return;
            }

            /* except in CDATA map < to &lt; etc. */
            if ((mode & CDATA) == 0)
            {
                if (c == '<')
                {
                    AddC('&', _linelen++);
                    AddC('l', _linelen++);
                    AddC('t', _linelen++);
                    AddC(';', _linelen++);
                    return;
                }

                if (c == '>')
                {
                    AddC('&', _linelen++);
                    AddC('g', _linelen++);
                    AddC('t', _linelen++);
                    AddC(';', _linelen++);
                    return;
                }

                /*
				naked '&' chars can be left alone or
				quoted as &amp; The latter is required
				for XML where naked '&' are illegal.
				*/
                if (c == '&' && _options.QuoteAmpersand)
                {
                    AddC('&', _linelen++);
                    AddC('a', _linelen++);
                    AddC('m', _linelen++);
                    AddC('p', _linelen++);
                    AddC(';', _linelen++);
                    return;
                }

                if (c == '"' && _options.QuoteMarks)
                {
                    AddC('&', _linelen++);
                    AddC('q', _linelen++);
                    AddC('u', _linelen++);
                    AddC('o', _linelen++);
                    AddC('t', _linelen++);
                    AddC(';', _linelen++);
                    return;
                }

                if (c == '\'' && _options.QuoteMarks)
                {
                    AddC('&', _linelen++);
                    AddC('#', _linelen++);
                    AddC('3', _linelen++);
                    AddC('9', _linelen++);
                    AddC(';', _linelen++);
                    return;
                }

                if (c == 160 && _options.CharEncoding != CharEncoding.Raw)
                {
                    if (_options.QuoteNbsp)
                    {
                        AddC('&', _linelen++);

                        if (_options.NumEntities)
                        {
                            AddC('#', _linelen++);
                            AddC('1', _linelen++);
                            AddC('6', _linelen++);
                            AddC('0', _linelen++);
                        }
                        else
                        {
                            AddC('n', _linelen++);
                            AddC('b', _linelen++);
                            AddC('s', _linelen++);
                            AddC('p', _linelen++);
                        }

                        AddC(';', _linelen++);
                    }
                    else
                    {
                        AddC(c, _linelen++);
                    }

                    return;
                }
            }

            /* otherwise ISO 2022 characters are passed raw */
            if (_options.CharEncoding == CharEncoding.Iso2022 || _options.CharEncoding == CharEncoding.Raw)
            {
                AddC(c, _linelen++);
                return;
            }

            /* if preformatted text, map &nbsp; to space */
            if (c == 160 && ((mode & PREFORMATTED) != 0))
            {
                AddC(' ', _linelen++);
                return;
            }

            /*
			Filters from Word and PowerPoint often use smart
			quotes resulting in character codes between 128
			and 159. Unfortunately, the corresponding HTML 4.0
			entities for these are not widely supported. The
			following converts dashes and quotation marks to
			the nearest ASCII equivalent. My thanks to
			Andrzej Novosiolov for his help with this code.
			*/

            if (_options.MakeClean)
            {
                if (c >= 0x2013 && c <= 0x201E)
                {
                    switch (c)
                    {
                        case 0x2013:
                        case 0x2014:
                            c = '-';
                            break;

                        case 0x2018:
                        case 0x2019:
                        case 0x201A:
                            c = '\'';
                            break;

                        case 0x201C:
                        case 0x201D:
                        case 0x201E:
                            c = '"';
                            break;
                    }
                }
            }

            /* don't map latin-1 chars to entities */
            if (_options.CharEncoding == CharEncoding.Latin1)
            {
                if (c > 255)
                {
                    /* multi byte chars */
                    if (!_options.NumEntities)
                    {
                        entity = EntityTable.DefaultEntityTable.EntityName((short) c);
                        if (entity != null)
                        {
                            entity = "&" + entity + ";";
                        }
                        else
                        {
                            entity = "&#" + c + ";";
                        }
                    }
                    else
                    {
                        entity = "&#" + c + ";";
                    }

                    for (int i = 0; i < entity.Length; i++)
                    {
                        AddC(entity[i], _linelen++);
                    }

                    return;
                }

                if (c > 126 && c < 160)
                {
                    entity = "&#" + c + ";";

                    for (int i = 0; i < entity.Length; i++)
                    {
                        AddC(entity[i], _linelen++);
                    }

                    return;
                }

                AddC(c, _linelen++);
                return;
            }

            /* don't map utf8 chars to entities */
            if (_options.CharEncoding == CharEncoding.Utf8)
            {
                AddC(c, _linelen++);
                return;
            }

            /* use numeric entities only  for XML */
            if (_options.XmlTags)
            {
                /* if ASCII use numeric entities for chars > 127 */
                if (c > 127 && _options.CharEncoding == CharEncoding.Ascii)
                {
                    entity = "&#" + c + ";";

                    for (int i = 0; i < entity.Length; i++)
                    {
                        AddC(entity[i], _linelen++);
                    }

                    return;
                }

                /* otherwise output char raw */
                AddC(c, _linelen++);
                return;
            }

            /* default treatment for ASCII */
            if (c > 126 || (c < ' ' && c != '\t'))
            {
                if (!_options.NumEntities)
                {
                    entity = EntityTable.DefaultEntityTable.EntityName((short) c);
                    if (entity != null)
                    {
                        entity = "&" + entity + ";";
                    }
                    else
                    {
                        entity = "&#" + c + ";";
                    }
                }
                else
                {
                    entity = "&#" + c + ";";
                }

                for (int i = 0; i < entity.Length; i++)
                {
                    AddC(entity[i], _linelen++);
                }

                return;
            }

            AddC(c, _linelen++);
        }

        /* 
		The line buffer is uint not char so we can
		hold Unicode values unencoded. The translation
		to UTF-8 is deferred to the outc routine called
		to flush the line buffer.
		*/

        private void PrintText(Out fout, int mode, int indent, byte[] textarray, int start, int end)
        {
            int i;
            var ci = new MutableInteger();

            for (i = start; i < end; ++i)
            {
                if (indent + _linelen >= _options.WrapLen)
                {
                    WrapLine(fout, indent);
                }

                int c = (textarray[i]) & 0xFF;

                /* look for UTF-8 multibyte character */
                if (c > 0x7F)
                {
                    i += GetUtf8(textarray, i, ci);
                    c = ci.Val;
                }

                if (c == '\n')
                {
                    FlushLine(fout, indent);
                    continue;
                }

                PrintChar(c, mode);
            }
        }

        private void PrintString(string str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                AddC(str[i], _linelen++);
            }
        }

        private void PrintAttrValue(Out fout, int indent, string val, int delim, bool wrappable)
        {
            var ci = new MutableInteger();
            bool wasinstring = false;
            byte[] valueChars = null;
            int mode = (wrappable ? (NORMAL | ATTRIBVALUE) : (PREFORMATTED | ATTRIBVALUE));

            if (val != null)
            {
                valueChars = Lexer.GetBytes(val);
            }

            /* look for ASP, Tango or PHP instructions for computed attribute value */
            if (valueChars != null && valueChars.Length >= 5 && valueChars[0] == '<')
            {
                var tmpChar = new char[valueChars.Length];
                valueChars.CopyTo(tmpChar, 0);
                if (valueChars[1] == '%' || valueChars[1] == '@' || (new string(tmpChar, 0, 5)).Equals("<?php"))
                    mode |= CDATA;
            }

            if (delim == 0)
            {
                delim = '"';
            }

            AddC('=', _linelen++);

            /* don't wrap after "=" for xml documents */
            if (!_options.XmlOut)
            {
                if (indent + _linelen < _options.WrapLen)
                {
                    _wraphere = _linelen;
                }

                if (indent + _linelen >= _options.WrapLen)
                {
                    WrapLine(fout, indent);
                }

                if (indent + _linelen < _options.WrapLen)
                {
                    _wraphere = _linelen;
                }
                else
                {
                    CondFlushLine(fout, indent);
                }
            }

            AddC(delim, _linelen++);

            if (val != null)
            {
                _inString = false;

                int i = 0;
                while (valueChars != null && i < valueChars.Length)
                {
                    int c = (valueChars[i]) & 0xFF;

                    if (wrappable && c == ' ' && indent + _linelen < _options.WrapLen)
                    {
                        _wraphere = _linelen;
                        wasinstring = _inString;
                    }

                    if (wrappable && _wraphere > 0 && indent + _linelen >= _options.WrapLen)
                        WrapAttrVal(fout, indent, wasinstring);

                    if (c == delim)
                    {
                        string entity = (c == '"' ? "&quot;" : "&#39;");

                        for (int j = 0; j < entity.Length; j++)
                        {
                            AddC(entity[j], _linelen++);
                        }

                        ++i;
                        continue;
                    }
                    if (c == '"')
                    {
                        if (_options.QuoteMarks)
                        {
                            AddC('&', _linelen++);
                            AddC('q', _linelen++);
                            AddC('u', _linelen++);
                            AddC('o', _linelen++);
                            AddC('t', _linelen++);
                            AddC(';', _linelen++);
                        }
                        else
                        {
                            AddC('"', _linelen++);
                        }

                        if (delim == '\'')
                        {
                            _inString = !_inString;
                        }

                        ++i;
                        continue;
                    }
                    if (c == '\'')
                    {
                        if (_options.QuoteMarks)
                        {
                            AddC('&', _linelen++);
                            AddC('#', _linelen++);
                            AddC('3', _linelen++);
                            AddC('9', _linelen++);
                            AddC(';', _linelen++);
                        }
                        else
                        {
                            AddC('\'', _linelen++);
                        }

                        if (delim == '"')
                        {
                            _inString = !_inString;
                        }

                        ++i;
                        continue;
                    }

                    /* look for UTF-8 multibyte character */
                    if (c > 0x7F)
                    {
                        i += GetUtf8(valueChars, i, ci);
                        c = ci.Val;
                    }

                    ++i;

                    if (c == '\n')
                    {
                        FlushLine(fout, indent);
                        continue;
                    }

                    PrintChar(c, mode);
                }
            }

            _inString = false;
            AddC(delim, _linelen++);
        }

        private void PrintAttribute(Out fout, int indent, Node node, AttVal attr)
        {
            bool wrappable = false;

            if (_options.IndentAttributes)
            {
                FlushLine(fout, indent);
                indent += _options.Spaces;
            }

            string name = attr.Attribute;

            if (indent + _linelen >= _options.WrapLen)
            {
                WrapLine(fout, indent);
            }

            if (!_options.XmlTags && !_options.XmlOut && attr.Dict != null)
            {
                if (AttributeTable.DefaultAttributeTable.IsScript(name))
                {
                    wrappable = _options.WrapScriptlets;
                }
                else if (!attr.Dict.Nowrap && _options.WrapAttVals)
                {
                    wrappable = true;
                }
            }

            if (indent + _linelen < _options.WrapLen)
            {
                _wraphere = _linelen;
                AddC(' ', _linelen++);
            }
            else
            {
                CondFlushLine(fout, indent);
                AddC(' ', _linelen++);
            }

            for (int i = 0; i < name.Length; i++)
            {
                AddC(Lexer.FoldCase(name[i], _options.UpperCaseAttrs, _options.XmlTags), _linelen++);
            }

            if (indent + _linelen >= _options.WrapLen)
            {
                WrapLine(fout, indent);
            }

            if (attr.Val == null)
            {
                if (_options.XmlTags || _options.XmlOut)
                {
                    PrintAttrValue(fout, indent, attr.Attribute, attr.Delim, true);
                }
                else if (!attr.BoolAttribute && !Node.IsNewNode(node))
                {
                    PrintAttrValue(fout, indent, "", attr.Delim, true);
                }
                else if (indent + _linelen < _options.WrapLen)
                {
                    _wraphere = _linelen;
                }
            }
            else
            {
                PrintAttrValue(fout, indent, attr.Val, attr.Delim, wrappable);
            }
        }

        private void PrintAttrs(Out fout, int indent, Node node, AttVal attr)
        {
            if (attr != null)
            {
                if (attr.Next != null)
                {
                    PrintAttrs(fout, indent, node, attr.Next);
                }

                if (attr.Attribute != null)
                {
                    PrintAttribute(fout, indent, node, attr);
                }
                else if (attr.Asp != null)
                {
                    AddC(' ', _linelen++);
                    PrintAsp(fout, indent, attr.Asp);
                }
                else if (attr.Php != null)
                {
                    AddC(' ', _linelen++);
                    PrintPhp(fout, indent, attr.Php);
                }
            }

            /* add xml:space attribute to pre and other elements */
            if (_options.XmlOut && _options.XmlSpace && ParserImpl.XmlPreserveWhiteSpace(node, _options.TagTable) &&
                node.GetAttrByName("xml:space") == null)
            {
                PrintString(" xml:space=\"preserve\"");
            }
        }

        /*
		Line can be wrapped immediately after inline start tag provided
		if follows a text node ending in a space, or it parent is an
		inline element that that rule applies to. This behaviour was
		reverse engineered from Netscape 3.0
		*/

        private static bool AfterSpace(Node node)
        {
            if (node == null || node.Tag == null || (node.Tag.Model & ContentModel.INLINE) == 0)
            {
                return true;
            }

            Node prev = node.Prev;

            if (prev != null)
            {
                if (prev.Type == Node.TEXT_NODE && prev.End > prev.Start)
                {
                    int c = (prev.Textarray[prev.End - 1]) & 0xFF;

                    if (c == 160 || c == ' ' || c == '\n')
                    {
                        return true;
                    }
                }

                return false;
            }

            return AfterSpace(node.Parent);
        }

        private void PrintTag(Lexer lexer, Out fout, int mode, int indent, Node node)
        {
            TagCollection tt = _options.TagTable;

            AddC('<', _linelen++);

            if (node.Type == Node.END_TAG)
            {
                AddC('/', _linelen++);
            }

            string p = node.Element;
            for (int i = 0; i < p.Length; i++)
            {
                AddC(Lexer.FoldCase(p[i], _options.UpperCaseTags, _options.XmlTags), _linelen++);
            }

            PrintAttrs(fout, indent, node, node.Attributes);

            if ((_options.XmlOut || lexer != null && lexer.Isvoyager) &&
                (node.Type == Node.START_END_TAG || (node.Tag.Model & ContentModel.EMPTY) != 0))
            {
                AddC(' ', _linelen++); /* compatibility hack */
                AddC('/', _linelen++);
            }

            AddC('>', _linelen++);

            if (node.Type == Node.START_END_TAG || (mode & PREFORMATTED) != 0) return;
            if (indent + _linelen >= _options.WrapLen)
            {
                WrapLine(fout, indent);
            }

            if (indent + _linelen < _options.WrapLen)
            {
                /*
					wrap after start tag if is <br/> or if it's not
					inline or it is an empty tag followed by </a>
					*/
                if (AfterSpace(node))
                {
                    if ((mode & NOWRAP) == 0 &&
                        ((node.Tag.Model & ContentModel.INLINE) == 0 || (node.Tag == tt.TagBr) ||
                         (((node.Tag.Model & ContentModel.EMPTY) != 0) && node.Next == null &&
                          node.Parent.Tag == tt.TagA)))
                    {
                        _wraphere = _linelen;
                    }
                }
            }
            else
            {
                CondFlushLine(fout, indent);
            }
        }

        private void PrintEndTag(Node node)
        {
            /*
			Netscape ignores SGML standard by not ignoring a
			line break before </A> or </U> etc. To avoid rendering 
			this as an underlined space, I disable line wrapping
			before inline end tags by the #if 0 ... #endif
			*/
            //if (false)
            //{
            //	if (indent + linelen < _options.WrapLen && !((mode & NOWRAP) != 0))
            //		wraphere = linelen;
            //}

            AddC('<', _linelen++);
            AddC('/', _linelen++);

            string p = node.Element;
            for (int i = 0; i < p.Length; i++)
            {
                AddC(Lexer.FoldCase(p[i], _options.UpperCaseTags, _options.XmlTags), _linelen++);
            }

            AddC('>', _linelen++);
        }

        private void PrintComment(Out fout, int indent, Node node)
        {
            if (indent + _linelen < _options.WrapLen)
            {
                _wraphere = _linelen;
            }

            AddC('<', _linelen++);
            AddC('!', _linelen++);
            AddC('-', _linelen++);
            AddC('-', _linelen++);
            PrintText(fout, COMMENT, indent, node.Textarray, node.Start, node.End);
            // See Lexer.java: AQ 8Jul2000
            AddC('-', _linelen++);
            AddC('-', _linelen++);
            AddC('>', _linelen++);

            if (node.Linebreak)
            {
                FlushLine(fout, indent);
            }
        }

        private void PrintDocType(Out fout, int indent, Node node)
        {
            bool q = _options.QuoteMarks;

            _options.QuoteMarks = false;

            if (indent + _linelen < _options.WrapLen)
            {
                _wraphere = _linelen;
            }

            CondFlushLine(fout, indent);

            AddC('<', _linelen++);
            AddC('!', _linelen++);
            AddC('D', _linelen++);
            AddC('O', _linelen++);
            AddC('C', _linelen++);
            AddC('T', _linelen++);
            AddC('Y', _linelen++);
            AddC('P', _linelen++);
            AddC('E', _linelen++);
            AddC(' ', _linelen++);

            if (indent + _linelen < _options.WrapLen)
            {
                _wraphere = _linelen;
            }

            PrintText(fout, 0, indent, node.Textarray, node.Start, node.End);

            if (_linelen < _options.WrapLen)
            {
                _wraphere = _linelen;
            }

            AddC('>', _linelen++);
            _options.QuoteMarks = q;
            CondFlushLine(fout, indent);
        }

        private void PrintPi(Out fout, int indent, Node node)
        {
            if (indent + _linelen < _options.WrapLen)
            {
                _wraphere = _linelen;
            }

            AddC('<', _linelen++);
            AddC('?', _linelen++);

            /* set CDATA to pass < and > unescaped */
            PrintText(fout, CDATA, indent, node.Textarray, node.Start, node.End);

            if (node.Textarray[node.End - 1] != (byte) '?')
            {
                AddC('?', _linelen++);
            }

            AddC('>', _linelen++);
            CondFlushLine(fout, indent);
        }

        /* note ASP and JSTE share <% ... %> syntax */

        private void PrintAsp(Out fout, int indent, Node node)
        {
            int savewraplen = _options.WrapLen;

            /* disable wrapping if so requested */

            if (!_options.WrapAsp || !_options.WrapJste)
            {
                _options.WrapLen = 0xFFFFFF;
            }
            /* a very large number */

            AddC('<', _linelen++);
            AddC('%', _linelen++);

            PrintText(fout, (_options.WrapAsp ? CDATA : COMMENT), indent, node.Textarray, node.Start, node.End);

            AddC('%', _linelen++);
            AddC('>', _linelen++);

            /* CondFlushLine(fout, indent); */
            _options.WrapLen = savewraplen;
        }

        /* JSTE also supports <# ... #> syntax */

        private void PrintJste(Out fout, int indent, Node node)
        {
            int savewraplen = _options.WrapLen;

            /* disable wrapping if so requested */

            if (!_options.WrapJste)
            {
                _options.WrapLen = 0xFFFFFF;
            }

            /* a very large number */

            AddC('<', _linelen++);
            AddC('#', _linelen++);

            PrintText(fout, (_options.WrapJste ? CDATA : COMMENT), indent, node.Textarray, node.Start, node.End);

            AddC('#', _linelen++);
            AddC('>', _linelen++);
            /* CondFlushLine(fout, indent); */
            _options.WrapLen = savewraplen;
        }

        /* PHP is based on XML processing instructions */

        private void PrintPhp(Out fout, int indent, Node node)
        {
            int savewraplen = _options.WrapLen;

            /* disable wrapping if so requested */

            if (!_options.WrapPhp)
            {
                _options.WrapLen = 0xFFFFFF;
            }
            /* a very large number */
            AddC('<', _linelen++);
            AddC('?', _linelen++);

            PrintText(fout, (_options.WrapPhp ? CDATA : COMMENT), indent, node.Textarray, node.Start, node.End);

            AddC('?', _linelen++);
            AddC('>', _linelen++);
            /* PCondFlushLine(fout, indent); */
            _options.WrapLen = savewraplen;
        }

        private void PrintCdata(Out fout, int indent, Node node)
        {
            int savewraplen = _options.WrapLen;

            CondFlushLine(fout, indent);

            /* disable wrapping */

            _options.WrapLen = 0xFFFFFF; /* a very large number */

            AddC('<', _linelen++);
            AddC('!', _linelen++);
            AddC('[', _linelen++);
            AddC('C', _linelen++);
            AddC('D', _linelen++);
            AddC('A', _linelen++);
            AddC('T', _linelen++);
            AddC('A', _linelen++);
            AddC('[', _linelen++);

            PrintText(fout, COMMENT, indent, node.Textarray, node.Start, node.End);

            AddC(']', _linelen++);
            AddC(']', _linelen++);
            AddC('>', _linelen++);
            CondFlushLine(fout, indent);
            _options.WrapLen = savewraplen;
        }

        private void PrintSection(Out fout, int indent, Node node)
        {
            int savewraplen = _options.WrapLen;

            /* disable wrapping if so requested */
            if (!_options.WrapSection)
            {
                _options.WrapLen = 0xFFFFFF;
            }

            /* a very large number */

            AddC('<', _linelen++);
            AddC('!', _linelen++);
            AddC('[', _linelen++);

            PrintText(fout, (_options.WrapSection ? CDATA : COMMENT), indent, node.Textarray, node.Start, node.End);

            AddC(']', _linelen++);
            AddC('>', _linelen++);
            /* PCondFlushLine(fout, indent); */
            _options.WrapLen = savewraplen;
        }

        private bool ShouldIndent(Node node)
        {
            TagCollection tt = _options.TagTable;

            if (!_options.IndentContent)
                return false;

            if (_options.SmartIndent)
            {
                if (node.Content != null && ((node.Tag.Model & ContentModel.NO_INDENT) != 0))
                {
                    for (node = node.Content; node != null; node = node.Next)
                    {
                        if (node.Tag != null && (node.Tag.Model & ContentModel.BLOCK) != 0)
                        {
                            return true;
                        }
                    }

                    return false;
                }

                if ((node.Tag.Model & ContentModel.HEADING) != 0)
                {
                    return false;
                }

                if (node.Tag == tt.TagP)
                {
                    return false;
                }

                if (node.Tag == tt.TagTitle)
                {
                    return false;
                }
            }

            if ((node.Tag.Model & (ContentModel.FIELD | ContentModel.OBJECT)) != 0)
            {
                return true;
            }

            if (node.Tag == tt.TagMap)
            {
                return true;
            }

            return (node.Tag.Model & ContentModel.INLINE) == 0;
        }

        public virtual void PrintTree(Out fout, int mode, int indent, Lexer lexer, Node node)
        {
            Node content;
            TagCollection tt = _options.TagTable;

            if (node == null)
                return;

            if (node.Type == Node.TEXT_NODE)
            {
                PrintText(fout, mode, indent, node.Textarray, node.Start, node.End);
            }
            else if (node.Type == Node.COMMENT_TAG)
            {
                PrintComment(fout, indent, node);
            }
            else if (node.Type == Node.ROOT_NODE)
            {
                for (content = node.Content; content != null; content = content.Next)
                {
                    PrintTree(fout, mode, indent, lexer, content);
                }
            }
            else if (node.Type == Node.DOC_TYPE_TAG)
            {
                PrintDocType(fout, indent, node);
            }
            else if (node.Type == Node.PROC_INS_TAG)
            {
                PrintPi(fout, indent, node);
            }
            else if (node.Type == Node.CDATA_TAG)
            {
                PrintCdata(fout, indent, node);
            }
            else if (node.Type == Node.SECTION_TAG)
            {
                PrintSection(fout, indent, node);
            }
            else if (node.Type == Node.ASP_TAG)
            {
                PrintAsp(fout, indent, node);
            }
            else if (node.Type == Node.JSTE_TAG)
            {
                PrintJste(fout, indent, node);
            }
            else if (node.Type == Node.PHP_TAG)
            {
                PrintPhp(fout, indent, node);
            }
            else if ((node.Tag.Model & ContentModel.EMPTY) != 0 || node.Type == Node.START_END_TAG)
            {
                if ((node.Tag.Model & ContentModel.INLINE) == 0)
                {
                    CondFlushLine(fout, indent);
                }

                if (node.Tag == tt.TagBr && node.Prev != null && node.Prev.Tag != tt.TagBr && _options.BreakBeforeBr)
                {
                    FlushLine(fout, indent);
                }

                if (_options.MakeClean && node.Tag == tt.TagWbr)
                {
                    PrintString(" ");
                }
                else
                {
                    PrintTag(lexer, fout, mode, indent, node);
                }

                if (node.Tag == tt.TagParam || node.Tag == tt.TagArea)
                {
                    CondFlushLine(fout, indent);
                }
                else if (node.Tag == tt.TagBr || node.Tag == tt.TagHr)
                {
                    FlushLine(fout, indent);
                }
            }
            else
            {
                /* some kind of container element */
                if (node.Tag != null && node.Tag.Parser == ParserImpl.ParsePre)
                {
                    CondFlushLine(fout, indent);

                    indent = 0;
                    CondFlushLine(fout, indent);
                    PrintTag(lexer, fout, mode, indent, node);
                    FlushLine(fout, indent);

                    for (content = node.Content; content != null; content = content.Next)
                    {
                        PrintTree(fout, (mode | PREFORMATTED | NOWRAP), indent, lexer, content);
                    }

                    CondFlushLine(fout, indent);
                    PrintEndTag(node);
                    FlushLine(fout, indent);

                    if (_options.IndentContent == false && node.Next != null)
                    {
                        FlushLine(fout, indent);
                    }
                }
                else if (node.Tag == tt.TagStyle || node.Tag == tt.TagScript)
                {
                    CondFlushLine(fout, indent);

                    indent = 0;
                    CondFlushLine(fout, indent);
                    PrintTag(lexer, fout, mode, indent, node);
                    FlushLine(fout, indent);

                    for (content = node.Content; content != null; content = content.Next)
                    {
                        PrintTree(fout, (mode | PREFORMATTED | NOWRAP | CDATA), indent, lexer, content);
                    }

                    CondFlushLine(fout, indent);
                    PrintEndTag(node);
                    FlushLine(fout, indent);

                    if (_options.IndentContent == false && node.Next != null)
                    {
                        FlushLine(fout, indent);
                    }
                }
                else if ((node.Tag.Model & ContentModel.INLINE) != 0)
                {
                    if (_options.MakeClean)
                    {
                        /* discards <font> and </font> tags */
                        if (node.Tag == tt.TagFont)
                        {
                            for (content = node.Content; content != null; content = content.Next)
                            {
                                PrintTree(fout, mode, indent, lexer, content);
                            }
                            return;
                        }

                        /* replace <nobr>...</nobr> by &nbsp; or &#160; etc. */
                        if (node.Tag == tt.TagNobr)
                        {
                            for (content = node.Content; content != null; content = content.Next)
                            {
                                PrintTree(fout, (mode | NOWRAP), indent, lexer, content);
                            }
                            return;
                        }
                    }

                    /* otherwise a normal inline element */

                    PrintTag(lexer, fout, mode, indent, node);

                    /* indent content for SELECT, TEXTAREA, MAP, OBJECT and APPLET */

                    if (ShouldIndent(node))
                    {
                        CondFlushLine(fout, indent);
                        indent += _options.Spaces;

                        for (content = node.Content; content != null; content = content.Next)
                        {
                            PrintTree(fout, mode, indent, lexer, content);
                        }

                        CondFlushLine(fout, indent);
                        indent -= _options.Spaces;
                        CondFlushLine(fout, indent);
                    }
                    else
                    {
                        for (content = node.Content; content != null; content = content.Next)
                        {
                            PrintTree(fout, mode, indent, lexer, content);
                        }
                    }

                    PrintEndTag(node);
                }
                else
                {
                    /* other tags */
                    CondFlushLine(fout, indent);

                    if (_options.SmartIndent && node.Prev != null)
                    {
                        FlushLine(fout, indent);
                    }

                    if (_options.HideEndTags == false ||
                        !(node.Tag != null && ((node.Tag.Model & ContentModel.OMIT_ST) != 0)))
                    {
                        PrintTag(lexer, fout, mode, indent, node);

                        if (ShouldIndent(node))
                        {
                            CondFlushLine(fout, indent);
                        }
                        else if ((node.Tag.Model & ContentModel.HTML) != 0 || node.Tag == tt.TagNoframes ||
                                 ((node.Tag.Model & ContentModel.HEAD) != 0 && node.Tag != tt.TagTitle))
                        {
                            FlushLine(fout, indent);
                        }
                    }

                    if (node.Tag == tt.TagBody && _options.BurstSlides)
                    {
                        PrintSlide(fout, mode, (_options.IndentContent ? indent + _options.Spaces : indent), lexer);
                    }
                    else
                    {
                        Node last = null;

                        for (content = node.Content; content != null; content = content.Next)
                        {
                            /* kludge for naked text before block level tag */
                            if (last != null && !_options.IndentContent && last.Type == Node.TEXT_NODE &&
                                content.Tag != null && (content.Tag.Model & ContentModel.BLOCK) != 0)
                            {
                                FlushLine(fout, indent);
                                FlushLine(fout, indent);
                            }

                            PrintTree(fout, mode, (ShouldIndent(node) ? indent + _options.Spaces : indent), lexer,
                                      content);

                            last = content;
                        }
                    }

                    /* don't flush line for td and th */
                    if (ShouldIndent(node) ||
                        (((node.Tag.Model & ContentModel.HTML) != 0 || node.Tag == tt.TagNoframes ||
                          ((node.Tag.Model & ContentModel.HEAD) != 0 && node.Tag != tt.TagTitle)) &&
                         _options.HideEndTags == false))
                    {
                        CondFlushLine(fout, (_options.IndentContent ? indent + _options.Spaces : indent));

                        if (_options.HideEndTags == false || (node.Tag.Model & ContentModel.OPT) == 0)
                        {
                            PrintEndTag(node);
                            FlushLine(fout, indent);
                        }
                    }
                    else
                    {
                        if (_options.HideEndTags == false || (node.Tag.Model & ContentModel.OPT) == 0)
                        {
                            PrintEndTag(node);
                        }

                        FlushLine(fout, indent);
                    }

                    if (_options.IndentContent == false && node.Next != null && _options.HideEndTags == false &&
                        (node.Tag.Model &
                         (ContentModel.BLOCK | ContentModel.LIST | ContentModel.DEFLIST | ContentModel.TABLE)) != 0)
                    {
                        FlushLine(fout, indent);
                    }
                }
            }
        }

        public virtual void PrintXmlTree(Out fout, int mode, int indent, Lexer lexer, Node node)
        {
            TagCollection tt = _options.TagTable;

            if (node == null)
            {
                return;
            }

            if (node.Type == Node.TEXT_NODE)
            {
                PrintText(fout, mode, indent, node.Textarray, node.Start, node.End);
            }
            else if (node.Type == Node.COMMENT_TAG)
            {
                CondFlushLine(fout, indent);
                PrintComment(fout, 0, node);
                CondFlushLine(fout, 0);
            }
            else if (node.Type == Node.ROOT_NODE)
            {
                Node content;

                for (content = node.Content; content != null; content = content.Next)
                {
                    PrintXmlTree(fout, mode, indent, lexer, content);
                }
            }
            else if (node.Type == Node.DOC_TYPE_TAG)
            {
                PrintDocType(fout, indent, node);
            }
            else if (node.Type == Node.PROC_INS_TAG)
            {
                PrintPi(fout, indent, node);
            }
            else if (node.Type == Node.SECTION_TAG)
            {
                PrintSection(fout, indent, node);
            }
            else if (node.Type == Node.ASP_TAG)
            {
                PrintAsp(fout, indent, node);
            }
            else if (node.Type == Node.JSTE_TAG)
            {
                PrintJste(fout, indent, node);
            }
            else if (node.Type == Node.PHP_TAG)
            {
                PrintPhp(fout, indent, node);
            }
            else if ((node.Tag.Model & ContentModel.EMPTY) != 0 || node.Type == Node.START_END_TAG)
            {
                CondFlushLine(fout, indent);
                PrintTag(lexer, fout, mode, indent, node);
                FlushLine(fout, indent);

                if (node.Next != null)
                {
                    FlushLine(fout, indent);
                }
            }
            else
            {
                /* some kind of container element */
                Node content;
                bool mixed = false;
                int cindent;

                for (content = node.Content; content != null; content = content.Next)
                {
                    if (content.Type == Node.TEXT_NODE)
                    {
                        mixed = true;
                        break;
                    }
                }

                CondFlushLine(fout, indent);

                if (ParserImpl.XmlPreserveWhiteSpace(node, tt))
                {
                    indent = 0;
                    cindent = 0;
                    mixed = false;
                }
                else if (mixed)
                {
                    cindent = indent;
                }
                else
                {
                    cindent = indent + _options.Spaces;
                }

                PrintTag(lexer, fout, mode, indent, node);

                if (!mixed)
                {
                    FlushLine(fout, indent);
                }

                for (content = node.Content; content != null; content = content.Next)
                {
                    PrintXmlTree(fout, mode, cindent, lexer, content);
                }

                if (!mixed)
                {
                    CondFlushLine(fout, cindent);
                }
                PrintEndTag(node);
                CondFlushLine(fout, indent);

                if (node.Next != null)
                {
                    FlushLine(fout, indent);
                }
            }
        }


        /* split parse tree by h2 elements and output to separate files */

        /* counts number of h2 children belonging to node */

        public virtual int CountSlides(Node node)
        {
            int n = 1;
            TagCollection tt = _options.TagTable;

            for (node = node.Content; node != null; node = node.Next)
            {
                if (node.Tag == tt.TagH2)
                {
                    ++n;
                }
            }

            return n;
        }

        /*
		inserts a space gif called "dot.gif" to ensure
		that the  slide is at least n pixels high
		*/

        //private void PrintVertSpacer(Out fout, int indent)
        //{
        //    CondFlushLine(fout, indent);
        //    PrintString("<img width=\"0\" height=\"0\" hspace=\"1\" src=\"dot.gif\" vspace=\"%d\" align=\"left\">");
        //    CondFlushLine(fout, indent);
        //}

        private void PrintNavBar(Out fout, int indent)
        {
            string buf;

            CondFlushLine(fout, indent);
            PrintString("<center><small>");

            if (_slide > 1)
            {
                buf = "<a href=\"slide" + (_slide - 1).ToString() + ".html\">previous</a> | ";
                PrintString(buf);
                CondFlushLine(fout, indent);

                PrintString(_slide < _count
                                ? "<a href=\"slide1.html\">start</a> | "
                                : "<a href=\"slide1.html\">start</a>");

                CondFlushLine(fout, indent);
            }

            if (_slide < _count)
            {
                buf = "<a href=\"slide" + (_slide + 1).ToString() + ".html\">next</a>";
                PrintString(buf);
            }

            PrintString("</small></center>");
            CondFlushLine(fout, indent);
        }

        /*
		Called from printTree to print the content of a slide from
		the node slidecontent. On return slidecontent points to the
		node starting the next slide or null. The variables slide
		and count are used to customise the navigation bar.
		*/

        public virtual void PrintSlide(Out fout, int mode, int indent, Lexer lexer)
        {
            TagCollection tt = _options.TagTable;

            /* insert div for onclick handler */
            string s = "<div onclick=\"document.location='slide" + (_slide < _count ? _slide + 1 : 1).ToString() +
                       ".html'\">";
            PrintString(s);
            CondFlushLine(fout, indent);

            /* first print the h2 element and navbar */
            if (_slidecontent.Tag == tt.TagH2)
            {
                PrintNavBar(fout, indent);

                /* now print an hr after h2 */

                AddC('<', _linelen++);


                AddC(Lexer.FoldCase('h', _options.UpperCaseTags, _options.XmlTags), _linelen++);
                AddC(Lexer.FoldCase('r', _options.UpperCaseTags, _options.XmlTags), _linelen++);

                if (_options.XmlOut)
                {
                    PrintString(" />");
                }
                else
                {
                    AddC('>', _linelen++);
                }

                if (_options.IndentContent)
                {
                    CondFlushLine(fout, indent);
                }

                /* PrintVertSpacer(fout, indent); */

                /*CondFlushLine(fout, indent); */

                /* print the h2 element */
                PrintTree(fout, mode, (_options.IndentContent ? indent + _options.Spaces : indent), lexer, _slidecontent);

                _slidecontent = _slidecontent.Next;
            }

            /* now continue until we reach the next h2 */

            Node last = null;
            Node content = _slidecontent;

            for (; content != null; content = content.Next)
            {
                if (content.Tag == tt.TagH2)
                {
                    break;
                }

                /* kludge for naked text before block level tag */
                if (last != null && !_options.IndentContent && last.Type == Node.TEXT_NODE && content.Tag != null &&
                    (content.Tag.Model & ContentModel.BLOCK) != 0)
                {
                    FlushLine(fout, indent);
                    FlushLine(fout, indent);
                }

                PrintTree(fout, mode, (_options.IndentContent ? indent + _options.Spaces : indent), lexer, content);

                last = content;
            }

            _slidecontent = content;

            /* now print epilog */

            CondFlushLine(fout, indent);

            PrintString("<br clear=\"all\">");
            CondFlushLine(fout, indent);

            AddC('<', _linelen++);


            AddC(Lexer.FoldCase('h', _options.UpperCaseTags, _options.XmlTags), _linelen++);
            AddC(Lexer.FoldCase('r', _options.UpperCaseTags, _options.XmlTags), _linelen++);

            if (_options.XmlOut)
            {
                PrintString(" />");
            }
            else
            {
                AddC('>', _linelen++);
            }

            if (_options.IndentContent)
            {
                CondFlushLine(fout, indent);
            }

            PrintNavBar(fout, indent);

            /* end tag for div */
            PrintString("</div>");
            CondFlushLine(fout, indent);
        }

        /*
		Add meta element for page transition effect, this works on IE but not NS
		*/

        public virtual void AddTransitionEffect(Lexer lexer, Node root, short effect, double duration)
        {
            Node head = root.FindHead(lexer.Options.TagTable);
            string transition;

            if (0 <= effect && effect <= 23)
            {
                transition = "revealTrans(Duration=" + (duration).ToString() + ",Transition=" + effect + ")";
            }
            else
            {
                transition = "blendTrans(Duration=" + (duration).ToString() + ")";
            }

            if (head != null)
            {
                Node meta = lexer.InferredTag("meta");
                meta.AddAttribute("http-equiv", "Page-Enter");
                meta.AddAttribute("content", transition);
                Node.InsertNodeAtStart(head, meta);
            }
        }

        public virtual void CreateSlides(Lexer lexer, Node root)
        {
            Out output = new OutImpl();

            Node body = root.FindBody(lexer.Options.TagTable);
            _count = CountSlides(body);
            _slidecontent = body.Content;
            AddTransitionEffect(lexer, root, EFFECT_BLEND, 3.0);

            for (_slide = 1; _slide <= _count; ++_slide)
            {
                string buf = "slide" + _slide + ".html";
                output.State = StreamIn.FSM_ASCII;
                output.Encoding = _options.CharEncoding;

                try
                {
                    output.Output = new MemoryStream();
                    PrintTree(output, 0, 0, lexer, root);
                    FlushLine(output, 0);
                }
                catch (IOException e)
                {
                    Debug.WriteLine(buf + e);
                }
            }
        }
    }
}