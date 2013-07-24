using System.Collections.Generic;

namespace Tidy.Core
{
    /// <summary>
    ///     Tag dictionary node hash table
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
    internal class TagCollection
    {
        private static readonly Dict[] Tags = new[]
            {
                new Dict("html", HtmlVersion.All | HtmlVersion.Frames,
                         ContentModel.HTML | ContentModel.OPT | ContentModel.OMIT_ST, ParserImpl.ParseHtml,
                         CheckAttribsImpl.CheckHtml),
                new Dict("head", HtmlVersion.All | HtmlVersion.Frames,
                         ContentModel.HTML | ContentModel.OPT | ContentModel.OMIT_ST, ParserImpl.ParseHead, null),
                new Dict("title", HtmlVersion.All | HtmlVersion.Frames, ContentModel.HEAD, ParserImpl.ParseTitle, null),
                new Dict("base", HtmlVersion.All | HtmlVersion.Frames, ContentModel.HEAD | ContentModel.EMPTY, null,
                         null),
                new Dict("link", HtmlVersion.All | HtmlVersion.Frames, ContentModel.HEAD | ContentModel.EMPTY, null,
                         CheckAttribsImpl.CheckLink),
                new Dict("meta", HtmlVersion.All | HtmlVersion.Frames, ContentModel.HEAD | ContentModel.EMPTY, null,
                         null),
                new Dict("style", HtmlVersion.From32 | HtmlVersion.Frames, ContentModel.HEAD, ParserImpl.ParseScript,
                         CheckAttribsImpl.CheckStyle),
                new Dict("script", HtmlVersion.From32 | HtmlVersion.Frames,
                         ContentModel.HEAD | ContentModel.MIXED | ContentModel.BLOCK | ContentModel.INLINE,
                         ParserImpl.ParseScript, CheckAttribsImpl.CheckScript),
                new Dict("server", HtmlVersion.Netscape,
                         ContentModel.HEAD | ContentModel.MIXED | ContentModel.BLOCK | ContentModel.INLINE,
                         ParserImpl.ParseScript, null),
                new Dict("body", HtmlVersion.All, ContentModel.HTML | ContentModel.OPT | ContentModel.OMIT_ST,
                         ParserImpl.ParseBody, null),
                new Dict("frameset", HtmlVersion.Frames, ContentModel.HTML | ContentModel.FRAMES,
                         ParserImpl.ParseFrameSet, null),
                new Dict("p", HtmlVersion.All, ContentModel.BLOCK | ContentModel.OPT, ParserImpl.ParseInline, null),
                new Dict("h1", HtmlVersion.All, ContentModel.BLOCK | ContentModel.HEADING, ParserImpl.ParseInline, null)
                ,
                new Dict("h2", HtmlVersion.All, ContentModel.BLOCK | ContentModel.HEADING, ParserImpl.ParseInline, null)
                ,
                new Dict("h3", HtmlVersion.All, ContentModel.BLOCK | ContentModel.HEADING, ParserImpl.ParseInline, null)
                ,
                new Dict("h4", HtmlVersion.All, ContentModel.BLOCK | ContentModel.HEADING, ParserImpl.ParseInline, null)
                ,
                new Dict("h5", HtmlVersion.All, ContentModel.BLOCK | ContentModel.HEADING, ParserImpl.ParseInline, null)
                ,
                new Dict("h6", HtmlVersion.All, ContentModel.BLOCK | ContentModel.HEADING, ParserImpl.ParseInline, null)
                ,
                new Dict("ul", HtmlVersion.All, ContentModel.BLOCK, ParserImpl.ParseList, null),
                new Dict("ol", HtmlVersion.All, ContentModel.BLOCK, ParserImpl.ParseList, null),
                new Dict("dl", HtmlVersion.All, ContentModel.BLOCK, ParserImpl.ParseDefList, null),
                new Dict("dir", HtmlVersion.Loose, ContentModel.BLOCK | ContentModel.OBSOLETE, ParserImpl.ParseList,
                         null),
                new Dict("menu", HtmlVersion.Loose, ContentModel.BLOCK | ContentModel.OBSOLETE, ParserImpl.ParseList,
                         null),
                new Dict("pre", HtmlVersion.All, ContentModel.BLOCK, ParserImpl.ParsePre, null),
                new Dict("listing", HtmlVersion.All, ContentModel.BLOCK | ContentModel.OBSOLETE, ParserImpl.ParsePre,
                         null),
                new Dict("xmp", HtmlVersion.All, ContentModel.BLOCK | ContentModel.OBSOLETE, ParserImpl.ParsePre, null),
                new Dict("plaintext", HtmlVersion.All, ContentModel.BLOCK | ContentModel.OBSOLETE, ParserImpl.ParsePre,
                         null),
                new Dict("address", HtmlVersion.All, ContentModel.BLOCK, ParserImpl.ParseBlock, null),
                new Dict("blockquote", HtmlVersion.All, ContentModel.BLOCK, ParserImpl.ParseBlock, null),
                new Dict("form", HtmlVersion.All, ContentModel.BLOCK, ParserImpl.ParseBlock, null),
                new Dict("isindex", HtmlVersion.Loose, ContentModel.BLOCK | ContentModel.EMPTY, null, null),
                new Dict("fieldset", HtmlVersion.Html40, ContentModel.BLOCK, ParserImpl.ParseBlock, null),
                new Dict("table", HtmlVersion.From32, ContentModel.BLOCK, ParserImpl.ParseTableTag,
                         CheckAttribsImpl.CheckTable),
                new Dict("hr", HtmlVersion.All, ContentModel.BLOCK | ContentModel.EMPTY, null, CheckAttribsImpl.CheckHr)
                ,
                new Dict("div", HtmlVersion.From32, ContentModel.BLOCK, ParserImpl.ParseBlock, null),
                new Dict("multicol", HtmlVersion.Netscape, ContentModel.BLOCK, ParserImpl.ParseBlock, null),
                new Dict("nosave", HtmlVersion.Netscape, ContentModel.BLOCK, ParserImpl.ParseBlock, null),
                new Dict("layer", HtmlVersion.Netscape, ContentModel.BLOCK, ParserImpl.ParseBlock, null),
                new Dict("ilayer", HtmlVersion.Netscape, ContentModel.INLINE, ParserImpl.ParseInline, null),
                new Dict("nolayer", HtmlVersion.Netscape, ContentModel.BLOCK | ContentModel.INLINE | ContentModel.MIXED,
                         ParserImpl.ParseBlock, null),
                new Dict("align", HtmlVersion.Netscape, ContentModel.BLOCK, ParserImpl.ParseBlock, null),
                new Dict("center", HtmlVersion.Loose, ContentModel.BLOCK, ParserImpl.ParseBlock, null),
                new Dict("ins", HtmlVersion.Html40, ContentModel.INLINE | ContentModel.BLOCK | ContentModel.MIXED,
                         ParserImpl.ParseInline, null),
                new Dict("del", HtmlVersion.Html40, ContentModel.INLINE | ContentModel.BLOCK | ContentModel.MIXED,
                         ParserImpl.ParseInline, null),
                new Dict("li", HtmlVersion.All, ContentModel.LIST | ContentModel.OPT | ContentModel.NO_INDENT,
                         ParserImpl.ParseBlock, null),
                new Dict("dt", HtmlVersion.All, ContentModel.DEFLIST | ContentModel.OPT | ContentModel.NO_INDENT,
                         ParserImpl.ParseInline, null),
                new Dict("dd", HtmlVersion.All, ContentModel.DEFLIST | ContentModel.OPT | ContentModel.NO_INDENT,
                         ParserImpl.ParseBlock, null),
                new Dict("caption", HtmlVersion.From32, ContentModel.TABLE, ParserImpl.ParseInline,
                         CheckAttribsImpl.CheckCaption),
                new Dict("colgroup", HtmlVersion.Html40, ContentModel.TABLE | ContentModel.OPT, ParserImpl.ParseColGroup,
                         null),
                new Dict("col", HtmlVersion.Html40, ContentModel.TABLE | ContentModel.EMPTY, null, null),
                new Dict("thead", HtmlVersion.Html40, ContentModel.TABLE | ContentModel.ROWGRP | ContentModel.OPT,
                         ParserImpl.ParseRowGroup, null),
                new Dict("tfoot", HtmlVersion.Html40, ContentModel.TABLE | ContentModel.ROWGRP | ContentModel.OPT,
                         ParserImpl.ParseRowGroup, null),
                new Dict("tbody", HtmlVersion.Html40, ContentModel.TABLE | ContentModel.ROWGRP | ContentModel.OPT,
                         ParserImpl.ParseRowGroup, null),
                new Dict("tr", HtmlVersion.From32, ContentModel.TABLE | ContentModel.OPT, ParserImpl.ParseRow, null),
                new Dict("td", HtmlVersion.From32, ContentModel.ROW | ContentModel.OPT | ContentModel.NO_INDENT,
                         ParserImpl.ParseBlock, CheckAttribsImpl.CheckTableCell),
                new Dict("th", HtmlVersion.From32, ContentModel.ROW | ContentModel.OPT | ContentModel.NO_INDENT,
                         ParserImpl.ParseBlock, CheckAttribsImpl.CheckTableCell),
                new Dict("q", HtmlVersion.Html40, ContentModel.INLINE, ParserImpl.ParseInline, null),
                new Dict("a", HtmlVersion.All, ContentModel.INLINE, ParserImpl.ParseInline, CheckAttribsImpl.CheckAnchor)
                ,
                new Dict("br", HtmlVersion.All, ContentModel.INLINE | ContentModel.EMPTY, null, null),
                new Dict("img", HtmlVersion.All, ContentModel.INLINE | ContentModel.IMG | ContentModel.EMPTY, null,
                         CheckAttribsImpl.CheckImg),
                new Dict("object", HtmlVersion.Html40,
                         ContentModel.OBJECT | ContentModel.HEAD | ContentModel.IMG | ContentModel.INLINE |
                         ContentModel.PARAM, ParserImpl.ParseBlock, null),
                new Dict("applet", HtmlVersion.Loose,
                         ContentModel.OBJECT | ContentModel.IMG | ContentModel.INLINE | ContentModel.PARAM,
                         ParserImpl.ParseBlock, null),
                new Dict("servlet", HtmlVersion.Sun,
                         ContentModel.OBJECT | ContentModel.IMG | ContentModel.INLINE | ContentModel.PARAM,
                         ParserImpl.ParseBlock, null),
                new Dict("param", HtmlVersion.From32, ContentModel.INLINE | ContentModel.EMPTY, null, null),
                new Dict("embed", HtmlVersion.Netscape, ContentModel.INLINE | ContentModel.IMG | ContentModel.EMPTY,
                         null, null),
                new Dict("noembed", HtmlVersion.Netscape, ContentModel.INLINE, ParserImpl.ParseInline, null),
                new Dict("iframe", HtmlVersion.Html40Loose, ContentModel.INLINE, ParserImpl.ParseBlock, null),
                new Dict("frame", HtmlVersion.Frames, ContentModel.FRAMES | ContentModel.EMPTY, null, null),
                new Dict("noframes", HtmlVersion.Iframes, ContentModel.BLOCK | ContentModel.FRAMES,
                         ParserImpl.ParseNoFrames, null),
                new Dict("noscript", HtmlVersion.Frames | HtmlVersion.Html40,
                         ContentModel.BLOCK | ContentModel.INLINE | ContentModel.MIXED, ParserImpl.ParseBlock, null),
                new Dict("b", HtmlVersion.All, ContentModel.INLINE, ParserImpl.ParseInline, null),
                new Dict("i", HtmlVersion.All, ContentModel.INLINE, ParserImpl.ParseInline, null),
                new Dict("u", HtmlVersion.Loose, ContentModel.INLINE, ParserImpl.ParseInline, null),
                new Dict("tt", HtmlVersion.All, ContentModel.INLINE, ParserImpl.ParseInline, null),
                new Dict("s", HtmlVersion.Loose, ContentModel.INLINE, ParserImpl.ParseInline, null),
                new Dict("strike", HtmlVersion.Loose, ContentModel.INLINE, ParserImpl.ParseInline, null),
                new Dict("big", HtmlVersion.From32, ContentModel.INLINE, ParserImpl.ParseInline, null),
                new Dict("small", HtmlVersion.From32, ContentModel.INLINE, ParserImpl.ParseInline, null),
                new Dict("sub", HtmlVersion.From32, ContentModel.INLINE, ParserImpl.ParseInline, null),
                new Dict("sup", HtmlVersion.From32, ContentModel.INLINE, ParserImpl.ParseInline, null),
                new Dict("em", HtmlVersion.All, ContentModel.INLINE, ParserImpl.ParseInline, null),
                new Dict("strong", HtmlVersion.All, ContentModel.INLINE, ParserImpl.ParseInline, null),
                new Dict("dfn", HtmlVersion.All, ContentModel.INLINE, ParserImpl.ParseInline, null),
                new Dict("code", HtmlVersion.All, ContentModel.INLINE, ParserImpl.ParseInline, null),
                new Dict("samp", HtmlVersion.All, ContentModel.INLINE, ParserImpl.ParseInline, null),
                new Dict("kbd", HtmlVersion.All, ContentModel.INLINE, ParserImpl.ParseInline, null),
                new Dict("var", HtmlVersion.All, ContentModel.INLINE, ParserImpl.ParseInline, null),
                new Dict("cite", HtmlVersion.All, ContentModel.INLINE, ParserImpl.ParseInline, null),
                new Dict("abbr", HtmlVersion.Html40, ContentModel.INLINE, ParserImpl.ParseInline, null),
                new Dict("acronym", HtmlVersion.Html40, ContentModel.INLINE, ParserImpl.ParseInline, null),
                new Dict("span", HtmlVersion.From32, ContentModel.INLINE, ParserImpl.ParseInline, null),
                new Dict("blink", HtmlVersion.Proprietary, ContentModel.INLINE, ParserImpl.ParseInline, null),
                new Dict("nobr", HtmlVersion.Proprietary, ContentModel.INLINE, ParserImpl.ParseInline, null),
                new Dict("wbr", HtmlVersion.Proprietary, ContentModel.INLINE | ContentModel.EMPTY, null, null),
                new Dict("marquee", HtmlVersion.Microsoft, ContentModel.INLINE | ContentModel.OPT,
                         ParserImpl.ParseInline, null),
                new Dict("bgsound", HtmlVersion.Microsoft, ContentModel.HEAD | ContentModel.EMPTY, null, null),
                new Dict("comment", HtmlVersion.Microsoft, ContentModel.INLINE, ParserImpl.ParseInline, null),
                new Dict("spacer", HtmlVersion.Netscape, ContentModel.INLINE | ContentModel.EMPTY, null, null),
                new Dict("keygen", HtmlVersion.Netscape, ContentModel.INLINE | ContentModel.EMPTY, null, null),
                new Dict("nolayer", HtmlVersion.Netscape, ContentModel.BLOCK | ContentModel.INLINE | ContentModel.MIXED,
                         ParserImpl.ParseBlock, null),
                new Dict("ilayer", HtmlVersion.Netscape, ContentModel.INLINE, ParserImpl.ParseInline, null),
                new Dict("map", HtmlVersion.From32, ContentModel.INLINE, ParserImpl.ParseBlock,
                         CheckAttribsImpl.CheckMap),
                new Dict("area", HtmlVersion.All, ContentModel.BLOCK | ContentModel.EMPTY, null,
                         CheckAttribsImpl.CheckArea),
                new Dict("input", HtmlVersion.All, ContentModel.INLINE | ContentModel.IMG | ContentModel.EMPTY, null,
                         null),
                new Dict("select", HtmlVersion.All, ContentModel.INLINE | ContentModel.FIELD, ParserImpl.ParseSelect,
                         null),
                new Dict("option", HtmlVersion.All, ContentModel.FIELD | ContentModel.OPT, ParserImpl.ParseText, null),
                new Dict("optgroup", HtmlVersion.Html40, ContentModel.FIELD | ContentModel.OPT, ParserImpl.ParseOptGroup,
                         null),
                new Dict("textarea", HtmlVersion.All, ContentModel.INLINE | ContentModel.FIELD, ParserImpl.ParseText,
                         null),
                new Dict("label", HtmlVersion.Html40, ContentModel.INLINE, ParserImpl.ParseInline, null),
                new Dict("legend", HtmlVersion.Html40, ContentModel.INLINE, ParserImpl.ParseInline, null),
                new Dict("button", HtmlVersion.Html40, ContentModel.INLINE, ParserImpl.ParseInline, null),
                new Dict("basefont", HtmlVersion.Loose, ContentModel.INLINE | ContentModel.EMPTY, null, null),
                new Dict("font", HtmlVersion.Loose, ContentModel.INLINE, ParserImpl.ParseInline, null),
                new Dict("bdo", HtmlVersion.Html40, ContentModel.INLINE, ParserImpl.ParseInline, null)
            };

        private readonly Dictionary<object, object> _tagHashtable = new Dictionary<object, object>();
        public Dict TagA = null;
        public Dict TagApplet = null;
        public Dict TagArea = null;
        public Dict TagB = null;
        public Dict TagBase = null;
        public Dict TagBig = null;
        public Dict TagBlockquote = null;

        public Dict TagBody = null;
        public Dict TagBr = null;
        public Dict TagCaption = null;
        public Dict TagCenter = null;
        public Dict TagCol = null;
        public Dict TagDd = null;
        public Dict TagDir = null;
        public Dict TagDiv = null;
        public Dict TagDl = null;
        public Dict TagDt = null;
        public Dict TagEm = null;
        public Dict TagFont = null;
        public Dict TagForm = null;
        public Dict TagFrame = null;
        public Dict TagFrameset = null;
        public Dict TagH1 = null;
        public Dict TagH2 = null;
        public Dict TagHead = null;
        public Dict TagHr = null;
        public Dict TagHtml = null;
        public Dict TagI = null;
        public Dict TagImg = null;
        public Dict TagLayer = null;
        public Dict TagLi = null;
        public Dict TagLink = null;
        public Dict TagListing = null;
        public Dict TagMap = null;
        public Dict TagMeta = null;
        public Dict TagNobr = null;
        public Dict TagNoframes = null;
        public Dict TagNoscript = null;
        public Dict TagObject = null;
        public Dict TagOl = null;
        public Dict TagOptgroup = null;
        public Dict TagOption = null;
        public Dict TagP = null;
        public Dict TagParam = null;
        public Dict TagPre = null;
        public Dict TagScript = null;
        public Dict TagSmall = null;
        public Dict TagSpacer = null;
        public Dict TagSpan = null;
        public Dict TagStrong = null;
        public Dict TagStyle = null;
        public Dict TagTable = null;
        public Dict TagTd = null;
        public Dict TagTextarea = null;
        public Dict TagTh = null;
        public Dict TagTitle = null;
        public Dict TagTr = null;
        public Dict TagUl = null;
        public Dict TagWbr = null;
        public Dict XmlTags = new Dict(null, HtmlVersion.All, ContentModel.BLOCK, null, null);

        public TagCollection()
        {
            foreach (Dict tag in Tags)
                Add(tag);

            TagHtml = Lookup("html");
            TagHead = Lookup("head");
            TagBody = Lookup("body");
            TagFrameset = Lookup("frameset");
            TagFrame = Lookup("frame");
            TagNoframes = Lookup("noframes");
            TagMeta = Lookup("meta");
            TagTitle = Lookup("title");
            TagBase = Lookup("base");
            TagHr = Lookup("hr");
            TagPre = Lookup("pre");
            TagListing = Lookup("listing");
            TagH1 = Lookup("h1");
            TagH2 = Lookup("h2");
            TagP = Lookup("p");
            TagUl = Lookup("ul");
            TagOl = Lookup("ol");
            TagDir = Lookup("dir");
            TagLi = Lookup("li");
            TagDt = Lookup("dt");
            TagDd = Lookup("dd");
            TagDl = Lookup("dl");
            TagTd = Lookup("td");
            TagTh = Lookup("th");
            TagTr = Lookup("tr");
            TagCol = Lookup("col");
            TagBr = Lookup("br");
            TagA = Lookup("a");
            TagLink = Lookup("link");
            TagB = Lookup("b");
            TagI = Lookup("i");
            TagStrong = Lookup("strong");
            TagEm = Lookup("em");
            TagBig = Lookup("big");
            TagSmall = Lookup("small");
            TagParam = Lookup("param");
            TagOption = Lookup("option");
            TagOptgroup = Lookup("optgroup");
            TagImg = Lookup("img");
            TagMap = Lookup("map");
            TagArea = Lookup("area");
            TagNobr = Lookup("nobr");
            TagWbr = Lookup("wbr");
            TagFont = Lookup("font");
            TagSpacer = Lookup("spacer");
            TagLayer = Lookup("layer");
            TagCenter = Lookup("center");
            TagStyle = Lookup("style");
            TagScript = Lookup("script");
            TagNoscript = Lookup("noscript");
            TagTable = Lookup("table");
            TagCaption = Lookup("caption");
            TagForm = Lookup("form");
            TagTextarea = Lookup("textarea");
            TagBlockquote = Lookup("blockquote");
            TagApplet = Lookup("applet");
            TagObject = Lookup("object");
            TagDiv = Lookup("div");
            TagSpan = Lookup("span");
        }

        public TidyOptions Options { get; set; }

        public Dict Lookup(string name)
        {
            return (Dict) _tagHashtable[name];
        }

        public Dict Add(Dict dict)
        {
            Dict d = _tagHashtable.ContainsKey(dict.Name) ? (Dict) _tagHashtable[dict.Name] : null;
            if (d != null)
            {
                d.Versions = dict.Versions;
                d.Model |= dict.Model;
                d.Parser = dict.Parser;
                d.CheckAttribs = dict.CheckAttribs;
                return d;
            }
            _tagHashtable[dict.Name] = dict;
            return dict;
        }

        /* public method for finding tag by name */

        public bool FindTag(Node node)
        {
            if (Options != null && Options.XmlTags)
            {
                node.Tag = XmlTags;
                return true;
            }

            if (node.Element != null)
            {
                Dict np = Lookup(node.Element);
                if (np != null)
                {
                    node.Tag = np;
                    return true;
                }
            }

            return false;
        }

        public IParser FindParser(Node node)
        {
            if (node.Element != null)
            {
                Dict np = Lookup(node.Element);
                if (np != null)
                {
                    return np.Parser;
                }
            }

            return null;
        }

        public void DefineInlineTag(string name)
        {
            Add(new Dict(name, HtmlVersion.Proprietary,
                         (ContentModel.INLINE | ContentModel.NO_INDENT | ContentModel.NEW),
                         ParserImpl.ParseBlock, null));
        }

        public void DefineBlockTag(string name)
        {
            Add(new Dict(name, HtmlVersion.Proprietary, (ContentModel.BLOCK | ContentModel.NO_INDENT | ContentModel.NEW),
                         ParserImpl.ParseBlock, null));
        }

        public void DefineEmptyTag(string name)
        {
            Add(new Dict(name, HtmlVersion.Proprietary, (ContentModel.EMPTY | ContentModel.NO_INDENT | ContentModel.NEW),
                         ParserImpl.ParseBlock, null));
        }

        public void DefinePreTag(string name)
        {
            Add(new Dict(name, HtmlVersion.Proprietary, (ContentModel.BLOCK | ContentModel.NO_INDENT | ContentModel.NEW),
                         ParserImpl.ParsePre, null));
        }
    }
}