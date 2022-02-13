namespace PSSharp.Commands
open PSSharp
open System
open System.Collections.Generic
open System.Management.Automation

type NewDynamicParameterDictionaryCommand () =
    inherit Cmdlet ()

    let parameters = new List<RuntimeDefinedParameter>()

    [<Parameter(Mandatory = true, Position = 0, ValueFromRemainingArguments = true)>]
    [<NoCompletion>]
    member val DynamicParameter = Array.empty with get, set

    override this.ProcessRecord () =
        parameters.AddRange this.DynamicParameter

    override this.EndProcessing () =
        let dictionary = new RuntimeDefinedParameterDictionary()
        for parameter in parameters do
            dictionary.Add(parameter.Name, parameter)
        this.WriteObject dictionary