namespace PSSharp
open ArgumentCompletion
open ReflectionCompletion
open System.Management.Automation
open System.Reflection
open System

module AttributeTypesCompletion =
    [<CompiledName("CompleteAttributeTypeNames")>]
    let completeAttributeTypeNames wordToComplete = 
        let { QuotationType = quoteType; UnquotedText = wordToComplete } = trimQuotes wordToComplete
        let wc = WildcardPattern.Get(wordToComplete + "*", WildcardOptions.IgnoreCase)
        AppDomain.CurrentDomain.GetAssemblies()
        |> Seq.filter (fun i -> not i.IsDynamic)
        |> Seq.collect (fun a -> a.GetExportedTypes())
        |> Seq.filter (fun t ->
            (not t.IsAbstract)
            && t.IsAssignableTo(typeof<Attribute>)
            )
        |> toCompletions
            wordToComplete
            (fun (t, wc) -> wc.IsMatch(t.FullName) || wc.IsMatch(t.Name))
            (fun i -> i.FullName)
            (fun i -> i.FullName)
            CompletionResultType.ParameterValue
            (fun i -> i.FullName)
            RequoteOptions.None

/// Generate completion results for attribute type names of attribute types loaded into the application domain.
type AttributeTypesCompletionAttribute () =
    inherit CompletionBaseAttribute ()

    override _.CompleteArgument(_, _, wordToComplete, _, _) =
        AttributeTypesCompletion.completeAttributeTypeNames wordToComplete