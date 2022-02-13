namespace PSSharp
open System
open System.Collections
open System.Collections.Generic
open System.Management.Automation

[<Struct>]
type TransformationResult<'a> =
/// The value should be returned as input.
| NotTransformed
/// The result has been transformed to a collection and should be returned as a collection.
| Collection of collection: 'a seq
/// The result has been transformed to a single value and should be returned as a single value.
| Single of value: 'a
    /// Converts to a TransformationResult.Single with the value that is cast.
    static member op_Implicit single =
        Single single
    /// Converts to a TransformationResult.FlattenableCollection with the collection that is cast.
    static member op_Implicit collection =
        TransformationResult<'a>.FlattenableCollection(collection)
    /// Creates a TransformationResult.Single if the collection has only one element; otherwise,
    /// creates a TransformationResult.Collection with the entire collection.
    static member FlattenableCollection collection =
        match Seq.tryExactlyOne collection with
        | Some single -> Single single
        | _ -> Collection collection
    member this.GetValuesOrDefault(``default`` : 'a seq) =
        match this with
        | NotTransformed -> ``default``
        | Collection collection -> collection
        | Single single -> [|single|]
    member this.GetValuesOrGetFrom(action : unit -> 'a seq) =
        match this with
        | NotTransformed -> action()
        | Collection collection -> collection
        | Single single -> [|single|]

module TransformationResult =
    let notTransformed = NotTransformed
    let NotTransformed () = NotTransformed
    let Collection collection = Collection collection
    let Single single = Single single
    let FlattenableCollection flattenableCollection =
        TransformationResult<_>.FlattenableCollection flattenableCollection

    [<return: Struct>]
    let (|FlattenableCollection|_|) result =
        match result with
        | NotTransformed -> ValueNone
        | Collection items -> ValueSome items
        | Single item -> ValueSome [|item|]

    let private canMerge i =
        i <> notTransformed
    
    /// Combine the values of first and second. Fail if first or second is NotTransformed.
    [<CompiledName("Merge")>]
    let merge allowFlatten first second =
        let allValues =
            [|
            match first with
            | NotTransformed -> invalidOp "Cannot merge from NotTransformed."
            | Single item -> yield item
            | Collection items -> yield! items
            match second with
            | NotTransformed -> invalidOp "Cannot merge from NotTransformed."
            | Single item -> yield item
            | Collection items -> yield! items
            |]
        match allowFlatten with
        | true -> FlattenableCollection allValues
        | false -> Collection allValues
    /// Combine the values of first and second. Return ValueNone if first or second is NotTransformed.
    [<CompiledName("TryMerge")>]
    let tryMerge allowFlatten first second =
        match canMerge first && canMerge second with
        | true -> merge allowFlatten first second |> ValueSome
        | false -> ValueNone

    /// Merge two TransformationResults of different types by mapping the values.
    /// Fail if first or second is NotTransformed.
    [<CompiledName("MergeMap")>]
    let mergeMap
        (first : TransformationResult<'a>)
        (second : TransformationResult<'b>)
        (mapFirst : 'a -> 'c)
        (mapSecond : 'b -> 'c)
        =
        FlattenableCollection
            [|
            match first with
            | NotTransformed -> invalidOp "Cannot merge from NotTransformed."
            | Single item -> [|mapFirst item|]
            | Collection items -> [|for item in items do mapFirst item|]

            match second with
            | NotTransformed -> invalidOp "Cannot merge from NotTransformed."
            | Single item -> [|mapSecond item|]
            | Collection items -> [|for item in items do mapSecond item|]
            |]
    /// Merge two TransformationResults of different types by mapping the values.
    /// Return ValueNone if first or second is NotTransformed.
    [<CompiledName("TryMergeMap")>]
    let tryMergeMap 
        (first : TransformationResult<'a>)
        (second : TransformationResult<'b>)
        (mapFirst : 'a -> 'c)
        (mapSecond : 'b -> 'c)
        =
        if canMerge first && canMerge second then
            ValueSome (mergeMap first second mapFirst mapSecond)
        else ValueNone

    [<CompiledName("Map")>]
    let map ifNotTransformed fn result =
        match result with
        | NotTransformed -> ifNotTransformed()
        | Single item -> Single <| fn item
        | Collection items -> Collection <| [|for item in items do fn item|]

    [<CompiledName("Cast")>]
    let cast ifNotTransformed (result: TransformationResult<_>) : TransformationResult<'b> =
        match result with
        | NotTransformed -> ifNotTransformed()
        | Single item -> TransformationResult<_>.Single(box item :?> 'b)
        | Collection items -> TransformationResult<_>.Collection <| [|for item in items do box item :?> 'b|]

    let box ifNotTransformed result =
        match result with
        | NotTransformed -> ifNotTransformed()
        | Single item -> Single (box item)
        | Collection items -> Collection([|for item in items do box item|])

    [<CompiledName("DefaultWith")>]
    let defaultWith result defaultValue =
        match result with
        | NotTransformed -> defaultValue
        | _ -> result
    [<CompiledName("DefaultWithAction")>]
    let defaultWithAction result getDefaultValue =
        match result with
        | NotTransformed -> getDefaultValue ()
        | _ -> result

    [<CompiledName("Iter")>]
    let iter fn ifNotTransformed result =
        match result with
        | NotTransformed -> ifNotTransformed()
        | Single value -> fn value
        | Collection values -> Seq.iter fn values

    let enumerate ifNotTransformed result =
        match result with
        | NotTransformed -> ifNotTransformed()
        | Collection values -> values
        | Single value -> [|value|]
    
    let unwrap (ifNotTransformed : unit -> obj) result =
        match result with
        | NotTransformed -> ifNotTransformed()
        | Collection values -> Operators.box values
        | Single value -> Operators.box value

    let concat (many : TransformationResult<'a> seq) =
        let rec concat (restEnumerator : IEnumerator<TransformationResult<'a>>) (first: TransformationResult<'a>) =
            match restEnumerator.MoveNext() with
            | true -> 
                merge true first restEnumerator.Current
                |> concat restEnumerator
            | false -> first
        use enumerator = many.GetEnumerator()
        match enumerator.MoveNext() with
        | true -> concat enumerator enumerator.Current
        | false -> Collection(Array.empty)
