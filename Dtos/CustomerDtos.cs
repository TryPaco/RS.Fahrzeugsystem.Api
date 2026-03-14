namespace RS.Fahrzeugsystem.Api.Dtos;

public sealed record CreateCustomerRequest(
    string CustomerNumber,
    string? CompanyName,
    string FirstName,
    string LastName,
    string? Phone,
    string? Email,
    string? Street,
    string? ZipCode,
    string? City,
    string? Country,
    string? Notes);

public sealed record UpdateCustomerRequest(
    string CustomerNumber,
	string? CompanyName,
    string FirstName,
    string LastName,
    string? Phone,
    string? Email,
    string? Street,
    string? ZipCode,
    string? City,
    string? Country,
    string? Notes,
    bool IsArchived);
