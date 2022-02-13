namespace PSSharp
open System.Management.Automation

type CommandCompletionAttribute () =
    inherit CompletionBaseAttribute ()

    override _.CompleteArgument(_, _, wordToComplete, _, _) =
        CompletionCompleters.CompleteCommand(wordToComplete)