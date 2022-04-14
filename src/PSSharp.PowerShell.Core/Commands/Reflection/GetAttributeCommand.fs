namespace PSSharp.Commands
open PSSharp
open System
open System.Management.Automation
open System.Reflection

module GetAttributeCommand =
    [<CompiledName("FilterFunction")>]
    let private filterFn t i =
        box i <> null && i.GetType().IsAssignableTo(t)
    [<CompiledName("FilterByAttributeType")>]
    let filterByAttributeType attributeType attributes =
        match attributeType with
        | Some attributeType ->
            attributes
            |> Seq.filter (filterFn attributeType)
            |> Seq.toArray
        | None ->
            attributes
            |> Seq.toArray

    [<CompiledName("GetAttributesFromScriptBlock")>]
    let getAttributesFromScriptBlock (scriptBlock: ScriptBlock) =
        scriptBlock.Attributes :> _ seq

    [<CompiledName("GetAttributesFromMember")>]
    let getAttributesFromMember (memberInfo: MemberInfo) =
        memberInfo.GetCustomAttributes()

    [<CompiledName("GetAttributesFromType")>]
    let getAttributesFromType (``type``: Type) =
        ``type``.GetCustomAttributes()

    [<CompiledName("GetAttributesFromParameter")>]
    let getAttributesFromParameter (parameter: ParameterMetadata) =
        parameter.Attributes

    [<CompiledName("GetAttributesFromCommand")>]
    let rec getAttributesFromCommand (command: CommandInfo) =
        match command with
        | :? FunctionInfo as functionInfo ->
            getAttributesFromScriptBlock functionInfo.ScriptBlock
        | :? CmdletInfo as cmdletInfo ->
            getAttributesFromType cmdletInfo.ImplementingType
        | :? AliasInfo as aliasInfo ->
            getAttributesFromCommand aliasInfo.ReferencedCommand
        | _ -> 
            command.CommandType
            |> string
            |> sprintf "Cannot get custom attributes for command type '%s'."
            |> invalidOp

    [<CompiledName("GetCommandOrParameterAttributes")>]
    let getCommandOrParameterAttributes
        (command)
        (parameterName)
        =
        match parameterName with
        | None -> getAttributesFromCommand command
        | Some parameterName ->
            let parameter = command.Parameters[parameterName]
            getAttributesFromParameter parameter
    [<CompiledName("GetTypeOrMemberAttributes")>]
    let getTypeOrMemberAttributes
        (``type``)
        (memberName)
        =
        match memberName with
        | None -> getAttributesFromType ``type``
        | Some memberName ->
            ``type``.GetMember(memberName, BindingFlags.Public ||| BindingFlags.Instance ||| BindingFlags.FlattenHierarchy)
            |> Seq.map getAttributesFromMember
            |> Seq.concat
            |> Seq.toArray
            :> _ seq

    [<CompiledName("GetVariableAttributes")>]
    let getVariableAttributes (variable: PSVariable) =
        variable.Attributes :> _ seq

    [<Literal>]
    let internal TypeParameterSet = "TypeSet"

    [<Literal>]
    let internal ScriptBlockParameterSet = "ScriptSet"

    [<Literal>]
    let internal CommandParameterSet = "CommandSet"

    [<Literal>]
    let internal VariableParameterSet = "VariableSet"

open GetAttributeCommand

[<Cmdlet(VerbsCommon.Get, Nouns.Attribute, DefaultParameterSetName = ScriptBlockParameterSet)>]
[<OutputType(typeof<Attribute>)>]
type GetAttributeCommand () =
    inherit PSCmdlet()

    /// Use backing field so we don't call Option.ofObj on every call to this.WriteAttributesFiltered
    let mutable attributeType = None

    [<Parameter(Mandatory = true, ParameterSetName = ScriptBlockParameterSet)>]
    [<EmptyScriptCompletion>]
    member val ScriptBlock: ScriptBlock = null with get, set

    [<Parameter(Mandatory = true, ParameterSetName = VariableParameterSet)>]
    [<VariableCompletion>]
    member val VariableName: string = null with get, set

    [<Parameter(Mandatory = true, ParameterSetName = CommandParameterSet)>]
    [<CommandCompletion>]
    member val CommandName: string = null with get, set

    [<Parameter(ParameterSetName = CommandParameterSet)>]
    member val ParameterName: string = null with get, set

    [<Parameter(Mandatory = true, ParameterSetName = TypeParameterSet)>]
    [<ObjectToTypeTransformation>]
    [<TypeNameCompletion>]
    member val Type: Type = null with get, set
    
    [<Parameter(ParameterSetName = TypeParameterSet)>]
    [<MemberNameCompletion(nameof Unchecked.defaultof<GetAttributeCommand>.Type)>]
    member val MemberName: string = null with get, set

    [<Parameter>]
    [<AttributeTypesCompletion>]
    member _.AttributeType
        with get () = attributeType |> Option.toObj
        and set v = attributeType <- Some v

    member private this.WriteAttributesFiltered attributes =
        attributes |> filterByAttributeType attributeType |> this.WriteObjectEnumerated

    override this.ProcessRecord () =
        this.ScriptBlock |> Option.ofObj |> Option.iter (getAttributesFromScriptBlock >> this.WriteAttributesFiltered)
        
        match this.CommandName with
        | null ->
            let command = this.SessionState.InvokeCommand.GetCommand(this.CommandName, CommandTypes.All)
            match command with
            | null -> ()
            | _ ->
                this.ParameterName
                |> Option.ofObj
                |> getCommandOrParameterAttributes command
                |> this.WriteAttributesFiltered
        | _ -> ()

        match this.Type with
        | null -> ()
        | _ ->
            this.MemberName
            |> Option.ofObj
            |> getTypeOrMemberAttributes this.Type
            |> this.WriteAttributesFiltered

        match this.VariableName with
        | null -> ()
        | _ ->
            let variable = this.SessionState.PSVariable.Get(this.VariableName)
            match variable with
            | null -> ()
            | _ ->
                variable
                |> getVariableAttributes
                |> this.WriteAttributesFiltered
