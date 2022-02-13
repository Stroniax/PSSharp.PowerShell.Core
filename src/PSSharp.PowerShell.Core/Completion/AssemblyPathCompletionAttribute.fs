namespace PSSharp
open System.Reflection

type AssemblyPathCompletionAttribute () =
    inherit CompletionBaseAttribute ()

    static let selectCompletionText (assembly: Assembly) = assembly.Location

    override _.CompleteArgument(_, _, wordToComplete, _, _) =
        ReflectionCompletion.getAssemblyCompletions selectCompletionText wordToComplete