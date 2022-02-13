namespace PSSharp
open System
open System.Collections.Generic
open System.Management.Automation
open PSSharp.PSLanguagePrimitives

[<AbstractClass>]
type PSTypeConverter<'a> () =
    inherit PSTypeConverter ()

    override _.CanConvertFrom(sourceValue : obj, destinationType: Type) =
        false
    override _.CanConvertTo(sourceValue : obj, destinationType: Type) =
        false
    override _.ConvertFrom(sourceValue : obj, destinationType : Type, formatProvider: IFormatProvider, ignoreCase : bool) : obj =
        null
    override _.ConvertTo(sourceValue : obj, destinationType: Type, formatProvider : IFormatProvider, ignoreCase : bool) : obj =
        null

[<AbstractClass>]
type FSTypeConverter<'a> () =
    inherit PSTypeConverter ()

    override this.CanConvertTo(sourceValue : obj, destinationType : Type) =
        match sourceValue with
        | :? 'a as sourceValue -> 
            match this.TryConvertTo(sourceValue, destinationType) with
            | ValueSome _ -> true
            | ValueNone -> false
        | _ -> false
    override this.CanConvertFrom(sourceValue : obj, destinationType : Type) =
        if destinationType.IsAssignableFrom(typeof<'a>) then
            match this.TryConvertFrom(sourceValue) with
            | ValueSome _ -> true
            | ValueNone -> false
        else false

    override this.ConvertTo(sourceValue : obj, destinationType : Type, _ : IFormatProvider, _ : bool) : obj =
        match sourceValue with
        | :? 'a as sourceValue ->
            match this.TryConvertTo(sourceValue, destinationType) with
            | ValueSome value -> box value
            | ValueNone -> new InvalidCastException () |> raise
        | _ -> new InvalidCastException () |> raise

    override this.ConvertFrom(sourceValue : obj, destinationType : Type, _ : IFormatProvider, _ : bool) : obj =
        if destinationType.IsAssignableFrom(typeof<'a>) then
            match this.TryConvertFrom(sourceValue) with
            | ValueSome value -> box value
            | ValueNone -> raise <| new InvalidCastException()
        else raise <| new InvalidCastException()

    abstract TryConvertTo : sourceValue : 'a * ``type`` : Type-> 'a voption
    abstract TryConvertFrom : sourceValue : obj -> 'a voption