namespace PSSharp
open System
open System.Reflection
open System.Diagnostics.CodeAnalysis
open System.Management.Automation
open System.ComponentModel

/// Determine which types are provided by a type completer.
[<Struct;RequireQualifiedAccess>]
type TypeNameCompletionCriteria =
    /// Only include public types.
    | Public
    /// Always include private types.
    | Private
    /// Only include private types if no public types matched the word to complete.
    | PrivateFallback
    /// Parse value as TypeNameCompletionCriteria. Return ValueNone if not parsed.
    static member TryParseOption value =
        let inline eq c = String.Equals(value, c, StringComparison.OrdinalIgnoreCase)
        if eq <| nameof Public then
            ValueSome Public
        else if eq <| nameof Private then
            ValueSome Private
        else if eq <| nameof PrivateFallback then
            ValueSome PrivateFallback
        else
            ValueNone
    /// Parse value as TypeNameCompletionCriteria. Return false if not parsed.
    static member TryParse (value: string, v : TypeNameCompletionCriteria outref) =
        match TypeNameCompletionCriteria.TryParseOption value with
        | ValueSome parsed -> v <- parsed ; true
        | ValueNone -> false
    /// Parse value as TypeNameCompletionCriteria. Raise FormatException if not parsed.
    static member Parse (value : string) =
        match TypeNameCompletionCriteria.TryParseOption value with
        | ValueSome parsed -> parsed
        | ValueNone -> raise <| new FormatException()

/// Reflection-based argument completion logic.
[<EditorBrowsable(EditorBrowsableState.Never)>]
module ReflectionCompletion =
    let inline private getAssemblies () = AppDomain.CurrentDomain.GetAssemblies()
    let inline private assemblyFilter ([<DisallowNull; NotNull>] assembly : Assembly) = not assembly.IsDynamic
    let private mapToTypes (includePrivateTypes: bool) (assembly : Assembly) =
        match includePrivateTypes with
        | true -> assembly.GetTypes()
        | false -> assembly.GetExportedTypes()
    [<Literal>]
    let private ParameterValue = CompletionResultType.ParameterValue

    /// Get all types loaded into the current Application Domain.
    /// Filters to exported types if includePrivateTypes is not true.
    [<CompiledName("GetTypes")>]
    let getTypes (includePrivateTypes : bool) =
        getAssemblies()
        |> Seq.filter assemblyFilter
        |> Seq.map (mapToTypes includePrivateTypes)
        |> Seq.concat
        |> Seq.toArray

    /// Identifies a ToolTip to offer with an argument completion for a Type instance.
    [<CompiledName("SelectTypeToolTip")>]
    let selectTypeToolTip ([<DisallowNull; NotNull>] t : Type) =
        sprintf """Type         : %s
Base Type    : %s%s
Namespace    : %s
Assembly     : %s
"""
            t.Name
            (
                match t.BaseType with
                | null -> ""
                | t -> t.FullName
            )
            (
                match t.DeclaringType with
                | null -> ""
                | t -> sprintf "\nDeclaring Type: %s" t.FullName
            )
            t.Namespace
            t.Assembly.FullName

    /// Gets argument completions for types loaded into the AppDomain.
    [<CompiledName("GetTypeCompletions")>] 
    let getTypeCompletions
        (criteria : TypeNameCompletionCriteria)
        ([<DisallowNull; NotNull>] selectCompletionText: Type -> string) 
        ([<System.Diagnostics.CodeAnalysis.AllowNull>] wordToComplete : string) =
        
        let inline getCompletions includePrivate =
            includePrivate
            |> getTypes 
            |> ArgumentCompletion.toCompletions
                wordToComplete
                (fun (t, wc) -> wc.IsMatch(t.Name) || wc.IsMatch(t.FullName) || wc.IsMatch(selectCompletionText(t)))
                selectCompletionText
                selectCompletionText
                ParameterValue
                selectTypeToolTip
                ArgumentCompletion.RequoteOptions.None
             |> Seq.toArray
        match criteria with
        | TypeNameCompletionCriteria.Public ->
            getCompletions false
        | TypeNameCompletionCriteria.Private ->
            getCompletions true
        | TypeNameCompletionCriteria.PrivateFallback ->
            let completions = getCompletions false
            match completions.Length with
            | 0 -> getCompletions true
            | _ -> completions

    /// Gets argument completions for assemblies loaded into the AppDomain.
    [<CompiledName("GetAssemblyCompletions")>] 
    let getAssemblyCompletions
        ([<DisallowNull>] selectCompletionText: Assembly -> string)
        ([<System.Diagnostics.CodeAnalysis.AllowNull>] wordToComplete: string) =
        getAssemblies ()
        |> Seq.filter assemblyFilter
        |> ArgumentCompletion.toCompletions
            wordToComplete
            (fun (a, wc) -> a |> selectCompletionText |> wc.IsMatch)
            selectCompletionText
            selectCompletionText
            ParameterValue
            selectCompletionText
            ArgumentCompletion.RequoteOptions.None

    /// Gets aregument compmletions for namespaces loaded into the AppDomain.
    [<CompiledName("GetNamespaceCompletions")>]
    let getNamespaceCompletions
        ([<System.Diagnostics.CodeAnalysis.AllowNull>] wordToComplete: string) =
        getAssemblies()
        |> Seq.filter assemblyFilter
        |> Seq.map (fun a -> a.GetTypes())
        |> Seq.concat
        |> Seq.map (fun t -> t.Namespace)
        |> Seq.distinct
        |> ArgumentCompletion.toCompletions
            wordToComplete
            (fun (ns, wc) -> wc.IsMatch(ns))
            id
            id
            ParameterValue
            id
            ArgumentCompletion.RequoteOptions.None
