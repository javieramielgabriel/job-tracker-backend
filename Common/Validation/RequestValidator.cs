using System.ComponentModel.DataAnnotations;

namespace JobTracker.Api.Common.Validation;

/// <summary>
/// Reusable validation utility for validating request DTOs and nested collections using DataAnnotations.
/// </summary>
public static class RequestValidator
{
    /// <summary>
    /// Validates a single object instance using DataAnnotations.
    /// </summary>
    public static Dictionary<string, string[]> ValidateObject<TRequest>(TRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            validationResults,
            validateAllProperties: true);

        return ToValidationDictionary(validationResults);
    }

    /// <summary>
    /// Validates a parent request DTO along with a nested collection of child items, optionally enforcing unique IDs.
    /// </summary>
    public static Dictionary<string, string[]> ValidateWithCollection<TRequest, TChild>(
        TRequest request,
        IReadOnlyList<TChild>? children,
        string collectionPropertyName,
        Func<TChild, Guid?>? childIdSelector = null,
        bool validateUniqueIds = false,
        string? duplicateIdMessagePrefix = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            validationResults,
            validateAllProperties: true);

        if (children is null)
        {
            validationResults.Add(new ValidationResult(
                $"{collectionPropertyName} cannot be null.",
                [collectionPropertyName]));
        }
        else
        {
            var seenIds = new HashSet<Guid>();
            for (var index = 0; index < children.Count; index++)
            {
                var currentIndex = index;
                var item = children[currentIndex];
                if (item is null)
                {
                    validationResults.Add(new ValidationResult(
                        "Collection item cannot be null.",
                        [$"{collectionPropertyName}[{currentIndex}]"]));
                    continue;
                }

                if (validateUniqueIds && childIdSelector is not null)
                {
                    var itemId = childIdSelector(item);
                    if (itemId is Guid id && !seenIds.Add(id))
                    {
                        var prefix = duplicateIdMessagePrefix ?? "Duplicate ID";
                        validationResults.Add(new ValidationResult(
                            $"{prefix} '{id}'.",
                            [$"{collectionPropertyName}[{currentIndex}].Id"]));
                    }
                }

                var childResults = new List<ValidationResult>();
                Validator.TryValidateObject(
                    item,
                    new ValidationContext(item),
                    childResults,
                    validateAllProperties: true);

                validationResults.AddRange(childResults.Select(result =>
                    new ValidationResult(
                        result.ErrorMessage,
                        result.MemberNames
                            .Select(name => $"{collectionPropertyName}[{currentIndex}].{name}")
                            .ToArray())));
            }
        }

        return ToValidationDictionary(validationResults);
    }

    public static Dictionary<string, string[]> ToValidationDictionary(
        IEnumerable<ValidationResult> validationResults) =>
        validationResults
            .SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty)
                .Select(member => new { Member = member, Error = result.ErrorMessage ?? "The value is invalid." }))
            .GroupBy(item => item.Member)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.Error).Distinct().ToArray());
}
