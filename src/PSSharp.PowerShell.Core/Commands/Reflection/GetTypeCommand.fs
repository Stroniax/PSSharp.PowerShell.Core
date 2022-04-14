namespace PSSharp.Commands
open PSSharp
open System
open System.Reflection
open System.Management.Automation

module GetTypeCommand =
    open Microsoft.FSharp.Core

    let private errorIfExists element cannotContain list =
        match list |> List.contains cannotContain with
        | true -> Error (element, cannotContain)
        | false -> Ok()
    let private errorIfAnyExists element cannotContainAny list =
        let mutable errors = List.empty
        for each in cannotContainAny do
            match errorIfExists element each list with
            | Ok() -> ()
            | Error er -> errors <- er :: errors
        match List.isEmpty errors with
        | true -> Ok()
        | _ -> Error errors

    [<Struct>]
    type TypeFilter =
        | Name of name: WildcardPattern
        | RequireValueType
        | RequireReferenceType
        | RequireInterface
        | RequireAbstract
        | RequireImplemented
        | RequireGeneric
        | ExcludeGeneric
        | RequireEnum
        | ExcludeEnum
        static member internal CheckConflicting (typeFilter: TypeFilter list) =
            match typeFilter with
            | [] -> Ok()
            | head :: tail ->
                let res =
                    match head with
                    | RequireValueType -> errorIfAnyExists head [RequireReferenceType] tail
                    | RequireReferenceType -> errorIfAnyExists head [RequireValueType; RequireEnum] tail
                    | RequireInterface -> errorIfAnyExists head [RequireValueType; RequireImplemented; RequireEnum] tail
                    | RequireAbstract -> errorIfAnyExists head [RequireValueType; RequireImplemented; RequireEnum] tail
                    | RequireImplemented -> errorIfAnyExists head [RequireInterface; RequireAbstract] tail
                    | RequireGeneric -> errorIfAnyExists head [ExcludeGeneric; RequireEnum] tail
                    | ExcludeGeneric -> errorIfAnyExists head [RequireGeneric] tail
                    | RequireEnum -> errorIfAnyExists head [ExcludeEnum; RequireReferenceType; RequireInterface; RequireAbstract; RequireGeneric] tail
                    | ExcludeEnum -> errorIfAnyExists head [RequireEnum] tail
                    | _ -> Ok()

                let remainingErrors = TypeFilter.CheckConflicting tail
                match res with
                | Ok() -> remainingErrors
                | Error errors -> 
                    match remainingErrors with
                    | Ok() -> Error errors
                    | Error moreErrors -> Error (errors @ moreErrors)

    [<CompiledName("GetFilterFunction")>]
    let getFilterFunction typeFilter =
        match typeFilter with
        | Name wc -> fun (t: Type) -> wc.IsMatch(t.Name) || wc.IsMatch(t.FullName)
        | RequireValueType -> fun (t: Type) -> t.IsValueType
        | RequireReferenceType -> fun (t: Type) -> t.IsByRef
        | RequireInterface -> fun (t: Type) -> t.IsInterface
        | RequireAbstract -> fun (t: Type) -> t.IsAbstract
        | RequireImplemented -> fun (t: Type) -> not t.IsAbstract
        | RequireGeneric -> fun (t: Type) -> t.IsGenericTypeDefinition
        | ExcludeGeneric -> fun (t: Type) -> not t.IsGenericTypeDefinition
        | RequireEnum -> fun (t: Type) -> t.IsEnum
        | ExcludeEnum -> fun (t: Type) -> not t.IsEnum
    let andPredicate pred1 pred2 inp =
        pred1 inp && pred2 inp
    let orPredicate pred1 pred2 inp =
        pred1 inp || pred2 inp
    let andPredicates predicates inp =
        let rec inner predicates =
            match predicates with
            | [] -> true
            | [head] -> head inp
            | head :: tail -> head inp && inner tail
        inner predicates
    let orPredicates predicates inp =
        let rec inner predicates =
            match predicates with
            | [] -> true
            | [head] -> head inp
            | head :: tail -> head inp || inner tail
        inner predicates
    let getAllFilters typeFilters =
        let rec inner typeFilters =
            match typeFilters with
            | [] -> fun _ -> true
            | [typeFilter] -> getFilterFunction typeFilter
            | head :: tail -> andPredicate (getFilterFunction head) (inner tail)
        typeFilters
        |> List.distinct
        |> inner

    let failIfConflicting typeFilters =
        let mapErrorToException (value, conflicting) =
            new InvalidOperationException(
                String.Format(
                    ErrorMessages.ConflictingTypeFilterInterpolated,
                    value,
                    conflicting
                    )
                )
                :> exn
        match TypeFilter.CheckConflicting typeFilters with
        | Ok() -> typeFilters
        | Error conflicts ->
            let exns = conflicts |> List.map mapErrorToException
            let exn = new AggregateException(exns)
            raise exn

    let getTypesWithFilters (includePrivateTypes: bool) (filters: TypeFilter list) =
        let filterTypes = 
            filters
            |> failIfConflicting
            |> getAllFilters
        let getTypes =
            match includePrivateTypes with
            | true -> fun (assembly: Assembly) -> assembly.GetTypes()
            | false -> fun (assembly: Assembly) -> assembly.GetExportedTypes()
        let filterAssemblies (assembly: Assembly) = not assembly.IsDynamic

        AppDomain.CurrentDomain.GetAssemblies()
        |> Array.toList
        |> List.filter filterAssemblies
        |> List.collect (getTypes >> Array.toList)
        |> List.filter filterTypes

    let filterByAnyDerivedType types derivedFromAnyOf =
        let derivedFromPredicate = 
            derivedFromAnyOf
            |> List.map (fun (i: Type) -> fun (t: Type) -> t.IsAssignableTo(i))
            |> orPredicates
        types |> List.filter derivedFromPredicate
    let filterByAnyImplementedInterface types implementsAny =
        let implementsPredicate =
            implementsAny
            |> List.map (fun (i: Type) -> fun (t: Type) -> t.GetInterfaces() |> Array.contains i)
            |> orPredicates
        types |> List.filter implementsPredicate

[<Cmdlet(VerbsCommon.Get, Nouns.Type, DefaultParameterSetName = "TypeNameSet")>]
type GetTypeCommand () =
    inherit Cmdlet ()

    [<Parameter(Position = 0, ParameterSetName = "TypeNameSet")>]
    [<ValidateNotNullOrEmpty>]
    [<SupportsWildcards>]
    [<TrimTypeBracketTransformation>]
    [<TypeNameCompletion>]
    member val TypeName = Array.empty with get, set

    [<Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "ObjectSet")>]
    [<AllowNull>]
    [<AllowEmptyString>]
    [<AllowEmptyCollection>]
    [<NoCompletion>]
    member val InputObject = psnull with get, set

    /// Include private types
    [<Parameter(ParameterSetName = "TypeNameSet")>]
    [<Alias("IncludePrivateTypes")>]
    member val Force = SwitchParameter() with get, set

    /// Base class of types to return.
    [<Parameter(ParameterSetName = "TypeNameSet")>]
    [<Alias("BaseType")>]
    [<TypeNameCompletion>]
    [<TrimTypeBracketTransformation>]
    member val DerivedFrom = Array.empty with get, set

    [<Parameter(ParameterSetName = "TypeNameSet")>]
    [<Alias("Interface")>]
    [<InterfaceNameCompletion>]
    [<TrimTypeBracketTransformation>]
    member val ImplementsInterface = Array.empty with get, set

    [<Parameter(ParameterSetName = "TypeNameSet")>]
    member val IsInterface = SwitchParameter() with get, set

    [<Parameter(ParameterSetName = "TypeNameSet")>]
    member val IsAbstract = SwitchParameter() with get, set

    [<Parameter(ParameterSetName = "TypeNameSet")>]
    member val IsStatic = SwitchParameter() with get, set

    [<Parameter(ParameterSetName = "TypeNameSet")>]
    member val IsSealed = SwitchParameter() with get, set

    [<Parameter(ParameterSetName = "TypeNameSet")>]
    member val IsGeneric = SwitchParameter() with get, set

    member val IsValueType = SwitchParameter() with get, set

    [<Parameter(ParameterSetName = "TypeNameSet")>]
    member val IsEnum = SwitchParameter() with get, set