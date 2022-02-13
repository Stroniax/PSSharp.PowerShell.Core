namespace PSSharp
open ReflectionCompletion

/// Complete Namespace information for the namespaces loaded into the current AppDomain.
type NamespaceCompletionAttribute () =
    inherit CompletionBaseAttribute()

    override _.CompleteArgument(_, _, wordToComplete, _, _) =
        getNamespaceCompletions wordToComplete