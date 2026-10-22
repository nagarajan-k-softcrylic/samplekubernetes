using EmployeeApi.Models;

namespace EmployeeApi.Repositories;

/// <summary>
/// Defines asynchronous data access operations for Employee management.
/// </summary>
public interface IEmployeeRepository
{
    /// <summary>
    /// Retrieves all employees.
    /// </summary>
    Task<IReadOnlyList<Employee>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single employee by their unique identifier.
    /// </summary>
    Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates and persists a new employee.
    /// </summary>
    Task<Employee> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing employee by their unique identifier.
    /// </summary>
    Task<Employee?> UpdateAsync(int id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an employee by their unique identifier.
    /// </summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

