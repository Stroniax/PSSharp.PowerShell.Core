namespace PSSharp
open System
open System.Management.Automation
open Microsoft.FSharp.Reflection
open ArgumentCompletion

/// Completes the option names of F#'s Discriminated Union types.
type DiscriminatedUnionNameCompletionAttribute (``type``: Type) =
    inherit CompletionBaseAttribute ()

    static let selectName (v : UnionCaseInfo) = v.Name
    static let predicate (v: UnionCaseInfo, wc : WildcardPattern) = wc.IsMatch(v.Name)
    static let selectToolTip (v: UnionCaseInfo) =
        sprintf "Name: %s\nTag: %i\nFields:\n\t%s"
            v.Name
            v.Tag
            (String.Join(
                "\n\t",
                (v.GetFields() |> Seq.map (fun f -> f.ToString()))
                ))
    do match FSharpType.IsUnion ``type`` with
        | true-> ()
        | false -> raise <| new ArgumentException(ErrorMessages.TypeNotDiscriminatedUnion)

    member _.Type = ``type``

    override _.CompleteArgument(_, _, wordToComplete, _, _) =
        FSharpType.GetUnionCases(``type``)
        |> toCompletions
            wordToComplete
            predicate
            selectName
            selectName
            CompletionResultType.ParameterValue
            selectToolTip
            RequoteOptions.None

/// Completes the option names of F#'s Discriminated Union types.
[<Sealed>]
type DiscriminatedUnionNameCompletionAttribute<'a> () =
    inherit DiscriminatedUnionNameCompletionAttribute(typeof<'a>)