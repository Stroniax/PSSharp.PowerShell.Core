namespace PSSharp.Commands
open System.Management.Automation
open PSSharp
open TaskLike

[<Cmdlet(VerbsData.ConvertTo, Nouns.Task)>]
type ConvertToTaskCommand() =
    inherit Cmdlet()

    [<Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)>]
    [<Alias("Task")>]
    member val InputObject: obj array = null with get, set

    override this.ProcessRecord() =
        for item in this.InputObject do
            match item |> tryAsTask with
            | ValueSome task -> this.WriteObject task
            | ValueNone -> 
                let ex = new PSInvalidCastException(ErrorMessages.NotAwaitable)
                let er = new ErrorRecord(
                    ex,
                    nameof ErrorMessages.NotAwaitable,
                    ErrorCategory.InvalidArgument,
                    item
                    )
                er |> this.WriteError