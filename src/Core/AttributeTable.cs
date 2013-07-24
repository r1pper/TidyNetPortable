using System.Collections.Generic;

namespace Tidy.Core
{
    /// <summary>
    ///     HTML attribute hash table
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
    internal class AttributeTable
    {
        private static AttributeTable _defaultAttributeTable;

        private static readonly Attribute[] Attrs = new[]
            {
                new Attribute("abbr", HtmlVersion.Html40, null),
                new Attribute("accept-charset", HtmlVersion.Html40, null),
                new Attribute("accept", HtmlVersion.All, null),
                new Attribute("accesskey", HtmlVersion.Html40, null),
                new Attribute("action", HtmlVersion.All, AttrCheckImpl.CheckUrl),
                new Attribute("add_date", HtmlVersion.Netscape, null),
                new Attribute("align", HtmlVersion.All, AttrCheckImpl.CheckAlign),
                new Attribute("alink", HtmlVersion.Loose, null),
                new Attribute("alt", HtmlVersion.All, null),
                new Attribute("archive", HtmlVersion.Html40, null),
                new Attribute("axis", HtmlVersion.Html40, null),
                new Attribute("background", HtmlVersion.Loose, AttrCheckImpl.CheckUrl),
                new Attribute("bgcolor", HtmlVersion.Loose, null),
                new Attribute("bgproperties", HtmlVersion.Proprietary, null),
                new Attribute("border", HtmlVersion.All, AttrCheckImpl.CheckBool),
                new Attribute("bordercolor", HtmlVersion.Microsoft, null),
                new Attribute("bottommargin", HtmlVersion.Microsoft, null),
                new Attribute("cellpadding", HtmlVersion.From32, null),
                new Attribute("cellspacing", HtmlVersion.From32, null),
                new Attribute("char", HtmlVersion.Html40, null),
                new Attribute("charoff", HtmlVersion.Html40, null),
                new Attribute("charset", HtmlVersion.Html40, null),
                new Attribute("checked", HtmlVersion.All, AttrCheckImpl.CheckBool),
                new Attribute("cite", HtmlVersion.Html40, AttrCheckImpl.CheckUrl),
                new Attribute("class", HtmlVersion.Html40, null),
                new Attribute("classid", HtmlVersion.Html40, AttrCheckImpl.CheckUrl),
                new Attribute("clear", HtmlVersion.Loose, null),
                new Attribute("code", HtmlVersion.Loose, null),
                new Attribute("codebase", HtmlVersion.Html40, AttrCheckImpl.CheckUrl),
                new Attribute("codetype", HtmlVersion.Html40, null),
                new Attribute("color", HtmlVersion.Loose, null),
                new Attribute("cols", HtmlVersion.Iframes, null),
                new Attribute("colspan", HtmlVersion.From32, null),
                new Attribute("compact", HtmlVersion.All, AttrCheckImpl.CheckBool),
                new Attribute("content", HtmlVersion.All, null),
                new Attribute("coords", HtmlVersion.From32, null),
                new Attribute("data", HtmlVersion.Html40, AttrCheckImpl.CheckUrl),
                new Attribute("datafld", HtmlVersion.Microsoft, null),
                new Attribute("dataformatas", HtmlVersion.Microsoft, null),
                new Attribute("datapagesize", HtmlVersion.Microsoft, null),
                new Attribute("datasrc", HtmlVersion.Microsoft, AttrCheckImpl.CheckUrl),
                new Attribute("datetime", HtmlVersion.Html40, null),
                new Attribute("declare", HtmlVersion.Html40, AttrCheckImpl.CheckBool),
                new Attribute("defer", HtmlVersion.Html40, AttrCheckImpl.CheckBool),
                new Attribute("dir", HtmlVersion.Html40, null),
                new Attribute("disabled", HtmlVersion.Html40, AttrCheckImpl.CheckBool),
                new Attribute("enctype", HtmlVersion.All, null),
                new Attribute("face", HtmlVersion.Loose, null),
                new Attribute("for", HtmlVersion.Html40, null),
                new Attribute("frame", HtmlVersion.Html40, null),
                new Attribute("frameborder", HtmlVersion.Frames, null),
                new Attribute("framespacing", HtmlVersion.Proprietary, null),
                new Attribute("gridx", HtmlVersion.Proprietary, null),
                new Attribute("gridy", HtmlVersion.Proprietary, null),
                new Attribute("headers", HtmlVersion.Html40, null),
                new Attribute("height", HtmlVersion.All, null),
                new Attribute("href", HtmlVersion.All, AttrCheckImpl.CheckUrl),
                new Attribute("hreflang", HtmlVersion.Html40, null),
                new Attribute("hspace", HtmlVersion.All, null),
                new Attribute("http-equiv", HtmlVersion.All, null),
                new Attribute("id", HtmlVersion.Html40, AttrCheckImpl.CheckId),
                new Attribute("ismap", HtmlVersion.All, AttrCheckImpl.CheckBool),
                new Attribute("label", HtmlVersion.Html40, null),
                new Attribute("lang", HtmlVersion.Html40, null),
                new Attribute("language", HtmlVersion.Loose, null),
                new Attribute("last_modified", HtmlVersion.Netscape, null),
                new Attribute("last_visit", HtmlVersion.Netscape, null),
                new Attribute("leftmargin", HtmlVersion.Microsoft, null),
                new Attribute("link", HtmlVersion.Loose, null),
                new Attribute("longdesc", HtmlVersion.Html40, AttrCheckImpl.CheckUrl),
                new Attribute("lowsrc", HtmlVersion.Proprietary, AttrCheckImpl.CheckUrl),
                new Attribute("marginheight", HtmlVersion.Iframes, null),
                new Attribute("marginwidth", HtmlVersion.Iframes, null),
                new Attribute("maxlength", HtmlVersion.All, null),
                new Attribute("media", HtmlVersion.Html40, null),
                new Attribute("method", HtmlVersion.All, null),
                new Attribute("multiple", HtmlVersion.All, AttrCheckImpl.CheckBool),
                new Attribute("name", HtmlVersion.All, AttrCheckImpl.CheckName),
                new Attribute("nohref", HtmlVersion.From32, AttrCheckImpl.CheckBool),
                new Attribute("noresize", HtmlVersion.Frames, AttrCheckImpl.CheckBool),
                new Attribute("noshade", HtmlVersion.Loose, AttrCheckImpl.CheckBool),
                new Attribute("nowrap", HtmlVersion.Loose, AttrCheckImpl.CheckBool),
                new Attribute("object", HtmlVersion.Html40Loose, null),
                new Attribute("onblur", HtmlVersion.Html40, AttrCheckImpl.CheckScript),
                new Attribute("onchange", HtmlVersion.Html40, AttrCheckImpl.CheckScript),
                new Attribute("onclick", HtmlVersion.Html40, AttrCheckImpl.CheckScript),
                new Attribute("ondblclick", HtmlVersion.Html40, AttrCheckImpl.CheckScript),
                new Attribute("onkeydown", HtmlVersion.Html40, AttrCheckImpl.CheckScript),
                new Attribute("onkeypress", HtmlVersion.Html40, AttrCheckImpl.CheckScript),
                new Attribute("onkeyup", HtmlVersion.Html40, AttrCheckImpl.CheckScript),
                new Attribute("onload", HtmlVersion.Html40, AttrCheckImpl.CheckScript),
                new Attribute("onmousedown", HtmlVersion.Html40, AttrCheckImpl.CheckScript),
                new Attribute("onmousemove", HtmlVersion.Html40, AttrCheckImpl.CheckScript),
                new Attribute("onmouseout", HtmlVersion.Html40, AttrCheckImpl.CheckScript),
                new Attribute("onmouseover", HtmlVersion.Html40, AttrCheckImpl.CheckScript),
                new Attribute("onmouseup", HtmlVersion.Html40, AttrCheckImpl.CheckScript),
                new Attribute("onsubmit", HtmlVersion.Html40, AttrCheckImpl.CheckScript),
                new Attribute("onreset", HtmlVersion.Html40, AttrCheckImpl.CheckScript),
                new Attribute("onselect", HtmlVersion.Html40, AttrCheckImpl.CheckScript),
                new Attribute("onunload", HtmlVersion.Html40, AttrCheckImpl.CheckScript),
                new Attribute("onafterupdate", HtmlVersion.Microsoft, AttrCheckImpl.CheckScript),
                new Attribute("onbeforeupdate", HtmlVersion.Microsoft, AttrCheckImpl.CheckScript),
                new Attribute("onerrorupdate", HtmlVersion.Microsoft, AttrCheckImpl.CheckScript),
                new Attribute("onrowenter", HtmlVersion.Microsoft, AttrCheckImpl.CheckScript),
                new Attribute("onrowexit", HtmlVersion.Microsoft, AttrCheckImpl.CheckScript),
                new Attribute("onbeforeunload", HtmlVersion.Microsoft, AttrCheckImpl.CheckScript),
                new Attribute("ondatasetchanged", HtmlVersion.Microsoft, AttrCheckImpl.CheckScript),
                new Attribute("ondataavailable", HtmlVersion.Microsoft, AttrCheckImpl.CheckScript),
                new Attribute("ondatasetcomplete", HtmlVersion.Microsoft, AttrCheckImpl.CheckScript),
                new Attribute("profile", HtmlVersion.Html40, AttrCheckImpl.CheckUrl),
                new Attribute("prompt", HtmlVersion.Loose, null),
                new Attribute("readonly", HtmlVersion.Html40, AttrCheckImpl.CheckBool),
                new Attribute("rel", HtmlVersion.All, null),
                new Attribute("rev", HtmlVersion.All, null),
                new Attribute("rightmargin", HtmlVersion.Microsoft, null),
                new Attribute("rows", HtmlVersion.All, null),
                new Attribute("rowspan", HtmlVersion.All, null),
                new Attribute("rules", HtmlVersion.Html40, null),
                new Attribute("scheme", HtmlVersion.Html40, null),
                new Attribute("scope", HtmlVersion.Html40, null),
                new Attribute("scrolling", HtmlVersion.Iframes, null),
                new Attribute("selected", HtmlVersion.All, AttrCheckImpl.CheckBool),
                new Attribute("shape", HtmlVersion.From32, null),
                new Attribute("showgrid", HtmlVersion.Proprietary, AttrCheckImpl.CheckBool),
                new Attribute("showgridx", HtmlVersion.Proprietary, AttrCheckImpl.CheckBool),
                new Attribute("showgridy", HtmlVersion.Proprietary, AttrCheckImpl.CheckBool),
                new Attribute("size", HtmlVersion.Loose, null),
                new Attribute("span", HtmlVersion.Html40, null),
                new Attribute("src", HtmlVersion.All | HtmlVersion.Frames, AttrCheckImpl.CheckUrl),
                new Attribute("standby", HtmlVersion.Html40, null),
                new Attribute("start", HtmlVersion.All, null),
                new Attribute("style", HtmlVersion.Html40, null),
                new Attribute("summary", HtmlVersion.Html40, null),
                new Attribute("tabindex", HtmlVersion.Html40, null),
                new Attribute("target", HtmlVersion.Html40, null),
                new Attribute("text", HtmlVersion.Loose, null),
                new Attribute("title", HtmlVersion.Html40, null),
                new Attribute("topmargin", HtmlVersion.Microsoft, null),
                new Attribute("type", HtmlVersion.From32, null),
                new Attribute("usemap", HtmlVersion.All, AttrCheckImpl.CheckBool),
                new Attribute("valign", HtmlVersion.From32, AttrCheckImpl.CheckValign),
                new Attribute("value", HtmlVersion.All, null),
                new Attribute("valuetype", HtmlVersion.Html40, null),
                new Attribute("version", HtmlVersion.All, null),
                new Attribute("vlink", HtmlVersion.Loose, null),
                new Attribute("vspace", HtmlVersion.Loose, null),
                new Attribute("width", HtmlVersion.All, null),
                new Attribute("wrap", HtmlVersion.Netscape, null),
                new Attribute("xml:lang", HtmlVersion.Xml, null),
                new Attribute("xmlns", HtmlVersion.All, null)
            };

        private readonly Dictionary<object, object> _attributeHashtable = new Dictionary<object, object>();

        public static AttributeTable DefaultAttributeTable
        {
            get
            {
                if (_defaultAttributeTable == null)
                {
                    _defaultAttributeTable = new AttributeTable();
                    foreach (Attribute t in Attrs)
                        _defaultAttributeTable.Install(t);

                    AttrHref = _defaultAttributeTable.Lookup("href");
                    AttrSrc = _defaultAttributeTable.Lookup("src");
                    AttrId = _defaultAttributeTable.Lookup("id");
                    AttrName = _defaultAttributeTable.Lookup("name");
                    AttrSummary = _defaultAttributeTable.Lookup("summary");
                    AttrAlt = _defaultAttributeTable.Lookup("alt");
                    AttrLongdesc = _defaultAttributeTable.Lookup("longdesc");
                    AttrUsemap = _defaultAttributeTable.Lookup("usemap");
                    AttrIsmap = _defaultAttributeTable.Lookup("ismap");
                    AttrLanguage = _defaultAttributeTable.Lookup("language");
                    AttrType = _defaultAttributeTable.Lookup("type");
                    AttrTitle = _defaultAttributeTable.Lookup("title");
                    AttrXmlns = _defaultAttributeTable.Lookup("xmlns");
                    AttrValue = _defaultAttributeTable.Lookup("value");
                    AttrContent = _defaultAttributeTable.Lookup("content");
                    AttrDatafld = _defaultAttributeTable.Lookup("datafld");
                    AttrWidth = _defaultAttributeTable.Lookup("width");
                    AttrHeight = _defaultAttributeTable.Lookup("height");

                    AttrAlt.Nowrap = true;
                    AttrValue.Nowrap = true;
                    AttrContent.Nowrap = true;
                }

                return _defaultAttributeTable;
            }
        }

        public static Attribute AttrHref { get; private set; }
        public static Attribute AttrSrc { get; private set; }
        public static Attribute AttrId { get; private set; }
        public static Attribute AttrName { get; private set; }
        public static Attribute AttrSummary { get; private set; }
        public static Attribute AttrAlt { get; private set; }
        public static Attribute AttrLongdesc { get; private set; }
        public static Attribute AttrUsemap { get; private set; }
        public static Attribute AttrIsmap { get; private set; }
        public static Attribute AttrLanguage { get; private set; }
        public static Attribute AttrType { get; private set; }
        public static Attribute AttrTitle { get; private set; }
        public static Attribute AttrXmlns { get; private set; }
        public static Attribute AttrValue { get; private set; }
        public static Attribute AttrContent { get; private set; }
        public static Attribute AttrDatafld { get; private set; }
        public static Attribute AttrWidth { get; private set; }
        public static Attribute AttrHeight { get; private set; }

        public virtual Attribute Lookup(string name)
        {
            return (Attribute) _attributeHashtable[name];
        }

        public virtual Attribute Install(Attribute attr)
        {
            object element = _attributeHashtable.ContainsKey(attr.Name) ? _attributeHashtable[attr.Name] : null;
            _attributeHashtable[attr.Name] = attr;
            return (Attribute) element;
        }

        public virtual Attribute FindAttribute(AttVal attval)
        {
            if (attval.Attribute != null)
            {
                Attribute np = Lookup(attval.Attribute);
                return np;
            }

            return null;
        }

        public virtual bool IsUrl(string attrname)
        {
            Attribute np = Lookup(attrname);
            return (np != null && np.AttrCheck == AttrCheckImpl.CheckUrl);
        }

        public virtual bool IsScript(string attrname)
        {
            Attribute np = Lookup(attrname);
            return (np != null && np.AttrCheck == AttrCheckImpl.CheckScript);
        }

        public virtual bool IsLiteralAttribute(string attrname)
        {
            Attribute np = Lookup(attrname);
            return (np != null && np.Literal);
        }

        /*
		Henry Zrepa reports that some folk are
		using embed with script attributes where
		newlines are signficant. These need to be
		declared and handled specially!
		*/

        public virtual void DeclareLiteralAttrib(string name)
        {
            Attribute attrib = Lookup(name) ?? Install(new Attribute(name, HtmlVersion.Proprietary, null));

            attrib.Literal = true;
        }
    }
}