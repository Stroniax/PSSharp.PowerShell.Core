namespace PSSharp.Commands
open System
open System.Threading.Tasks
open System.Management.Automation
open PSSharp

/// Gets the result of a completed task.
[<Cmdlet(VerbsCommunications.Receive, Nouns.Task, DefaultParameterSetName = "DefaultSet")>]
type ReceiveTaskCommand () =
    inherit TaskCmdlet ()

    /// The task to retrieve the result of. If the task is not complete, it will be ignored unless the -Wait
    /// parameter is present. Any task-like instance may be passed to this cmdlet, but for consistencies sake
    /// when processing a value that is not a task, it generally better to convert the value to a task before
    /// passing it to this command.
    [<Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ParameterSetName = "DefaultSet")>]
    [<TaskLikeTransformation>]
    [<Alias("Task")>]
    member val InputObject = Array.Empty<Task> () with get,set

    /// Wait for the task to complete.
    [<Parameter>]
    member val Wait = SwitchParameter(false) with get,set

    override this.ProcessRecord () =
        for task in this.InputObject do
            match this.Wait.IsPresent || task.IsCompleted with
            | true ->
                match task |> this.SafeWaitTask with
                | Ok result -> 
                    match result with
                    | ValueSome value -> this.WriteObject value
                    | ValueNone -> this.WriteDebug "Received a task with no result."
                | Result.Error exn -> 
                    let er = new ErrorRecord (
                        exn,
                        "TaskFailed",
                        ErrorCategory.NotSpecified,
                        task
                    )
                    this.WriteError er
            | false -> this.WriteDebug "Received an incomplete task."
