namespace PSSharp
open System
open System.Management.Automation
open ArgumentCompletion

module ArgumentCompleterTypesCompletion =
    [<CompiledName("CompleteArgumentCompleterNames")>]
    let completeArgumentCompleterTypes wordToComplete = 
        let { QuotationType = quoteType; UnquotedText = wordToComplete } = trimQuotes wordToComplete
        let wc = WildcardPattern.Get(wordToComplete + "*", WildcardOptions.IgnoreCase)
        AppDomain.CurrentDomain.GetAssemblies()
        |> Seq.filter (fun i -> not i.IsDynamic)
        |> Seq.map (fun a -> a.GetExportedTypes())
        |> Seq.concat
        |> Seq.filter (fun t ->
            (not t.IsAbstract)
            &&  (
                    t.IsAssignableTo(typeof<IArgumentCompleter>)
                ||  t.IsAssignableTo(typeof<ArgumentCompleterAttribute>)
                )
            )
        |> toCompletions
            wordToComplete
            (fun (t, wc) -> wc.IsMatch(t.FullName) || wc.IsMatch(t.Name))
            (fun i -> i.FullName)
            (fun i -> i.FullName)
            CompletionResultType.ParameterValue
            (fun i -> i.FullName)
            RequoteOptions.None

open ArgumentCompleterTypesCompletion

type ArgumentCompleterTypesCompletionAttribute () =
    inherit CompletionBaseAttribute ()

    override _.CompleteArgument(_, _, wordToComplete, _, _) =
        completeArgumentCompleterTypes wordToComplete