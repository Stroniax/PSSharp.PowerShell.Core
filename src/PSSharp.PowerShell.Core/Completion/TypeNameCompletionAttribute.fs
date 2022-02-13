namespace PSSharp
open System

/// Offer argument completion for type names.
type TypeNameCompletionAttribute () =
    inherit CompletionBaseAttribute ()
    member private this.MapToTypeName (t : Type) =
        match this.TypeNameOnly with
        | true -> t.Name
        | false -> t.FullName

    /// Complete only the type name instead of the full name of the type.
    member val TypeNameOnly = false with get, set

    /// Allow completion of private types
    member val TypeCriteria = TypeNameCompletionCriteria.Public with get, set

    override this.CompleteArgument(_, _, wordToComplete, _, _) =
        ReflectionCompletion.getTypeCompletions this.TypeCriteria this.MapToTypeName wordToComplete
