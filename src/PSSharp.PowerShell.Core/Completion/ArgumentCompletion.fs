namespace PSSharp
open System
open System.Management.Automation
open System.Diagnostics.CodeAnalysis

module ArgumentCompletion =
    /// Types of quotations, specifically as pertain to enclosing a PowerShell parameter value.
    [<Struct>]
    type QuotationType =
        /// The input text was not in quotation marks (e.g. Test-Command -Parameter Value).
        | NoQuote
        /// The input text was single-quoted (e.g. Test-Command -Parameter 'Value').
        | SingleQuote
        /// The input text was double-quoted (e.g. Test-Command -Parameter "Value").
        | DoubleQuote
        /// Parses the name of a QuoteType into the corresponding QuoteType, or returns ValueNone.
        static member TryParseOption ([<AllowNull>] name) =
            let inline eq v = String.Equals(name, v, StringComparison.OrdinalIgnoreCase)
            if eq <| nameof NoQuote then ValueSome NoQuote
            else if eq <| nameof SingleQuote then ValueSome SingleQuote
            else if eq <| nameof DoubleQuote then ValueSome DoubleQuote
            else ValueNone
        /// Parses the name of a QuoteType into the corresponding QuoteType, or returns false.
        static member TryParse ([<AllowNull>] name, v: QuotationType outref) =
            match QuotationType.TryParseOption name with
            | ValueSome value -> v <- value; true
            | ValueNone -> false
        /// Parses the name of a QuoteType into the corresponding QuoteType, or raises a FormatException.
        static member Parse ([<DisallowNull; NotNull>] name) =
            match QuotationType.TryParseOption name with
            | ValueSome value -> value
            | ValueNone -> raise <| new FormatException()
        /// Gets the quotation type used by a string.
        static member GetQuotationType([<AllowNull>] string : string) =
            if String.IsNullOrWhiteSpace(string) then NoQuote
            else if string.StartsWith ''' then SingleQuote
            else if string.StartsWith '"' then DoubleQuote
            else NoQuote

        /// The character represented by the QuoteType, or null.
        member this.Char =
            match this with
            | SingleQuote -> Nullable<char> '''
            | DoubleQuote -> Nullable<char> '"'
            | NoQuote -> Nullable<char> ()
        /// Gets the character represented by the QuoteType, or ValueNone.
        member this.TryGetChar() =
            this.Char |> ValueOption.ofNullable
        /// Trims the current quote type from a string.
        [<return: NotNull>]
        member this.Trim ([<AllowNull>] string : string) =
            if String.IsNullOrEmpty(string) then "" else
            match this.TryGetChar() with
            | ValueSome char ->
                if not <| string.StartsWith(char) then string else
                match string.EndsWith(char) with
                | true -> string.Substring(1, string.Length - 2)
                | false -> string.Substring(1)
            | ValueNone -> string

    /// Options that may be provided when re-quoting text that will be offered for completion.
    [<Flags>]
    type RequoteOptions =
        /// No options.
        | None = 0
        /// <summary>
        /// Do not escape single quotes in the completion text before wrapping it in single quotes.
        /// This flag is only checked with <see cref="QuotationType.SingleQuote"/>.
        /// </summary>
        | NoEscapeSingleQuote = 1
        /// <summary>
        /// Do not escape double quotes in the completion text before wrapping it in double quotes.
        /// This flag is only checked with <see cref="QuotationType.DoubleQuote"/>.
        /// </summary>
        | NoEscapeDoubleQuote = 2
        /// <summary>
        /// Escape wildcards in the completion text before wrapping the return value.
        /// </summary>
        | EscapeWildcard = 4
        /// <summary>
        /// Do not escape variables in the completion text before wrapping the return value.
        /// This flag is only applicable for QuotationType.DoubleQuote.
        /// </summary>
        | NoEscapeVariable = 8
        /// <summary>
        /// Do not wrap the return value in quotes unless the text contains a space (" ").
        /// More specifically, remove quotes if provided but not necessary.
        /// </summary>
        | TryAvoidQuotes = 16
        /// <summary>
        /// If the quotation type is <see cref="QuotationType.None"/> and the input text contains
        /// a space (" "), wrap the return value in double quotes instead of single quotes.
        /// </summary>
        | PreferDoubleQuote = 32
    
    [<Struct>]
    type TrimQuotesResult =
        {
            QuotationType: QuotationType
            [<NotNull>] UnquotedText: string
        }

    /// Trims quotes from a string and returns the trimmed string and the quote type identified.
    [<CompiledName("TrimQuotes")>]
    let trimQuotes ([<AllowNull>] string: string) =
        let quoteType = QuotationType.GetQuotationType(string)
        let unquotedText = quoteType.Trim(string)
        {
            QuotationType = quoteType
            UnquotedText = unquotedText
        }

    [<CompiledName("ShouldQuote")>]
    /// Determine if a string needs some form of quotation.
    let shouldQuote ([<AllowNull>] string: string) =
        not(String.IsNullOrEmpty(string))
        && (string.Contains(' ')
        || string.Contains('$')
        || string.Contains('`')
        || string.Contains(''')
        || string.Contains('"'))

    [<CompiledName("CanAvoidQuotes")>]
    let canAvoidQuotes ([<AllowNull>] string: string) =
        not <| shouldQuote string
    
    /// Single-quotes a string, escaping as indicated by quoteOptions.
    [<CompiledName("SingleQuote")>]
    let singleQuote
        ([<DisallowNull>] canAvoidQuotes : string -> bool)
        (quoteOptions: RequoteOptions)
        ([<AllowNull>] string: string) =
        let mutable string = match string with null -> "" | n -> n
        if not <| quoteOptions.HasFlag(RequoteOptions.NoEscapeSingleQuote) then
            string <- Language.CodeGeneration.EscapeSingleQuotedStringContent(string)
        if quoteOptions.HasFlag(RequoteOptions.EscapeWildcard) then
            string <- WildcardPattern.Escape(string)
        // variables are effectively escaped by single-quoting the string
        match quoteOptions.HasFlag(RequoteOptions.TryAvoidQuotes) with
        | true ->
            match canAvoidQuotes string with
            | true -> sprintf "'%s'" string
            | false -> string
        | false -> sprintf "'%s'" string

    /// Double-quotes a string, escaping as indicated by quoteOptions.
    [<CompiledName("DoubleQuote")>]
    let doubleQuote
        ([<DisallowNull>] canAvoidQuotes: string -> bool)
        (quoteOptions: RequoteOptions)
        ([<AllowNull>] string: string) =
        let mutable string = match string with null -> "" | n -> n
        if not <| quoteOptions.HasFlag(RequoteOptions.NoEscapeDoubleQuote) then
            string <- string.Replace("\"", "\"\"")
        if quoteOptions.HasFlag(RequoteOptions.EscapeWildcard) then
            string <- WildcardPattern.Escape(string)
        if not <| quoteOptions.HasFlag(RequoteOptions.NoEscapeVariable) then
            string <- Language.CodeGeneration.EscapeVariableName(string)
        match quoteOptions.HasFlag(RequoteOptions.TryAvoidQuotes) with
        | true ->
            match canAvoidQuotes string with
            | true ->  sprintf "\"%s\"" string
            | false -> string
        | false -> sprintf "\"%s\"" string

    /// Adds quotes to a string (even if it already is quoted or does not
    /// need quoted, unless quoteOptions has the TryAvoidQuotes flag and
    /// canAvoidQuotes returns true).
    [<CompiledName("Quote")>]
    let quote 
        ([<DisallowNull>] canAvoidQuotes : string -> bool)
        (quoteType : QuotationType)
        (quoteOptions: RequoteOptions)
        ([<AllowNull>] string: string) =
        match quoteType with
        | NoQuote ->
            match quoteOptions.HasFlag(RequoteOptions.PreferDoubleQuote) with
            | true  -> string |> doubleQuote canAvoidQuotes quoteOptions
            | false -> string |> singleQuote canAvoidQuotes quoteOptions
        | SingleQuote -> string |> singleQuote canAvoidQuotes quoteOptions
        | DoubleQuote -> string |> doubleQuote canAvoidQuotes quoteOptions
        
    /// Quote a string if shouldQuote resolves to true. Otherwise, returns the unchanged string.
    /// The ArgumentCompletion module provides a default shouldQuote and canAvoidQuotes method.
    [<CompiledName("MaybeQuote")>]
    let maybeQuote
        ([<DisallowNull>] shouldQuote: string -> bool)
        ([<DisallowNull>] canAvoidQuotes: string -> bool)
        (quoteType: QuotationType)
        (quoteOptions : RequoteOptions)
        ([<AllowNull>] string : string) =
        match quoteType <> QuotationType.NoQuote || shouldQuote string with
        | true  -> string |> quote canAvoidQuotes quoteType quoteOptions
        | false -> string

    /// Filters a collection of source data and maps completion results for the matches.
    [<CompiledName("ToCompletions")>]
    let toCompletions
        ([<AllowNull>] wordToComplete: string)
        ([<DisallowNull>] predicate: 'a * WildcardPattern -> bool)
        ([<DisallowNull>] selectCompletionText: 'a -> string)
        ([<DisallowNull>] selectListItemText: 'a -> string)
        (completionType: CompletionResultType)
        ([<DisallowNull>] selectToolTip: 'a -> string)
        (quoteOptions: RequoteOptions)
        ([<AllowNull>] completionSource: 'a seq)
        : CompletionResult seq
        =
        if completionSource = null then Seq.empty else
        let {QuotationType = quoteType; UnquotedText = t} = trimQuotes wordToComplete
        let wco =
            match Linq.Enumerable.TryGetNonEnumeratedCount(completionSource) with
            | true, count when count > 1000 -> WildcardOptions.IgnoreCase ||| WildcardOptions.Compiled
            | _ -> WildcardOptions.IgnoreCase
        let wc = WildcardPattern.Get(t + "*", wco)
        // PowerShell runs into a CLS noncompliance error when I return seq{}
        // if I call this method in PowerShell directly
        [|
            for item in completionSource do
                match predicate(item, wc) with
                | true ->
                    let completionText =
                        maybeQuote
                            shouldQuote
                            canAvoidQuotes
                            quoteType
                            quoteOptions
                            (selectCompletionText item)
                    new CompletionResult(
                        completionText,
                        selectListItemText(item),
                        completionType,
                        selectToolTip(item)
                        )
                | false -> ()
        |]