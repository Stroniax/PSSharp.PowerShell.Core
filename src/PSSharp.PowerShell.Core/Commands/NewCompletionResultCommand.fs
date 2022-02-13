namespace PSSharp.Commands
open System
open System.Management.Automation
open System.Collections.Generic
open System.Collections.ObjectModel
open PSSharp
open ArgumentCompletion

/// Create a completion result for argument auto-completion. Optionally pipe source
/// data into the command to build completion results from. Optionally provide the
/// word to complete to provide proper quotation and filtering. Optionally provide
/// a FilterScript to filter completion results.
[<Cmdlet(VerbsCommon.New, Nouns.CompletionResult)>]
type NewCompletionResultCommand () =
    inherit Cmdlet ()

    /// Actual, quote-stripped word to complete.
    let mutable wordToComplete = "*"
    /// Wildcard pattern to match against completions.
    let mutable wc = WildcardPattern.Get(wordToComplete, WildcardOptions.IgnoreCase)
    /// Type of quotation to use when re-quoting completions.
    let mutable quoteType = QuotationType.NoQuote

    /// Possibly flatten elements of a collection if the count is 1.
    static let maybeFlatten (coll : Collection<_>) : obj =
        if coll.Count = 1 then coll[0]
        else coll

    /// Coalesce value with defaultValue if value is null or empty.
    static let coalesceNullOrEmpty defaultValue value =
        match String.IsNullOrEmpty(value) with
        | true -> defaultValue
        | false -> value

    /// Get a string from a parameter's script result or the parameter value.
    static let tryGetText (pso : pso, inputObject : pso) : string voption =
        if pso = null then ValueNone else
        if pso = psnull then ValueNone else
        match psbase pso with
        | :? string as text -> ValueSome text
        | :? ScriptBlock as getScript ->
            let dollarUnder = new PSVariable(SpecialVariables.psItem, inputObject)
            let variables = new List<PSVariable>([dollarUnder])
            getScript.InvokeWithContext(null, variables, inputObject)
            |> maybeFlatten
            |> PSLanguagePrimitives.cast
            |> ValueSome
        // Use PS stringify in case of special cast to string behavior
        | otherwise ->
            otherwise
            |> PSLanguagePrimitives.cast
            |> ValueSome
    static let tryGetTextOrDefault (pso : pso, inputObject : pso, defaultValue: string) : string =
        match tryGetText(pso, inputObject) with
        | ValueSome s -> s
        | ValueNone -> defaultValue

    /// Determine if a completion should be offered for the current CompletionText based on
    /// a filter script, wildcard pattern, word to be completed, and input object.
    static let shouldOfferCompletion(completionText: string, filterScript: ScriptBlock, wildcard: WildcardPattern, wordToComplete: string, inputObject: pso) =
        match filterScript with
        | null -> wildcard.IsMatch(completionText)
        | _ ->  LanguagePrimitives.IsTrue(
                    filterScript.InvokeWithContext(
                        null,
                        new List<PSVariable>(
                                [
                                    new PSVariable(
                                        SpecialVariables.psItem,
                                        inputObject
                                    )
                                    new PSVariable(
                                        "wildcardPattern",
                                        wildcard
                                        )
                                    new PSVariable(
                                        "wordToComplete",
                                        wordToComplete
                                        )
                                    new PSVariable(
                                        SpecialVariables.input,
                                        inputObject
                                        )
                                ]
                            ),
                        inputObject
                    )
                )

    /// Actual text to offer for completion. This can be a script to generate the
    /// text with $PSItem ($_) representing the current InputObject. The text will be
    /// quoted based on the quotes surrounding the value of the WordToComplete parameter.
    [<Parameter(Mandatory = true, Position = 0)>]
    [<Alias("ct")>]
    [<AstStringConstantCompletion>]
    member val CompletionText = psnull with get, set

    /// Text to display in list view for the completion. This can be a script to generate
    /// the ListItemText with $PSItem ($_) representing the current InputObject.
    [<Parameter(Position = 1)>]
    [<Alias("listtext", "lit")>]
    [<DuplicateParameterCompletion("CompletionText")>]
    member val ListItemText = psnull with get, set

    /// The type of the completion result offered. Generally, ParmeterValue (default) is
    /// sufficient.
    [<Parameter(Position = 2)>]
    [<Alias("rt", "type")>]
    member val ResultType = CompletionResultType.ParameterValue with get, set

    /// Tool Tip for completion text when the [Ctrl + " "] key stroke is used, to display
    /// additional information about the currently selected completion.
    /// This can be a script to generate the ToolTip with $PSItem ($_) representing the
    /// current InputObject.
    [<Parameter(Position = 3)>]
    [<Alias("tt", "tip")>]
    [<DuplicateParameterCompletion("CompletionText")>]
    member val ToolTip = psnull with get, set

    /// Word to be completed. Determines quoting information on the completion text offered.
    /// When FilterScript is not provided, this value will also filter out results where
    /// CompletionText does not match the wildcard pattern generated from this parameter.
    /// To provide a WordToComplete for quoting purposes but not filter out results,
    /// use `-FilterScript {$true}`.
    [<Parameter>]
    [<Alias("wildcard", "from", "wtc")>]
    member val WordToComplete = "" with get, set

    /// Input object to process when creating a completion text from an input value.
    [<Parameter(ValueFromPipeline = true)>]
    [<Alias("io", "psitem")>]
    [<NoCompletion>]
    member val InputObject : pso = psnull with get, set

    /// Script to filter InputObject. The script has the $PSItem ($_) variable defined as the current
    /// InputObject, the [WildcardPattern]$wildcardPattern representing a wildcarded version of
    /// the WordToComplete parameter, and the [string]$wordToComplete representing the quote-stripped,
    /// wildcarded WordToComplete parameter.
    /// If $null, the value of the WordToComplete parameter will be matched to the CompletionText
    /// to determine if a completion result should be passed to the pipeline.
    [<Parameter>]
    [<Alias("fs")>]
    [<EmptyScriptCompletion>]
    member val FilterScript : ScriptBlock = null with get, set

    /// Options in determining how completion text is quoted after identified by the
    /// CompletionText script or parameter value.
    [<Parameter>]
    [<Alias("options", "ro")>]
    member val RequoteOptions = RequoteOptions.None with get, set

    override this.BeginProcessing () =
        let { QuotationType = quoteTypeFromInput; UnquotedText = wordToCompleteFromInput } = trimQuotes this.WordToComplete
        wordToComplete <- wordToCompleteFromInput + "*"
        wc <- WildcardPattern.Get(wordToComplete, WildcardOptions.IgnoreCase)
        quoteType <- quoteTypeFromInput

    override this.ProcessRecord () =
        match tryGetText(this.CompletionText, this.InputObject) with
        | ValueSome completionText when
                not(String.IsNullOrEmpty(completionText))
                && shouldOfferCompletion(completionText, this.FilterScript, wc, wordToComplete, this.InputObject) ->
            let quotedCompletionText =
                maybeQuote
                    shouldQuote
                    canAvoidQuotes
                    quoteType
                    this.RequoteOptions
                    completionText
            new CompletionResult(
                quotedCompletionText,
                tryGetTextOrDefault(this.ListItemText, this.InputObject, completionText) |> coalesceNullOrEmpty completionText,
                this.ResultType,
                tryGetTextOrDefault(this.ToolTip, this.InputObject, completionText) |> coalesceNullOrEmpty completionText
            )
            |> this.WriteObject
        | _ -> ()