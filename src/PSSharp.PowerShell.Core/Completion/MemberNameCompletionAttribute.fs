namespace PSSharp
open System
open System.Reflection
open System.Collections
open System.Collections.Generic
open System.Management.Automation

/// Defines different methods that the MemberNameCompletionAttribute may
/// use to identify the type to complete members from.
[<Struct>]
type internal MemberNameCompletionType =
/// Complete from a type defined by another parameter.
| ParameterValue of parameterName: string
/// Complete from a type defined at compile time.
| CompiledType of ``type``: Type

/// Offers compiled member names as completions for a parameter.
type MemberNameCompletionAttribute private (typeSource: MemberNameCompletionType) =
    inherit CompletionBaseAttribute()
    
    static let predicate (m: MemberInfo, wc: WildcardPattern) = wc.IsMatch(m.Name)
    static let selectCompletionText (m: MemberInfo) = m.Name
    static let selectToolTipText (m: MemberInfo) = sprintf "%s $this.%s" (string m.MemberType) m.Name

    /// Instantiate a new completion attribute to offer completions based on members of the type provided.
    new(``type``: Type) = MemberNameCompletionAttribute(CompiledType ``type``)
    /// Instantiate a new completion attribute to offer completions based on members of the type
    /// passed to the parameter with the name provided.
    new(parameterName: string) = MemberNameCompletionAttribute(ParameterValue parameterName)

    member val BindingFlags = BindingFlags.Default with get, set
    member val MemberTypes = MemberTypes.All with get, set

    member private this.TryGetType (fakeBoundParams: IDictionary) =
        match typeSource with
        | CompiledType t -> Some t
        | ParameterValue parameterName when fakeBoundParams.Contains(parameterName) -> fakeBoundParams[parameterName] |> psTryCast
        | _ -> None

    override this.CompleteArgument(commandName, parameterName, wordToComplete, commandAst, fakeBoundParams) =
        match this.TryGetType(fakeBoundParams) with
        | None -> List.Empty
        | Some typ ->
            typ.GetMembers(this.BindingFlags)
            |> Array.filter (fun m -> this.MemberTypes.HasFlag(m.MemberType))
            |> ArgumentCompletion.toCompletions
                wordToComplete
                predicate
                selectCompletionText
                selectToolTipText
                CompletionResultType.ParameterValue
                selectToolTipText
                ArgumentCompletion.RequoteOptions.None
            