using System.ComponentModel.DataAnnotations;

namespace EmployeeApi.Models;

/// <summary>
/// Request payload for updating an existing employee with data annotations validation.
/// </summary>
public sealed class UpdateEmployeeRequest : IValidatableObject
{
    [Required(ErrorMessage = "Name is required.", AllowEmptyStrings = false)]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.", AllowEmptyStrings = false)]
    [EmailAddress(ErrorMessage = "A valid email address is required.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department is required.", AllowEmptyStrings = false)]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Department must be between 2 and 50 characters.")]
    public string Department { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult("Name cannot consist of whitespace only.", new[] { nameof(Name) });
        }

        if (string.IsNullOrWhiteSpace(Department))
        {
            yield return new ValidationResult("Department cannot consist of whitespace only.", new[] { nameof(Department) });
        }
    }
}

