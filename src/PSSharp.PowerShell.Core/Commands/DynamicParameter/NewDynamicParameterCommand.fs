namespace PSSharp.Commands
open PSSharp
open System
open System.Management.Automation

module NewDynamicParameterCommand =
    type internal DelegateCompletionAttribute (getCompleter) =  
        inherit ArgumentCompleterAttribute ()
        interface IArgumentCompleterFactory with
            member _.Create () = getCompleter ()

    [<Literal>]
    let DefaultSet = "DefaultSet"

    [<Literal>]
    let ExperimentSet = "ExperimentalSet"
open NewDynamicParameterCommand

/// Create a new RuntimeDefinedParameter to use as a dynamic parameter for a script function.
/// While parameters exist to define the ParameterAttribute information for the created parameter,
/// this only applies to a single parameter set. To support different parameter sets, provide
/// additional ParameterAttribute instances to the -Attributes parameter.
[<Cmdlet(VerbsCommon.New, Nouns.DynamicParameter, DefaultParameterSetName = DefaultSet)>]
[<OutputType(typeof<RuntimeDefinedParameter>)>]
type NewDynamicParameterCommand () =
    inherit Cmdlet ()

    let mutable isExperimentActionSet = false
    let mutable experimentAction = ExperimentAction.None
    let mutable experimentName = ""

    [<Parameter(Mandatory = true, Position = 0)>]
    [<AstStringConstantCompletion>]
    [<Alias("ParameterName")>]
    member val Name = "" with get, set

    [<Parameter(Position = 1)>]
    [<ValidateNotNullOrEmpty>]
    [<TypeNameCompletion>]
    [<Alias("tn")>]
    member val TypeName = "System.Object" with get, set

    [<Parameter(ParameterSetName = ExperimentSet, Mandatory = true)>]
    member _.ExperimentAction
        with get () = experimentAction
        and set v =
            isExperimentActionSet <- true
            experimentAction <- v
    
    [<Parameter(ParameterSetName = ExperimentSet, Mandatory = true)>]
    [<ArgumentCompleter(typeof<Microsoft.PowerShell.Commands.ExperimentalFeatureNameCompleter>)>]
    [<ValidateNotNullOrEmpty>]
    member _.ExperimentName
        with get () = experimentName
        and set v =
            isExperimentActionSet  <- true
            experimentName <- v

    [<Parameter>]
    [<AstStringConstantCompletion>]
    [<ValidateNotNullOrEmpty>]
    member val HelpMessage = "" with get, set
    
    [<Parameter>]
    [<AstStringConstantCompletion>]
    [<ValidateNotNullOrEmpty>]
    member val HelpMessageBaseName = "" with get, set
    
    [<Parameter>]
    [<AstStringConstantCompletion>]
    [<ValidateNotNullOrEmpty>]
    member val HelpMessageResourceId = "" with get, set
    
    [<Parameter>]
    member val Mandatory = SwitchParameter(false) with get, set
    
    [<Parameter>]
    [<AstStringConstantCompletion>]
    [<ValidateNotNullOrEmpty>]
    member val ParameterSetName = ParameterAttribute.AllParameterSets with get, set
    
    [<Parameter>]
    [<NumericCompletion(Max = 2147483647.0)>]
    member val Position = Int32.MinValue with get, set
    
    [<Parameter>]
    member val ValueFromPipeline = SwitchParameter(false) with get, set
    
    [<Parameter>]
    member val ValueFromPipelineByPropertyName = SwitchParameter(false) with get, set
    
    [<Parameter>]
    member val ValueFromRemainingArguments = SwitchParameter(false) with get, set

    [<Parameter>]
    member val DontShow = SwitchParameter(false) with get, set

    [<Parameter>]
    [<EmptyScriptCompletion>]
    [<Alias("ValidationScript", "vs")>]
    member val ValidateScript : ScriptBlock = null with get, set

    /// ScriptBlock argument completer, type of IArgumentCompleter, or constructed ArgumentCompleter or IArgumentCompleter instance.
    [<Parameter>]
    [<EitherTypeValidation(typeof<Type>, typeof<ScriptBlock>, typeof<ArgumentCompleterAttribute>, typeof<IArgumentCompleter>)>]
    [<ArgumentCompleterTypesCompletion>]
    [<StringToTypeTransformation>]
    [<Alias("ArgumentCompletion", "ac")>]
    member val ArgumentCompleter = psnull with get, set

    [<Parameter>]
    [<NoCompletion>]
    member val Attributes = Array.Empty<Attribute>() with get, set

    override this.ProcessRecord () =
        let parameter = new RuntimeDefinedParameter()
        parameter.Name <- this.Name
        match PSLanguagePrimitives.tryCast parameter.ParameterType with
        | ValueSome (t : Type) when t <> null -> parameter.ParameterType <- t
        | _ ->
            let typeNameAttribute = new PSTypeNameAttribute(this.TypeName)
            parameter.Attributes.Add(typeNameAttribute)
        for attr in this.Attributes do
            parameter.Attributes.Add attr

        let parameterAttr =
            match isExperimentActionSet with
                | true -> new ParameterAttribute(experimentName, experimentAction)
                | false -> new ParameterAttribute()

        if not <| String.IsNullOrEmpty(this.HelpMessage) then
            parameterAttr.HelpMessage <- this.HelpMessage
        if not <| String.IsNullOrEmpty(this.HelpMessageBaseName) then
            parameterAttr.HelpMessageBaseName <- this.HelpMessageBaseName
        if not <| String.IsNullOrEmpty(this.HelpMessageResourceId) then
            parameterAttr.HelpMessageResourceId <- this.HelpMessageResourceId
        parameterAttr.Mandatory <- this.Mandatory
        parameterAttr.ParameterSetName <- this.ParameterSetName
        parameterAttr.Position <- this.Position
        parameterAttr.ValueFromPipeline <- this.ValueFromPipeline
        parameterAttr.ValueFromPipelineByPropertyName <- this.ValueFromPipelineByPropertyName
        parameterAttr.ValueFromRemainingArguments <- this.ValueFromRemainingArguments
        parameterAttr.DontShow <- this.DontShow

        if this.ArgumentCompleter <> psnull then
            match psbase this.ArgumentCompleter with
            | :? ScriptBlock as completionScript ->
                let completerAttr = new ArgumentCompleterAttribute(completionScript)
                parameter.Attributes.Add completerAttr
            | :? Type as completerType ->
                let completerAttr = new ArgumentCompleterAttribute(completerType)
                parameter.Attributes.Add completerAttr
            | :? ArgumentCompleterAttribute as completerAttr ->
                parameter.Attributes.Add completerAttr
            | :? IArgumentCompleter as completer ->
                let completerAttr = new DelegateCompletionAttribute (fun () -> completer)
                parameter.Attributes.Add completerAttr
            | _ -> 
                let ex = new NotImplementedException(ErrorMessages.DynamicParameterCompleterNotImplemented)
                let er = new ErrorRecord(
                    ex,
                    nameof ErrorMessages.DynamicParameterCompleterNotImplemented,
                    ErrorCategory.NotImplemented,
                    this.Attributes)
                er.ErrorDetails <- new ErrorDetails(ErrorMessages.DynamicParameterCompleterNotImplementedInterpolated)
                er.ErrorDetails.RecommendedAction <- ErrorMessages.NotImplementedHelpMessage
                this.WriteError er

        if this.ValidateScript <> null then
            let validateScriptAttr = new ValidateScriptAttribute(this.ValidateScript)
            parameter.Attributes.Add validateScriptAttr

        this.WriteObject parameter