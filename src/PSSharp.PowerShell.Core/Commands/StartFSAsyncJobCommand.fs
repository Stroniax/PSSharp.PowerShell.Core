namespace PSSharp.Commands
open PSSharp
open System
open System.Management.Automation

[<Cmdlet(VerbsLifecycle.Start, Nouns.FSAsyncJob)>]
type StartFSAsyncJobCommand () =
    inherit PSCmdlet ()

    [<Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ParameterSetName = "ExistingJob")>]
    member val Job : FSAsyncJob = Unchecked.defaultof<_> with get, set

    [<Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ParameterSetName = "Computation")>]
    [<FSharpAsyncObjTransformation>]
    member val Computation : Async<obj> = Unchecked.defaultof<_> with get, set


    member this.CreateJob (computation : Async<obj>) : FSAsyncJob =
        let job =
            let pso = computation |> pso
            match pso.Properties["OriginalComputation"] with
            | null -> FSAsyncJob.Create(computation, this.MyInvocation.Line) :> FSAsyncJob
            | comp -> 
                match FSAsyncJob.TryCreateFromObj (comp.Value, this.MyInvocation.Line) with
                | Some job -> job
                | None -> FSAsyncJob.Create(computation, this.MyInvocation.Line)
        this.JobRepository.Add job
        job
    member this.StartAndWriteJob (job : FSAsyncJob) =
        job.StartJob ()
        this.WriteObject job

    override this.ProcessRecord () =
        match this.ParameterSetName with
        | "ExistingJob" -> this.StartAndWriteJob this.Job
        | "Computation" -> this.CreateJob this.Computation |> this.StartAndWriteJob
        | pset ->
            ErrorMessages.parameterSetNotImplemented pset
            |> this.ThrowTerminatingError
