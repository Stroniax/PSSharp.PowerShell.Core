namespace PSSharp.Commands
open System
open System.Collections.Generic
open System.Threading.Tasks
open System.Management.Automation
open PSSharp

/// Waits for a task to complete, and then writes the task back to the pipeline.
/// This command does not yield the result of the task.
[<Cmdlet(VerbsLifecycle.Wait, Nouns.Task, DefaultParameterSetName = "DefaultSet")>]
[<Alias("await")>]
type WaitTaskCommand () =
    inherit TaskCmdlet ()
    /// Tasks to process
    let tasks = new List<Task> ()

    /// The task to wait. To wait for an awaitable that is not a task, pass the object to the ConvertTo-Task
    /// cmdlet before piping it to this cmdlet.
    [<Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ParameterSetName = "DefaultSet")>]
    [<Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ParameterSetName = "OrderedSet")>]
    [<Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ParameterSetName = "AnySet")>]
    [<Alias("Task", "io")>]
    member val InputObject = Array.Empty<Task> () with get,set

    /// Process and return the tasks in the order they are provided.
    [<Parameter(ParameterSetName = "OrderedSet")>]
    member val Ordered = SwitchParameter(false) with get,set

    /// Wait only for the first task that completes.
    [<Parameter(ParameterSetName = "AnySet")>]
    member val Any = SwitchParameter(false) with get,set
        
    member private this.ProcessOrdered () =
        for task in tasks do
            task |> this.SafeWaitTask |> ignore
            this.WriteObject task
        ()

    member private this.ProcessAny () =
        Task.WhenAny tasks |> this.WaitTask |> this.WriteObject

    member private this.ProcessAll () =
        while tasks.Count > 0 do
            let completedTask = Task.WhenAny tasks |> this.WaitTask
            this.WriteObject completedTask
            tasks.Remove(completedTask) |> ignore


    /// If Ordered is not present, completed tasks will be cleared from the cache
    /// and written to the pipeline.
    member private this.ProcessCompletedIfAllUnordered () =
        if not this.Ordered.IsPresent && not this.Any.IsPresent then
            let completedTasks = tasks.FindAll(fun t -> t.IsCompleted)
            for completedTask in completedTasks do
                this.WriteObject completedTask
                tasks.Remove(completedTask) |> ignore

    /// Process pipeline object which is Task[]
    override this.ProcessRecord () =
        tasks.AddRange(this.InputObject)
        this.ProcessCompletedIfAllUnordered()

    /// Finish processing after all pipeline input has been received
    override this.EndProcessing () =
        match this.Any.IsPresent with
        | true -> this.ProcessAny ()
        | false ->
            match this.Ordered.IsPresent with
            | true -> this.ProcessOrdered ()
            | false -> this.ProcessAll()
