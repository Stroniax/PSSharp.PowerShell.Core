namespace PSSharp
open System.Management.Automation

/// Wrapper module for the LanguagePrimitives static class with helper methods.
module PSLanguagePrimitives =

    /// Gets an enumerable if <paramref name="obj"/> is an enumerable type according to the PowerShell engine.
    [<CompiledName("TryGetEnumerable")>]
    let tryGetEnumerable ([<System.Diagnostics.CodeAnalysis.AllowNull>] obj) =
        obj
        |> LanguagePrimitives.GetEnumerable
        |> ValueOption.ofObj

    /// Gets an enumerable if <paramref name="obj"/> is an enumerable type according to the PowerShell engien.
    [<return: Struct>]
    let (|PSIsEnumerable|_|) obj =
        match LanguagePrimitives.IsObjectEnumerable obj with
        | true -> obj |> LanguagePrimitives.GetEnumerable |> ValueSome
        | false -> ValueNone

    /// Casts an object to the requested type if possible by the langauge. Otherwise, an exception will be raised.
    [<CompiledName("Cast")>]
    let cast (obj) : 'a =
        LanguagePrimitives.ConvertTo<'a>(obj)

    /// Casts an object to the requested type if possible by the language.
    [<CompiledName("TryCast")>]
    let tryCast (obj: obj) : 'a voption =
        match LanguagePrimitives.TryConvertTo<'a> obj with
        | true, value -> ValueSome value
        | false, _ -> ValueNone

    /// Converts an object to the requested type if possible by the language.
    [<return: Struct>]
    let (|PSAsType|_|) (obj: obj) : 'a voption = tryCast obj
