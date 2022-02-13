namespace PSSharp.Commands
open System
open System.Collections.Generic
open System.Threading
open System.Management.Automation
open PSSharp

/// Converts an awaitable task-like object into a job familiar to PowerShell.
[<Cmdlet(VerbsData.ConvertTo, Nouns.AwaiterJob)>]
[<Alias("ctaj")>]
type ConvertToAwaiterJobCommand () =
    inherit PSCmdlet()
    
    /// Child jobs to aggregate into a single job when IsSingleJob is present.
    let childJobs = new List<AwaiterChildJob> ()

    /// The awaitable object, which must be a task-like instance.
    [<Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)>]
    [<TaskLikeValidation>]
    member val InputObject : obj = null with get, set

    /// The name of the created job.
    [<Parameter(Position = 1)>]
    member val Name : string = null with get, set

    /// Token that can be used to cancel the job.
    [<Parameter>]
    member val CancellationTokenSource : CancellationTokenSource = null with get, set

    /// Combine all tasks piped into this cmdlet into a single job.
    [<Parameter>]
    member val AsSingleJob = SwitchParameter(false) with get, set

    override this.ProcessRecord () =
        let cancellation =
            if this.CancellationTokenSource = null then null
            else new Action(fun () -> this.CancellationTokenSource.Cancel())
        let childJob = AwaiterChildJob.StartJob(
            this.InputObject |> psbase,
            cancellation = cancellation,
            jobName = this.Name,
            command = this.MyInvocation.Line);
        match this.AsSingleJob.IsPresent with
        | true -> childJobs.Add(childJob)
        | false ->
            let parentJob = AwaiterJob.StartJob(childJob, this.Name, this.MyInvocation.Line)
            this.JobRepository.Add parentJob
            this.WriteObject parentJob

    override this.EndProcessing () =
        match childJobs.Count with
        | 0 -> ()
        | _ ->
            let parentJob = AwaiterJob.StartJob(childJobs, this.Name, this.MyInvocation.Line)
            this.JobRepository.Add(parentJob)
            this.WriteObject(parentJob  )
