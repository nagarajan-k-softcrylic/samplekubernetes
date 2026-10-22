using System.ComponentModel.DataAnnotations;
using EmployeeApi.Models;
using EmployeeApi.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeApi.Endpoints;

public static class EmployeeEndpoints
{
    public static RouteGroupBuilder MapEmployeeEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/employees")
            .WithTags("Employees");

        group.MapGet("/", GetAllEmployees)
            .WithName("GetAllEmployees")
            .WithSummary("Retrieve all employees")
            .WithDescription("Returns a list of all active employee records.")
            .Produces<IReadOnlyList<Employee>>(StatusCodes.Status200OK);

        group.MapGet("/{id:int}", GetEmployeeById)
            .WithName("GetEmployeeById")
            .WithSummary("Retrieve an employee by ID")
            .WithDescription("Returns employee details for the given numeric ID.")
            .Produces<Employee>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateEmployee)
            .WithName("CreateEmployee")
            .WithSummary("Create a new employee")
            .WithDescription("Creates a new employee record and returns the created entity with location header.")
            .Produces<Employee>(StatusCodes.Status201Created)
            .Produces<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:int}", UpdateEmployee)
            .WithName("UpdateEmployee")
            .WithSummary("Update an existing employee")
            .WithDescription("Updates the details of an existing employee identified by ID.")
            .Produces<Employee>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapDelete("/{id:int}", DeleteEmployee)
            .WithName("DeleteEmployee")
            .WithSummary("Delete an employee")
            .WithDescription("Removes the employee record associated with the specified ID.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<Ok<IReadOnlyList<Employee>>> GetAllEmployees(
        IEmployeeRepository repository,
        CancellationToken cancellationToken)
    {
        var employees = await repository.GetAllAsync(cancellationToken);
        return TypedResults.Ok(employees);
    }

    private static async Task<Results<Ok<Employee>, NotFound<ProblemDetails>>> GetEmployeeById(
        int id,
        IEmployeeRepository repository,
        CancellationToken cancellationToken)
    {
        var employee = await repository.GetByIdAsync(id, cancellationToken);
        if (employee is null)
        {
            return TypedResults.NotFound(new ProblemDetails
            {
                Title = "Employee Not Found",
                Detail = $"No employee found with ID {id}.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return TypedResults.Ok(employee);
    }

    private static async Task<Results<CreatedAtRoute<Employee>, ValidationProblem>> CreateEmployee(
        CreateEmployeeRequest request,
        IEmployeeRepository repository,
        CancellationToken cancellationToken)
    {
        if (!TryValidate(request, out var validationErrors))
        {
            return TypedResults.ValidationProblem(validationErrors);
        }

        var created = await repository.CreateAsync(request, cancellationToken);

        return TypedResults.CreatedAtRoute(
            routeName: "GetEmployeeById",
            routeValues: new { id = created.Id },
            value: created);
    }

    private static async Task<Results<Ok<Employee>, NotFound<ProblemDetails>, ValidationProblem>> UpdateEmployee(
        int id,
        UpdateEmployeeRequest request,
        IEmployeeRepository repository,
        CancellationToken cancellationToken)
    {
        if (!TryValidate(request, out var validationErrors))
        {
            return TypedResults.ValidationProblem(validationErrors);
        }

        var updated = await repository.UpdateAsync(id, request, cancellationToken);
        if (updated is null)
        {
            return TypedResults.NotFound(new ProblemDetails
            {
                Title = "Employee Not Found",
                Detail = $"Cannot update. No employee found with ID {id}.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return TypedResults.Ok(updated);
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>>> DeleteEmployee(
        int id,
        IEmployeeRepository repository,
        CancellationToken cancellationToken)
    {
        var deleted = await repository.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return TypedResults.NotFound(new ProblemDetails
            {
                Title = "Employee Not Found",
                Detail = $"Cannot delete. No employee found with ID {id}.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return TypedResults.NoContent();
    }

    private static bool TryValidate(object instance, out IDictionary<string, string[]> errors)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(instance);

        var isValid = Validator.TryValidateObject(
            instance,
            validationContext,
            validationResults,
            validateAllProperties: true);

        if (isValid)
        {
            errors = new Dictionary<string, string[]>();
            return true;
        }

        errors = validationResults
            .SelectMany(r => (r.MemberNames.Any() ? r.MemberNames : new[] { "Request" })
                .Select(m => new { Member = m, r.ErrorMessage }))
            .GroupBy(x => x.Member)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.ErrorMessage ?? "Invalid input field.").Distinct().ToArray()
            );

        return false;
    }
}
