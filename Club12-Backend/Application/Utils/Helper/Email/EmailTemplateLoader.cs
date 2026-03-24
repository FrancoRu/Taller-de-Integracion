using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Application.Utils.Helper.Email;

/// <summary>
/// Loads HTML email templates from embedded resources and replaces named tokens
/// in the form <c>{{TokenName}}</c>.
/// </summary>
internal static class EmailTemplateLoader
{
    // Resolved once — typeof is guaranteed to resolve to the Application assembly.
    private static readonly Assembly _assembly = typeof(EmailTemplateLoader).Assembly;

    /// <summary>
    /// Reads the template file and replaces every key in <paramref name="tokens"/>
    /// with its corresponding value.
    /// </summary>
    /// <param name="templateName">
    /// File name without extension, e.g. <c>"PasswordResetTemplate"</c>.
    /// The file must be at <c>Utils/Helper/Email/Templates/{templateName}.html</c>
    /// and marked as <c>EmbeddedResource</c>.
    /// </param>
    /// <param name="tokens">Dictionary of <c>{{Token}}</c> → replacement value pairs.</param>
    public static string Render(string templateName, Dictionary<string, string> tokens)
    {
        string resourceName =
            $"Application.Utils.Helper.Email.Templates.{templateName}.html";

        using Stream? stream = _assembly.GetManifestResourceStream(resourceName) ?? throw new InvalidOperationException(
                $"Email template '{templateName}' not found as embedded resource. " +
                $"Ensure the file exists at 'Utils/Helper/Email/Templates/{templateName}.html' " +
                $"and is marked as <EmbeddedResource> in the Application.csproj.");
        using StreamReader reader = new(stream);
        string template = reader.ReadToEnd();

        return tokens.Aggregate(template,
            (current, token) => current.Replace(token.Key, token.Value));
    }
}
