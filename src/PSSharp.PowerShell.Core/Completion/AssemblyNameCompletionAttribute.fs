namespace PSSharp
open System.Reflection

type AssemblyNameCompletionAttribute () =
    inherit CompletionBaseAttribute ()

    /// Offer the Assembly.FullName instead of the short name of the assembly.
    member val FullName = false with get, set

    member private this.SelectCompletionText (assembly : Assembly) =
        match this.FullName with
        | true -> assembly.FullName
        | false -> assembly.GetName().Name

    override this.CompleteArgument(_, _, wordToComplete, _, _) =
        ReflectionCompletion.getAssemblyCompletions this.SelectCompletionText wordToComplete