using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Text;
using Org.Xml.Sax;

namespace BananaMailBoard.Util
{
    class HtmlConverter
    {
        class HtmlToTextTagHandler : Java.Lang.Object, Html.ITagHandler
        {
            HashSet<string> TAGS_WITH_IGNORED_CONTENT = new HashSet<string>()
            {
                "style", "script", "title", "!"
            };

            public void HandleTag(bool opening, string tag, IEditable output, IXMLReader xmlReader)
            {
                tag = tag.ToLowerInvariant();
                if (tag == "hr" && opening)
                {
                    output.Append("---------------------------------------------\r\n");
                }
                else if (TAGS_WITH_IGNORED_CONTENT.Contains(tag))
                {
                    HandleIgnoredTag(opening, output);
                }
            }

            private void HandleIgnoredTag(bool opening, IEditable output)
            {
                const string IGNORED_ANNOTATION_KEY = "DUMMY_KEY";
                const string IGNORED_ANNOTATION_VALUE = "DUMMY_VALUE";

                var len = output.Length();
                if (opening)
                {
                    output.SetSpan(new Annotation(IGNORED_ANNOTATION_KEY, IGNORED_ANNOTATION_VALUE), len, len, SpanTypes.MarkMark);
                }
                else
                {
                    var start = 
                        output.GetSpans(0, output.Length(), Java.Lang.Class.FromType(typeof(Annotation)))
                            .Cast<Annotation>()
                            .Where(a => output.GetSpanFlags(a) == SpanTypes.MarkMark)
                            .Where(a => a.Key == IGNORED_ANNOTATION_KEY)
                            .Where(a => a.Value == IGNORED_ANNOTATION_VALUE)
                            .FirstOrDefault();
                    if (start != null)
                    {
                        var where = output.GetSpanStart(start);
                        output.RemoveSpan(start);
                        output.Delete(where, len);
                    }
                }
            }
        }

        public static string HtmlToText(string html)
        {
            const char PREVIEW_OBJECT_CHARACTER = (char)0xfffc;
            const char PREVIEW_OBJECT_REPLACEMENT = (char)0x20; // space
            const char NBSP_CHARACTER = (char)0x00a0;           // utf-8 non-breaking space
            const char NBSP_REPLACEMENT = (char)0x20;           // space

            return Html.FromHtml(html, null, new HtmlToTextTagHandler()).ToString()
                .Replace(PREVIEW_OBJECT_CHARACTER, PREVIEW_OBJECT_REPLACEMENT)
                .Replace(NBSP_CHARACTER, NBSP_REPLACEMENT);
        }
    }
}