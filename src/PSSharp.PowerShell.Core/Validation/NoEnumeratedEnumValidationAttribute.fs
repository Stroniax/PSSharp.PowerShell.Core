namespace PSSharp
open System
open System.Management.Automation

/// Argument validation to ensure that a parameter is not given a collection (or array)
/// of values. This is particularly designed to prevent the PowerShell engine from
/// flattening an array of enum values that do not accept flags; by default, the engine
/// will convert these to numeric values and run -bor to combine the values, which
/// may result in an unexpected value.
type FlaglessEnumValidationAttribute () =
    // must inherit from transformation so that we capture the mutliple values
    // if there is more than one before the PowerShell engine flattens it
    inherit ArgumentTransformationAttribute()

    /// <summary>
    /// Raises an <see cref="ArgumentException"/> if <paramref name="value"/> is enumerable
    /// according to <see cref="LanguagePrimitives.IsObjectEnumerable(string)"/>.
    /// </summary>
    override _.Transform(_, value) =
        if LanguagePrimitives.IsObjectEnumerable(value) then
            new ArgumentException(ErrorMessages.NoEnumeratedEnumValidation)
            |> raise
        else
            value

