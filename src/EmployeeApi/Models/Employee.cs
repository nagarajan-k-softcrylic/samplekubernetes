namespace EmployeeApi.Models;

/// <summary>
/// Represents an Employee entity in the organization.
/// </summary>
public sealed class Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}

