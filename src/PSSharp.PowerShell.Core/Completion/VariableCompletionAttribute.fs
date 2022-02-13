namespace PSSharp
open System.Management.Automation
open ArgumentCompletion

type VariableCompletionAttribute () =
    inherit CompletionBaseAttribute ()
    
    static let completeVariablesFromSessionState wordToComplete =
        let { QuotationType = quoteType; UnquotedText = text } = trimQuotes wordToComplete
        match text.StartsWith('$') with
        | true -> text.Substring(1)
        | false -> text
        |> CompletionCompleters.CompleteVariable

    override _.CompleteArgument(_, _, wordToComplete, _, _) =
        completeVariablesFromSessionState wordToComplete