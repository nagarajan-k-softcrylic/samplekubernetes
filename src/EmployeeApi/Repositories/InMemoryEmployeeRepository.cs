using System.Collections.Concurrent;
using EmployeeApi.Models;

namespace EmployeeApi.Repositories;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IEmployeeRepository"/>.
/// Suitable for containerized execution and local/cloud prototyping.
/// </summary>
public sealed class InMemoryEmployeeRepository : IEmployeeRepository
{
    private readonly ConcurrentDictionary<int, Employee> _employees = new();
    private int _currentId;

    public InMemoryEmployeeRepository()
    {
        // Seed initial organizational data for immediate testing and verification
        SeedInitialData();
    }

    private void SeedInitialData()
    {
        var initialEmployees = new[]
        {
            new Employee { Id = 1, Name = "Alice Johnson", Email = "alice.johnson@contoso.com", Department = "Cloud Architecture" },
            new Employee { Id = 2, Name = "Bob Smith", Email = "bob.smith@contoso.com", Department = "DevOps & SRE" },
            new Employee { Id = 3, Name = "Carol Danvers", Email = "carol.danvers@contoso.com", Department = "Platform Engineering" },
            new Employee { Id = 4, Name = "David Miller", Email = "david.miller@contoso.com", Department = "Security & Governance" }
        };

        foreach (var employee in initialEmployees)
        {
            _employees.TryAdd(employee.Id, employee);
        }

        _currentId = initialEmployees.Length;
    }

    public Task<IReadOnlyList<Employee>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Employee> list = _employees.Values
            .OrderBy(e => e.Id)
            .ToList()
            .AsReadOnly();

        return Task.FromResult(list);
    }

    public Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _employees.TryGetValue(id, out var employee);
        return Task.FromResult(employee);
    }

    public Task<Employee> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var newId = Interlocked.Increment(ref _currentId);
        var employee = new Employee
        {
            Id = newId,
            Name = request.Name.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Department = request.Department.Trim()
        };

        _employees[newId] = employee;
        return Task.FromResult(employee);
    }

    public Task<Employee?> UpdateAsync(int id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        if (!_employees.TryGetValue(id, out var existing))
        {
            return Task.FromResult<Employee?>(null);
        }

        var updated = new Employee
        {
            Id = id,
            Name = request.Name.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Department = request.Department.Trim()
        };

        _employees[id] = updated;
        return Task.FromResult<Employee?>(updated);
    }

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var removed = _employees.TryRemove(id, out _);
        return Task.FromResult(removed);
    }
}

