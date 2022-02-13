namespace PSSharp
open System
open System.Management.Automation
open ArgumentCompletion

/// Completes with the value of another parameter, if that parameter's value is known.
type DuplicateParameterCompletionAttribute (parameterName: string) =
    inherit CompletionBaseAttribute ()

    do if parameterName = null then nullArg (nameof parameterName)

    member _.ParameterName = parameterName

    override _.CompleteArgument(_, _, wordToComplete, _, fakeBoundParameters) =
        let { QuotationType = quoteType; UnquotedText = wordToComplete } = trimQuotes wordToComplete
        let wc = WildcardPattern.Get(wordToComplete + "*", WildcardOptions.IgnoreCase)

        // I would like to work in the Ast here but it feels far too unreliable with positional parameters,
        // switch parameters, arrays, variables, and splatting...

        match fakeBoundParameters.Contains(parameterName) with
        | true ->
            // we can only complete a 'primitive' value from fakeBoundParameters
            let parameterValue =
                fakeBoundParameters[parameterName]
                |> PSLanguagePrimitives.cast
            if wc.IsMatch(parameterValue) then
                [
                    new CompletionResult(
                        parameterValue
                        |> maybeQuote
                            shouldQuote
                            canAvoidQuotes
                            quoteType
                            RequoteOptions.None,
                        parameterValue,
                        CompletionResultType.ParameterValue,
                        parameterValue
                    )
                ]
            else Array.empty
        | false -> Array.empty
