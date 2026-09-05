using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace API.Utils;

/// <summary>
/// Displays the display name of an enum value in the Swagger UI.
/// </summary>
public class DisplayEnumSchemaFilter : ISchemaFilter
{
    /// <summary>
    /// Replaces an enum's raw member names in the generated schema with each
    /// value's <see cref="DisplayAttribute.Name"/> (falling back to the member
    /// name when absent), so Swagger shows the same friendly text as the UI.
    /// </summary>
    /// <param name="schema">The OpenAPI schema being generated for <paramref name="context"/>'s type.</param>
    /// <param name="context">The schema generation context; used to check <see cref="SchemaFilterContext.Type"/> for enum-ness.</param>
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.IsEnum)
        {
            Type enumType = context.Type;
            schema.Enum = [.. Enum.GetNames(enumType)
                .Select(name =>
                {
                    MemberInfo[] memberInfo = enumType.GetMember(name);
                    DisplayAttribute? displayAttribute = memberInfo[0].GetCustomAttribute<DisplayAttribute>();

                    string displayName = displayAttribute?.Name ?? name;
                    return new OpenApiString(displayName) as IOpenApiAny;
                })];
        }
    }
}