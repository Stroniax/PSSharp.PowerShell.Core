namespace PSSharp
open System

/// Methods for working with nullable reference types, even when the corresponding type
/// does not generally allow for nullability. Often useful during interop, such as between
/// C# or PowerShell and F#.
module NullableReference =
    let (|Null|NotNull|) v =
        match box v with
        | null -> Null
        | _ -> NotNull
    let iter fn n =
        match box n with
        | null -> ()
        | _ -> fn n
    let get n =
        match box n with
        | null -> raise <| new NullReferenceException()
        | _ -> n
    let map fn n =
        match box n with
        | null -> None
        | _ -> n |> fn |> Some
    let mapv fn n =
        match box n with
        | null -> ValueNone
        | _ -> n |> fn |> ValueSome
    let bind fn n =
        match box n with
        | null -> None
        | _ -> n |> fn
    let bindv fn n =
        match box n with
        | null -> ValueNone
        | _ -> n |> fn
    let isEmpty n =
        match box n with
        | null -> true
        | _ -> false
    let toList n =
        match box n with
        | null -> List.empty
        | _ -> [n]
    let toArray n =
        match box n with
        | null -> Array.empty
        | _ -> [|n|]
    let defaultValue v n =
        match box n with
        | null -> v
        | _ -> n
    let defaultWith fn n =
        match box n with
        | null -> n
        | _ -> fn()
    let toOption n =
        match box n with
        | null -> None
        | _ -> Some n
    let toValueOption n =
        match box n with
        | null -> ValueNone
        | _ -> ValueSome n