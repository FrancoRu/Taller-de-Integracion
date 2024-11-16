using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Club12.API.Utils;

/// <summary>
/// Displays the display name of an enum value in the Swagger UI.
/// </summary>
public class DisplayEnumSchemaFilter : ISchemaFilter
{
    /// <summary>
    /// Applies the filter to the schema.
    /// </summary>
    /// <param name="schema"></param>
    /// <param name="context"></param>
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.IsEnum)
        {
            Type enumType = context.Type;
            schema.Enum = Enum.GetNames(enumType)
                .Select(name =>
                {
                    MemberInfo[] memberInfo = enumType.GetMember(name);
                    DisplayAttribute? displayAttribute = memberInfo[0].GetCustomAttribute<DisplayAttribute>();

                    string displayName = displayAttribute?.Name ?? name;
                    return new OpenApiString(displayName) as IOpenApiAny;
                })
                .ToList();
        }
    }
}