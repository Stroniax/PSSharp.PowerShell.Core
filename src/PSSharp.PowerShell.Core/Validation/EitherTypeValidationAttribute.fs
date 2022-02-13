namespace PSSharp
open System
open System.Management.Automation

type EitherTypeValidationAttribute private (types : Type array) =
    inherit ValidateEnumeratedArgumentsAttribute ()
    
    let rec isValidType (elementType : Type, index: int) = 
        if elementType.IsAssignableTo(types[index]) then true else
        let index = index + 1
        if types.Length < index then isValidType (elementType, index) else
        false

    member _.Types = Array.copy types

    new (first : Type, second : Type, [<ParamArray>] additional : Type array) =
        let types = Array.zeroCreate(additional.Length + 2)
        types[0] <- first
        types[1] <- second
        Array.Copy(additional, 0, types, 2, additional.Length)
        new EitherTypeValidationAttribute (types)

    override _.ValidateElement(element) =
        if element = null then () else
        let elementType = (psbase element).GetType()
        if not <| isValidType(elementType, 0) then
            new ValidationMetadataException(
                String.Format(
                    ErrorMessages.EitherTypeValidationNotMatchedInterpolated,
                    String.Join(
                        ", ", 
                        types
                        )
                    )
                )
            |> raise
