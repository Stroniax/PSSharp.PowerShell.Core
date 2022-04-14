namespace PSSharp

/// Trims square brackets wrapping a type name on string parameter arguments wrapped
/// in square brackets.
type TrimTypeBracketTransformationAttribute () =
    inherit EnumeratedArgumentTransformationAttribute ()

    static let shouldTrimTypeBrackets (str: string) =
        str.StartsWith '[' && str.EndsWith ']'
    static let trimTypeBrackets (str: string) =
        match shouldTrimTypeBrackets str with
        | true -> str.Substring(1, str.Length - 2)
        | _ -> str

    static member TrimTypeBrackets str = trimTypeBrackets str

    override _.TransformElement(_, v) =
        match v with
        | :? string as typeName ->
            TransformationResult.Single(trimTypeBrackets typeName)
        | _ -> NotTransformed
