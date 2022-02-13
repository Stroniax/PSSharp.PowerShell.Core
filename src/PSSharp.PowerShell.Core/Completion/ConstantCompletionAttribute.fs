namespace PSSharp
open System
open System.Management.Automation
open System.Diagnostics.CodeAnalysis
open ArgumentCompletion

type ConstantCompletionQuoteBehavior =
    /// Use default quote behavior (requote text with initially provided quotes).
    | Default = 0
    /// Provide the CompletionText completion exactly as provided.
    | NotQuoted = 1

type ConstantCompletionAttribute (value) =
    inherit CompletionBaseAttribute ()

    member val CompletionText = value
    member val ListItemText = value with get, set
    member val ToolTip = value with get, set
    member val ResultType = CompletionResultType.ParameterValue with get, set
    member val QuoteBehavior = ConstantCompletionQuoteBehavior.Default with get, set

    member private this.GetQuotedText (quoteType) =
        match this.QuoteBehavior with
        | ConstantCompletionQuoteBehavior.NotQuoted -> this.CompletionText
        | ConstantCompletionQuoteBehavior.Default
        | _ ->
            maybeQuote shouldQuote canAvoidQuotes quoteType RequoteOptions.None this.CompletionText

    override this.CompleteArgument(_, _, wordToComplete, _, _) =
        let { QuotationType = quoteType; UnquotedText = wordToComplete } = trimQuotes wordToComplete
        let wc = WildcardPattern.Get(wordToComplete + "*", WildcardOptions.IgnoreCase)
        match wc.IsMatch(value) with
        | true ->
            [|
                new CompletionResult(
                    this.GetQuotedText(quoteType),
                    this.ListItemText,
                    this.ResultType,
                    this.ToolTip)
            |]
        | false -> Array.empty