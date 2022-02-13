namespace PSSharp.Commands
open System
open System.Management.Automation
open System.Management.Automation.Language
open PSSharp

module internal UseDisposableObjectCommand =
    [<Literal>]
    let DefaultSet = "DefaultSet"

    [<Literal>]
    let InitializationScriptSet = "InitializationScript"

    let dispose (pso : pso) =
        match pso |> psbase with
        | :? IDisposable as disposable -> disposable.Dispose()
        | :? IAsyncDisposable as asyncDisposable ->
            let vt = asyncDisposable.DisposeAsync()
            match vt.IsCompleted with
            | true -> ()
            | false -> vt.AsTask() |>Async.AwaitTask |> Async.RunSynchronously
        | _ -> ()

open UseDisposableObjectCommand

/// Wraps an expression in a using statement.
[<Cmdlet(VerbsOther.Use, Nouns.DisposableObject, DefaultParameterSetName = DefaultSet)>]
[<Alias("dispose")>]
type UseDisposableObjectCommand () =
    inherit PSCmdlet ()

    static let findVariableAssigment (ast : Ast) : bool =
        match ast with
        | :? AssignmentStatementAst as assignment ->
            assignment.Left :? VariableExpressionAst
        | _ -> false

    [<Parameter(Position = 0, Mandatory = true, ParameterSetName = DefaultSet)>]
    member val InputObject = psnull with get, set

    [<Parameter(Position = 0, Mandatory = true, ParameterSetName = InitializationScriptSet)>]
    member val InitializationScript : ScriptBlock = null with get, set

    [<Parameter(Position = 1, Mandatory = true)>]
    member val ScriptBlock : ScriptBlock = null with get, set

    override this.ProcessRecord () =
        match this.ParameterSetName with
        | DefaultSet -> 
            try
                let result = this.InvokeCommand.InvokeScript(false, this.ScriptBlock, null)
                this.WriteObject(result, true)
            finally
                dispose this.InputObject
        | InitializationScriptSet ->
            let assignmentAstOption = this.InitializationScript.Ast.Find(findVariableAssigment, false) |> ValueOption.ofObj
            match assignmentAstOption with
            | ValueSome assignmentAstBoxed  ->
                // get the variable so we can get rid of it later
                let assignmentAst = assignmentAstBoxed :?> AssignmentStatementAst
                let variableAst = assignmentAst.Left :?> VariableExpressionAst
                let variableName = variableAst.VariablePath.UserPath

                this.InvokeCommand.InvokeScript(false, this.InitializationScript, null) |> ignore
                try
                    let result = this.InvokeCommand.InvokeScript(false, this.ScriptBlock, null)
                    this.WriteObject(result, true)
                finally
                    let disposableVariable = this.SessionState.PSVariable.GetValue(variableName) |> pso
                    dispose disposableVariable
                    this.SessionState.PSVariable.Remove(variableName)
            | ValueNone ->
                let ex = new InvalidOperationException(ErrorMessages.ScriptBlockVariableAssignmentExpected)
                let er = new ErrorRecord(
                    ex,
                    nameof ErrorMessages.ScriptBlockVariableAssignmentExpected,
                    ErrorCategory.InvalidOperation,
                    this.InitializationScript
                    )
                this.WriteError er
            ()
        | parameterSet ->
            parameterSet
            |> ErrorMessages.parameterSetNotImplemented
            |> this.ThrowTerminatingError
