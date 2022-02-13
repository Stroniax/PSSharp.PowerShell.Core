namespace PSSharp
open PSSharp.ExtendedTypeDataAnalysis
open System.Management.Automation
open System.Management.Automation.Runspaces
open System.Diagnostics.CodeAnalysis
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Reflection
open System
open System.Collections.Generic

[<AbstractClass; Sealed; Extension>]
type ExtendedTypeDataAnalysisExtensions () =

    static let getOrAddTypeData(typeData: IDictionary<string, TypeData>, typeName: string) =
        if typeData = null then new TypeData(typeName) else
        match typeData.TryGetValue typeName with
        | true, foundTypeData -> foundTypeData
        | false, _ ->
            let newTypeData = new TypeData(typeName)
            typeData.Add(typeName, newTypeData)
            newTypeData
    static let addOrUpdateTypeDataMember (typeData: TypeData) (memberName: string) (updateValue: 'a -> 'a) (createValue: unit -> 'a) (force: bool) =
        match typeData.Members.TryGetValue(memberName) with
        | true, m ->
            if m :? 'a then
                typeData.Members[memberName] <- updateValue(m :?> 'a)
                typeData
            elif force then
                typeData.Members[memberName] <- createValue()
                typeData
            else 
                raise <| new ExtendedTypeSystemException(
                    String.Format(ErrorMessages.TypeDataMemberDefinedInterpolated, memberName, typeof<'a>.GetType().Name, typeData.TypeName)
                    )
        | false, _ -> createValue() |> typeData.Add
    static let failWithConflicting (typeData: TypeData) (typeMemberData: TypeMemberData) =
        raise <| new ExtendedTypeSystemException(String.Format(ErrorMessages.TypeDataDefinedInterpolated, typeMemberData.GetType().FullName, typeData.TypeName))
    static let updatePartialCodeMethod (typeData: TypeData) (definition: PSCodePropertyDefinition) (memberData: CodePropertyData) =
        if definition.GetCodeReference <> null && memberData.GetCodeReference <> null then
            failWithConflicting typeData memberData
        elif definition.SetCodeReference <> null && memberData.SetCodeReference <> null then
            failWithConflicting typeData memberData
        else
            if definition.GetCodeReference <> null then
                memberData.GetCodeReference <- definition.GetCodeReference.GetMethodInfo()
            if definition.SetCodeReference <> null then
                memberData.SetCodeReference <- definition.SetCodeReference.GetMethodInfo()
        memberData

    static let loadTypeData (force: bool) (definitions: PSExtendedTypeDataDefinition seq) =
        let exns = new List<exn>()
        let data = new Dictionary<string, TypeData>()
        for def in definitions do
            try def.GetTypeData(data, force) |> ignore
            with e ->
                exns.Add(
                    new ExtendedTypeSystemException(String.Format(ErrorMessages.TypeDataLoadExceptionInterpolated, def, e))
                    )
        if exns.Count > 0 then
            raise <| new AggregateException(ErrorMessages.TypeDataAggregateLoadException, exns)
        data

    static let mutable actualHandlerMethodCore = ValueNone
    static let createHandlerMethodCore (engine: EngineIntrinsics) =
        let sb = engine.InvokeCommand.NewScriptBlock("Update-TypeData -TypeData $args[0]")
        fun (typeData: TypeData array) -> engine.InvokeCommand.InvokeScript(engine.SessionState, sb, typeData) |> ignore
    static let getOrCreateHandlerMethodCore (engine) =
        match actualHandlerMethodCore with
        | ValueNone ->
            let newHandlerMethod = createHandlerMethodCore engine
            actualHandlerMethodCore <- ValueSome newHandlerMethod
            newHandlerMethod
        | ValueSome existingHandlerMethod -> existingHandlerMethod
    static let getHandlerMethod engine (sender: obj) (e: AssemblyLoadEventArgs) =
        let typeData : Dictionary<string, TypeData> = ExtendedTypeDataAnalysisExtensions.LoadTypeData(e.LoadedAssembly, false)
        let typeData : TypeData array = typeData |> Seq.map ( fun i -> i.Value ) |> Seq.toArray
        let fn = getOrCreateHandlerMethodCore engine
        fn typeData
    static let mutable actualAssemblyLoadEventHandler = ValueNone
    static let createAssemblyLoadEventHandler engine =
        new AssemblyLoadEventHandler(getHandlerMethod engine)
    static let getOrCreateAssemblyLoadEventHandler engine =
        match actualAssemblyLoadEventHandler with
        | ValueSome existingAssemblyLoadEventHandler -> existingAssemblyLoadEventHandler
        | ValueNone ->
            let newAssemblyLoadEventHandler = createAssemblyLoadEventHandler engine
            actualAssemblyLoadEventHandler <- ValueSome newAssemblyLoadEventHandler
            newAssemblyLoadEventHandler

    static member internal GetAssemblyLoadEventHandler(engineIntrinsics: EngineIntrinsics) = 
        getOrCreateAssemblyLoadEventHandler engineIntrinsics

    [<Extension>]
    static member internal Add(this: TypeData, typeDataMember: TypeMemberData) =
        this.Members.Add(typeDataMember.Name, typeDataMember)
        this
    
    [<Extension>]
    static member GetScript(this: PSScriptMethodDefinition) =
        ScriptBlock.Create(this.ScriptText)
    [<Extension>]
    static member TryGetMethodInfo(this: CodeReference) =
        let t = LanguagePrimitives.ConvertTo<Type>(this.TypeName)
        t.GetMethods(BindingFlags.Public ||| BindingFlags.Static)
        |> Array.filter (fun m -> m.Name.Equals(this.MethodName, StringComparison.OrdinalIgnoreCase))
        |> Array.map (fun m -> (m, m.GetParameters()))
        |> Array.filter (fun (m, p) -> p.Length > 0 && p[0].ParameterType = typeof<pso>)
        |> Array.map (fun (m,p) -> m)
        |> Array.tryExactlyOne
    [<Extension>]
    static member GetMethodInfo(this: CodeReference) =
        match this.TryGetMethodInfo() with
        | Some methodInfo -> methodInfo
        | None -> invalidOp ErrorMessages.CodeReferenceNotFound
    [<Extension>]
    static member GetTypeData(
            definition: PSAliasPropertyDefinition,
            [<Optional; DefaultParameterValue(null : IDictionary<string, TypeData>)>] typeData: IDictionary<string, TypeData>,
            [<Optional; DefaultParameterValue(false)>] force: bool
        ) =
        let typeData = getOrAddTypeData(typeData, definition.TypeName)
        addOrUpdateTypeDataMember
            typeData
            definition.MemberName
            (failWithConflicting typeData)
            (fun () -> new AliasPropertyData(definition.MemberName, definition.ReferencedMemberName))
            force
    [<Extension>]
    static member GetTypeData(
            definition: PSCodeMethodDefinition,
            [<Optional; DefaultParameterValue(null : IDictionary<string, TypeData>)>] typeData: IDictionary<string, TypeData>,
            [<Optional; DefaultParameterValue(false)>] force: bool
        ) =
        let typeData = getOrAddTypeData(typeData, definition.TypeName)
        addOrUpdateTypeDataMember
            typeData
            definition.MemberName
            (failWithConflicting typeData)
            (fun () ->
                let methodInfo = definition.CodeReference.GetMethodInfo()
                new CodeMethodData(definition.MemberName, methodInfo)
            )
            force
    [<Extension>]
    static member GetTypeData(
            definition: PSCodePropertyDefinition,
            [<Optional; DefaultParameterValue(null : IDictionary<string, TypeData>)>] typeData: IDictionary<string, TypeData>,
            [<Optional; DefaultParameterValue(false)>] force: bool
        ) =
        let typeData = getOrAddTypeData(typeData, definition.TypeName)
        addOrUpdateTypeDataMember
            typeData
            definition.MemberName
            (updatePartialCodeMethod typeData definition)
            (fun () ->
                new CodePropertyData(
                    definition.MemberName,
                    (if definition.GetCodeReference = null then null else definition.GetCodeReference.GetMethodInfo()),
                    (if definition.SetCodeReference = null then null else definition.SetCodeReference.GetMethodInfo())
                    )
            )
            force
    [<Extension>]
    static member GetTypeData(
            definition: PSNotePropertyDefinition,
            [<Optional; DefaultParameterValue(null : IDictionary<string, TypeData>)>] typeData: IDictionary<string, TypeData>,
            [<Optional; DefaultParameterValue(false)>] force: bool
        ) =
        let typeData = getOrAddTypeData(typeData, definition.TypeName)
        addOrUpdateTypeDataMember
            typeData
            definition.MemberName
            (failWithConflicting typeData)
            (fun () -> new NotePropertyData(definition.MemberName, definition.Value))
            force
    [<Extension>]
    static member GetTypeData(
            definition: PSScriptMethodDefinition,
            [<Optional; DefaultParameterValue(null : IDictionary<string, TypeData>)>] typeData: IDictionary<string, TypeData>,
            [<Optional; DefaultParameterValue(false)>] force: bool
        ) =
        let typeData = getOrAddTypeData(typeData, definition.TypeName)
        addOrUpdateTypeDataMember
            typeData
            definition.MemberName
            (failWithConflicting typeData)
            (fun () -> new ScriptMethodData(definition.MemberName, definition.GetScript()))
            force
    [<Extension>]
    static member GetTypeData(
            definition: PSScriptPropertyDefinition,
            [<Optional; DefaultParameterValue(null : IDictionary<string, TypeData>)>] typeData: IDictionary<string, TypeData>,
            [<Optional; DefaultParameterValue(false)>] force: bool
        ) =
        let typeData = getOrAddTypeData(typeData, definition.TypeName)
        addOrUpdateTypeDataMember
            typeData
            definition.MemberName
            (failWithConflicting typeData)
            (fun () -> new ScriptPropertyData(definition.MemberName, ScriptBlock.Create(definition.GetScriptText), ScriptBlock.Create(definition.SetScriptText)))
            force
    [<Extension>]
    static member GetTypeData(
            definition: PSTypeAdapterDefinition,
            [<Optional; DefaultParameterValue(null : IDictionary<string, TypeData>)>] typeData: IDictionary<string, TypeData>,
            [<Optional; DefaultParameterValue(false)>] force: bool
        ) =
        let typeData = getOrAddTypeData(typeData, definition.TypeName)
        if typeData.TypeAdapter <> null && not force then
            raise <| new ExtendedTypeSystemException(String.Format(ErrorMessages.TypeDataDefinedInterpolated, "type adapter", typeData.TypeName))
        else
            typeData.TypeAdapter <- LanguagePrimitives.ConvertTo<Type>(definition.TypeAdapterTypeName)
            typeData
    [<Extension>]
    static member GetTypeData(
            definition: PSTypeConverterDefinition,
            [<Optional; DefaultParameterValue(null : IDictionary<string, TypeData>)>] typeData: IDictionary<string, TypeData>,
            [<Optional; DefaultParameterValue(false)>] force: bool
        ) =
        let typeData = getOrAddTypeData(typeData, definition.TypeName)
        if typeData.TypeConverter <> null && not force then
            raise <| new ExtendedTypeSystemException(String.Format(ErrorMessages.TypeDataDefinedInterpolated, "type converter", typeData.TypeName))
        else
            typeData.TypeConverter <- LanguagePrimitives.ConvertTo<Type>(definition.TypeConverterTypeName)
            typeData

    [<Extension>]
    static member GetTypeData(
            definition: PSExtendedTypeDataDefinition,
            [<Optional; DefaultParameterValue(null : IDictionary<string, TypeData>)>] typeData: IDictionary<string, TypeData>,
            [<Optional; DefaultParameterValue(false)>] force: bool
        ) =
        match definition with
        | null -> null
        | :? PSAliasPropertyDefinition as d -> d.GetTypeData(typeData, force)
        | :? PSCodeMethodDefinition as d -> d.GetTypeData(typeData, force)
        | :? PSCodePropertyDefinition as d -> d.GetTypeData(typeData, force)
        | :? PSNotePropertyDefinition as d -> d.GetTypeData(typeData, force)
        | :? PSScriptMethodDefinition as d -> d.GetTypeData(typeData, force)
        | :? PSScriptPropertyDefinition as d -> d.GetTypeData(typeData, force)
        | :? PSTypeAdapterDefinition as d -> d.GetTypeData(typeData, force)
        | :? PSTypeConverterDefinition as d -> d.GetTypeData(typeData, force)
        | _ -> 
            raise <| new NotImplementedException(
                String.Format(
                    ErrorMessages.TypeDataDefinitionToTypeDataNotImplementedInterpolated,
                    definition.GetType().FullName
                    )
                )

    static member LoadTypeData(``member``: MemberInfo, [<Optional;DefaultParameterValue(false)>] force: bool) =
        PSExtendedTypeDataAnalysis.GetDefinitions(``member``)
        |> loadTypeData force

    static member LoadTypeData(``type``: Type, [<Optional;DefaultParameterValue(false)>] force: bool) =
        PSExtendedTypeDataAnalysis.GetDefinitions(``type``)
        |> loadTypeData force
    
    static member LoadTypeData(assembly: Assembly, [<Optional;DefaultParameterValue(false)>] force: bool) =
        PSExtendedTypeDataAnalysis.GetDefinitions(assembly)
        |> loadTypeData force
    
    static member LoadTypeData([<Optional;DefaultParameterValue(false)>] force: bool) =
        PSExtendedTypeDataAnalysis.GetDefinitionsInAppDomain()
        |> loadTypeData force
        