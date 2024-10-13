using Club12.DTOs.Abstract;

namespace Club12.Viewmodels.Abstract;

public class BaseResponse<T> where T : GenericEntity
{
    public string? ErrorMessage { get; set; }
    public int StatusCode { get; set; }
    public T? Content { get; set; }


    public BaseResponse(int statusCode, T? content, string? errorMessage)
    {
        StatusCode = statusCode;
        Content = content;
        ErrorMessage = errorMessage;
    }
}