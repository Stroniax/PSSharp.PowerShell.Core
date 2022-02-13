namespace PSSharp
    open System
    open System.Management.Automation
    open System.Reflection
    open Microsoft.FSharp.Reflection

    /// Type of a member defined by member shape
    type MemberShapeType =
        | Function  of {| input: Type ; output : Type|}
        | Property  of {| get : bool ; set : bool ; ``type`` : Type |}
    /// A shape definition for a member of a type
    type MemberShape =
        {
            MemberType : MemberShapeType
            MemberName : string
        }
    /// Validates the shape of input objects to ensure they have certain expected methods or properties.
    [<AttributeUsage(AttributeTargets.Field ||| AttributeTargets.Property, Inherited = false, AllowMultiple = false)>]
    type ShapeValidationAttribute (shape : MemberShape list) =
        inherit ValidateEnumeratedArgumentsAttribute ()

        [<Literal>]
        static let bindingFlags = BindingFlags.Public ||| BindingFlags.Instance ||| BindingFlags.FlattenHierarchy

        /// Gets a shape definition from a MemberInfo object. Note that properties of type FSharpFunc<_,_> are
        /// classified as methods. This is to allow F# anonymous record types to define required methods.
        static let getMemberShape (memberInfo : MemberInfo) =
            match memberInfo with
            | :? FieldInfo as f ->
                ValueSome {
                    MemberName = f.Name
                    MemberType = Property
                        {|
                            ``type`` = f.FieldType
                            get = true
                            set = true
                        |}
                }
            | :? PropertyInfo as p ->
                let isFunction = p.PropertyType.MakeGenericType() = typedefof<FSharpFunc<obj,obj>>
                match isFunction with
                | true ->
                    let funcInOut = p.PropertyType.GetGenericArguments()
                    let inputType = funcInOut[0]
                    let outputType = funcInOut[1]
                    ValueSome {
                        MemberName = p.Name
                        MemberType = Function
                            {|
                                input = inputType
                                output = outputType
                            |}
                    }
                | false ->
                    ValueSome {
                        MemberName = p.Name
                        MemberType = Property
                            {|
                                get = p.CanRead
                                set = p.CanWrite
                                ``type`` = p.PropertyType
                            |}
                    }
            | :? MethodInfo as m ->
                let methodParametersTupled =
                    [| for p in m.GetParameters() -> p.ParameterType |]
                    |> FSharpType.MakeTupleType
                ValueSome {
                    MemberName = m.Name
                    MemberType = Function
                        {|
                            input = methodParametersTupled
                            output = m.ReturnType
                        |}
                }
            | :? EventInfo
            | _ -> ValueNone
        /// Checks if a compiled property is defined by `t` matching the required definition.
        static let validateCompiledProperty (t : Type , name : string , returnType : Type , readable : bool , writable : bool) =
            match t.GetProperty(name, bindingFlags) |> ValueOption.ofObj with
            | ValueSome property ->
                property.PropertyType.IsAssignableTo(returnType)
                && (not readable || property.CanRead)
                && (not writable || property.CanWrite)
            | ValueNone -> false
        /// Checks if a compiled field is defined by `t` matching the required definition.
        static let validateCompiledField (t : Type , name : string , returnType : Type ) =
            match t.GetField(name, bindingFlags) |> ValueOption.ofObj with
            | ValueSome field -> field.FieldType.IsAssignableTo(returnType)
            | ValueNone -> false
        /// Checks if `method` matches the required definition.
        static let validateCompiledMethod (name : string, input : Type , output : Type) (method : MethodInfo) =
            method.Name = name
            && method.ReturnType.IsAssignableTo(output)
            &&
                // un-tuple our input
                let inputParameterTypes =
                    match FSharpType.IsTuple input with
                    | true -> FSharpType.GetTupleElements input
                    | false -> [|input|]
                let parameters = method.GetParameters()
                let mutable failed = false
                let mutable i = 0
                while i < parameters.Length && not failed do
                    let parameter = parameters[i]
                    let expectedType = inputParameterTypes[i]
                    if parameter.ParameterType <> expectedType then
                        failed <- true
                    i <- i + 1
                failed
        /// Checks if `value` has a property matching the required definition.
        static let validatePSProperty (value : PSObject, name : string , returnType : Type , read : bool , write : bool) =
            try
                let p = value.Properties[name]
                p <> null
                && (not read || p.IsGettable)
                && (not write || p.IsSettable)
                && (p.TypeNameOfValue = returnType.FullName
                    ||
                        // read property once
                        let v = p.Value
                        (v = null && not returnType.IsValueType) || v.GetType().IsAssignableTo(returnType)
                    )
            with _ -> false
        /// Not implemented: always returns false.
        static let validatePSMethod (value : PSObject, name : string , input : Type , output : Type) =
            let m = value.Methods[name]
            false
        /// Ensures that the member `m` is present on the object `value`.
        static let validateCompiledMember (t : Type, m : MemberShape) =
            match m.MemberType with
            | Property p ->
                validateCompiledProperty(t, m.MemberName, p.``type``, p.get, p.set)
                || validateCompiledField(t, m.MemberName, p.``type``)
            | Function f ->
                t.GetMethods(bindingFlags)
                |> Seq.exists (validateCompiledMethod (m.MemberName, f.input, f.output))
        /// Ensures that the member `m` is present on the object `value`.
        static let validatePSMember (value : PSObject, m : MemberShape) =
            match m.MemberType with
            | Property p -> validatePSProperty(value, m.MemberName, p.``type``, p.get, p.set)
            | Function f -> validatePSMethod(value, m.MemberName, f.input, f.output)
        /// Gets the error message for a member shape definition.
        static let getErrorMessageForMemberShape shape =
            match shape.MemberType with
            | Function f ->
                String.Format(
                    ErrorMessages.RequiredShapeMethodInterpolated,
                    f.output.FullName,
                    f.input.FullName
                    )
            | Property p ->
                String.Format(
                    ErrorMessages.RequiredShapePropertyInterpolated,
                    shape.MemberName,
                    p.``type``.FullName
                    )
        /// Raises a ValidationMetadataException if shapeErrors is not empty.
        static let raiseShapeNotMatchedExceptionIfEmpty (shapeErrors : string list) =
            match shapeErrors |> List.isEmpty with
            | true -> ()
            | false ->
                let message =
                    String.Join(
                       "\n\t",
                        ErrorMessages.RequiredShapeError :: shapeErrors
                        )
                raise <| new ValidationMetadataException(message)
        /// Validates that the members defined by `shape` exist on `value`.
        static let validateCompiledMembers (value : obj, shape : MemberShape list) =
            let t = value.GetType()
            [
                for m in shape do
                    if not <| validateCompiledMember (t, m) then
                        getErrorMessageForMemberShape m
            ]
        /// Validates that the PS Members defined by `shape` exist on `value`.
        static let validatePSMembers (value : PSObject, shape : MemberShape list) =
            [
                for m in shape do
                    if not <| validatePSMember (value, m) then
                        getErrorMessageForMemberShape m
            ]

        /// Set to true if the properties must be compiled members of the type and cannot be PS members.
        /// Generally this is needed if the object is treated as a dynamic object within a compiled
        /// cmdlet to access the required members, and for PowerShell script functions may remain false.
        member val RequireCompiledMembers = false with get, set

        /// Constructs a new validation attribute requiring a shape matching the shape defined by the type
        /// provided. If the attribute is defined in F#, anonymous record types provide a simple method
        /// of defining a shape definition.
        new (``type`` : Type) =
            let memberShape = 
                [
                    let members = ``type``.GetMembers(BindingFlags.Public ||| BindingFlags.Instance ||| BindingFlags.FlattenHierarchy)
                    for m in members do
                        match getMemberShape m with
                        | ValueSome s -> s
                        | _ -> ()
                ]
            new ShapeValidationAttribute (memberShape)

        /// Ensures that all required members exist on the element.
        override this.ValidateElement(element) =
            match this.RequireCompiledMembers with
            | true ->
                let o =
                    match element with
                    | :? PSObject as pso -> pso.BaseObject
                    | _ -> element
                validateCompiledMembers(o, shape)
            | false -> 
                let pso = PSObject.AsPSObject(element)
                validatePSMembers(pso, shape)
            |> raiseShapeNotMatchedExceptionIfEmpty
