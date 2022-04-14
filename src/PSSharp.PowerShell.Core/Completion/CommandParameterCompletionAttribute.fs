namespace PSSharp
open System.Management.Automation
open ArgumentCompletion

type CommandParameterCompletionAttribute () =
    inherit CompletionBaseAttribute ()

    override _.CompleteArgument(_, _, wordToComplete, _, fakeBoundParams) =
        
