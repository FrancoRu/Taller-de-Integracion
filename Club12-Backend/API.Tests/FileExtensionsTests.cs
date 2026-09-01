using API.Utils;

using Microsoft.AspNetCore.Http;

using System.Text;

namespace API.Tests;

/// <summary>
/// Covers <see cref="FileExtensions.IsValidPdfFile"/>, the gate on medical-record
/// uploads (HU-55/HU-56): only a non-empty, actual PDF may be stored, so the
/// same file an admin uploads is exactly what a later download returns
/// (round-tripped byte-for-byte, verified separately in
/// MedicalRecordStorageTests). This had no direct test coverage before.
/// </summary>
public class FileExtensionsTests
{
    [Fact]
    public void IsValidPdfFile_NullFile_ReturnsFalse()
    {
        // Deliberately passing null to verify the guard clause it exercises.
        IFormFile nullFile = null!;

        Assert.False(nullFile.IsValidPdfFile());
    }

    [Fact]
    public void IsValidPdfFile_EmptyFile_ReturnsFalse()
    {
        // Correct extension and content type, but zero bytes — must still be
        // rejected. This is the exact "no vacío" requirement.
        FormFile emptyFile = MakeFile("ficha.pdf", string.Empty, "application/pdf");

        Assert.False(emptyFile.IsValidPdfFile());
    }

    [Fact]
    public void IsValidPdfFile_NonPdfExtension_ReturnsFalse()
    {
        FormFile wordFile = MakeFile("ficha.docx", "some content", "application/pdf");

        Assert.False(wordFile.IsValidPdfFile());
    }

    [Fact]
    public void IsValidPdfFile_MismatchedContentType_ReturnsFalse()
    {
        // Right extension, but the browser/client reports a different type —
        // a renamed non-PDF file wearing a .pdf extension.
        FormFile mislabeled = MakeFile("ficha.pdf", "not really a pdf", "image/png");

        Assert.False(mislabeled.IsValidPdfFile());
    }

    [Fact]
    public void IsValidPdfFile_ValidNonEmptyPdf_ReturnsTrue()
    {
        FormFile validPdf = MakeFile("ficha.pdf", "%PDF-1.4 body", "application/pdf");

        Assert.True(validPdf.IsValidPdfFile());
    }

    [Fact]
    public void IsValidPdfFile_MissingContentType_StillAcceptedByExtension_ReturnsTrue()
    {
        // Some clients omit Content-Type on the multipart part; the extension
        // alone is enough when no type was declared at all.
        FormFile noContentType = MakeFile("ficha.pdf", "%PDF-1.4 body", contentType: string.Empty);

        Assert.True(noContentType.IsValidPdfFile());
    }

    private static FormFile MakeFile(string fileName, string content, string contentType)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(content);
        MemoryStream stream = new(bytes);

        return new FormFile(stream, 0, stream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };
    }
}
