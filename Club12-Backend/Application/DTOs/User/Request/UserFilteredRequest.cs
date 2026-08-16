using Application.DTOs.Abstract.Request;

using Domain.Enums;

namespace Application.DTOs.User.Request;

public class UserFilteredRequest : PaginatedFilterRequest
{
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public UserRoleType? Role { get; set; }
    public string? PhoneNumber { get; set; }
}
