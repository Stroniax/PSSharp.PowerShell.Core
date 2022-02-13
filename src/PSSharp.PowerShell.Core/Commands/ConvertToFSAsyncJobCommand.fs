namespace PSSharp.Commands
open PSSharp
open System.Management.Automation

[<Cmdlet(VerbsData.ConvertTo, Nouns.FSAsyncJob)>]
type ConvertToFSAsyncJobCommand () =
    inherit PSCmdlet ()

    [<Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0)>]
    [<FSharpAsyncObjTransformation>]
    member val InputObject : Async<obj> = Unchecked.defaultof<_> with get, set

    [<Parameter(Position = 1)>]
    member val Name : string = null with get, set

    override this.ProcessRecord () =
        let pso = this.InputObject |> pso
        match pso.Properties["OriginalComputation"] with
        | null -> FSAsyncJob.Create(this.InputObject, this.MyInvocation.Line, this.Name) :> FSAsyncJob
        | psproperty ->
            match FSAsyncJob.TryCreateFromObj(psproperty.Value, this.MyInvocation.Line, this.Name) with
            | Some job -> job
            | None -> FSAsyncJob.Create(this.InputObject, this.MyInvocation.Line, this.Name)
        |> this.WriteThrough
        |> this.JobRepository.Add
