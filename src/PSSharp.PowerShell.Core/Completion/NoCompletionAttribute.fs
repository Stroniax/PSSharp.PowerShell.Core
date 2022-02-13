namespace PSSharp
open System.Management.Automation
open System


type NoCompletionAttribute () =
    inherit CompletionBaseAttribute ()

    override _.CompleteArgument(_, _, _, _, _) =
        Array.Empty()


    