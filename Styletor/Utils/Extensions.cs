using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using VRC.UI.Core.Styles;

namespace Styletor.Utils
{
    public static class Extensions
    {
        public static string ToStringSmart(this Selector selector)
        {
            var builder = new StringBuilder();
            builder.Append("Selector(");
            if (!selector.field_Private_Boolean_0)
                builder.Append("dynamic,");
            var tags = selector.field_Private_HashSet_1_String_0;
            if (tags.Count > 0)
            {
                builder.Append("tags=[");
                foreach (var tag in tags)
                {
                    builder.Append(tag);
                    builder.Append(",");
                }
                builder.Append("],");
            }

            var subs = selector.field_Private_List_1_SubSelector_0;
            if (subs.Count > 0)
            {
                builder.Append("subs=[");
                foreach (var subSelector in subs)
                {
                    subSelector.ToString(builder);
                    builder.Append(",");
                }

                builder.Append("],");
            }

            builder.Append("text=");
            builder.Append(selector.field_Public_String_0);
            builder.Append(")");
            return builder.ToString();
        }
        
        public static string ToStringNormalized(this Selector selector)
        {
            var builder = new StringBuilder();
            if (!selector.field_Private_Boolean_0)
                builder.Append("D,");
            var tags = selector.field_Private_HashSet_1_String_0;
            if (tags.Count > 0)
            {
                builder.Append("t[");
                foreach (var tag in tags)
                {
                    builder.Append(tag);
                    builder.Append(",");
                }
                builder.Append("],");
            }

            var subs = selector.field_Private_List_1_SubSelector_0;
            if (subs.Count > 0)
            {
                builder.Append("s[");
                foreach (var subSelector in subs)
                {
                    subSelector.ToStringNormalized(builder);
                    builder.Append(",");
                }

                builder.Append("],");
            }
            
            return builder.ToString();
        }

        private static void ToString(this Selector.SubSelector sub, StringBuilder builder)
        {
            builder.Append("Sub(");
            if (!sub.field_Private_Boolean_0)
                builder.Append("dynamic,");
            if (sub.field_Public_SubSelector_0 != null)
            {
                builder.Append("parent=");
                sub.field_Public_SubSelector_0.ToString(builder);
                builder.Append(",");
            }
            builder.Append("connector=");
            builder.Append(sub.field_Public_Connector_0.ToString());
            builder.Append(",");
            builder.Append("stmts=[");
            foreach (var statement in sub.field_Private_List_1_Statement_0)
            {
                builder.Append("Stmt(");
                builder.Append("op=");
                builder.Append(statement.field_Public_Operation_0.ToString());
                builder.Append(",t=");
                builder.Append(statement.field_Public_Type_0.ToString());
                builder.Append(",v=");
                builder.Append(statement.field_Public_String_0);
                builder.Append("),");
            }
            builder.Append("])");
        }
        
        private static void ToStringNormalized(this Selector.SubSelector sub, StringBuilder builder)
        {
            builder.Append("(");
            if (!sub.field_Private_Boolean_0)
                builder.Append("D,");
            if (sub.field_Public_SubSelector_0 != null)
            {
                builder.Append("p=");
                sub.field_Public_SubSelector_0.ToStringNormalized(builder);
                builder.Append(",");
            }
            builder.Append("c=");
            builder.Append((int) sub.field_Public_Connector_0);
            builder.Append(",");
            builder.Append("s[");
            foreach (var statement in sub.field_Private_List_1_Statement_0)
            {
                builder.Append("(");
                builder.Append((int) statement.field_Public_Operation_0);
                builder.Append(",");
                builder.Append((int) statement.field_Public_Type_0);
                builder.Append(",");
                builder.Append(statement.field_Public_String_0);
                builder.Append("),");
            }
            builder.Append("])");
        }

        public static float Grayscale(this Color c) => 0.29899999499321f * c.r + 0.587000012397766f * c.g + 57.0f / 500.0f * c.b;
        public static Color RGBMultipliedClamped(this Color c, float m) => new() { r = Math.Min(c.r * m, 1f), g = Math.Min(c.g * m, 1f), b = Math.Min(c.b * m, 1f), a = c.a };

        public static byte[] ReadAllBytes(this Stream stream)
        {
            using var memStream = new MemoryStream();
            stream.CopyTo(memStream);
            return memStream.ToArray();
        }
        
        public static string ReadAllText(this Stream stream)
        {
            return Encoding.UTF8.GetString(stream.ReadAllBytes());
        }

        public static List<string> ReadAllLines(this Stream stream)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, false, 4096, true);

            var result = new List<string>();
            while (!reader.EndOfStream)
                result.Add(reader.ReadLine());
            
            return result;
        }
    }
}