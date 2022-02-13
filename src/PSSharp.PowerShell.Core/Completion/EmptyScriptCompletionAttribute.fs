namespace PSSharp
open System
open System.Management.Automation

/// Argument completer for an empty script block. Completes with `{}` if no input was already
/// provided to the parameter. Optionally set ScriptComment to complete with `{ <# Comment here #> }`.
type EmptyScriptCompletionAttribute () =
    inherit CompletionBaseAttribute ()

    let mutable completion =
        [
            new CompletionResult(
            "{}",
            "{}",
            CompletionResultType.ParameterValue,
            "Empty script block"
            )
        ]
    let mutable scriptComment = ""

    member _.ScriptComment
        with get () = scriptComment
        and set v =
            scriptComment <- v
            completion <-
                [
                    new CompletionResult(
                        sprintf "{ <#%s#> }" v,
                        sprintf "{ <#%s#> }" v,
                        CompletionResultType.ParameterValue,
                        scriptComment
                        )
                ]

    override _.CompleteArgument(_, _, wordToComplete, _, _) =
        if String.IsNullOrEmpty(wordToComplete) then
            completion
        else []