using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine;

/// <summary>
/// An assorted set of helper methods, fields and properties
/// </summary>
public static class Helper
{
    /// <summary>
    /// Builds a string that looks like what would be used in C# to express the full closed type
    /// </summary>
    public static string BuildTypeNameAsCSharpTypeExpression(Type type)
    {
        if (type.IsGenericType && type.IsConstructedGenericType is false)
            throw new ArgumentException("This method does not support open generic types", nameof(type));

        using (SharedObjectPools.StringBuilderPool.Rent().GetItem(out var sb))
        {
            AddTypeNameWithoutGeneric(sb, type.Name);
            FillTypeParams(sb, type.GenericTypeArguments);
            return sb.ToString();
        }

        static void FillTypeParams(StringBuilder sb, Type[] genericTypes)
        {
            if (genericTypes.Length == 0) return;

            Type type;
            sb.Append('<');
            for (int i = 0; i < genericTypes.Length - 1; i++)
            {
                type = genericTypes[i];
                AddTypeNameWithoutGeneric(sb, type.Name);
                FillTypeParams(sb, type.GetGenericArguments());
                sb.Append(", ");
            }
            type = genericTypes[^1];
            AddTypeNameWithoutGeneric(sb, type.Name);
            FillTypeParams(sb, type.GetGenericArguments());
            sb.Append('>');
        }

        static void AddTypeNameWithoutGeneric(StringBuilder sb, string name)
        {
            int ind = name.IndexOf('`');
            if (ind > 0)
                sb.Append(name.AsSpan(0, ind));
            else
                sb.Append(name);
        }
    }
}
