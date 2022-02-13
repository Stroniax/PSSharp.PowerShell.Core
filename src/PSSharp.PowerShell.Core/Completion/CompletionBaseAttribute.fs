namespace PSSharp
open System
open System.Collections
open System.Collections.Generic
open System.Management.Automation
open System.Management.Automation.Language

[<AbstractClass>]
type CompletionBaseAttribute () =
    inherit ArgumentCompleterAttribute()

    abstract CompleteArgument : commandName : string * parameterName : string * wordToComplete : string * commandAst : CommandAst * fakeBoundParameters : IDictionary -> CompletionResult seq

    interface IArgumentCompleter with
        member this.CompleteArgument(commandName, parameterName, wordToComplete, commandAst, fakeBoundParameters) =
            this.CompleteArgument(commandName, parameterName, wordToComplete, commandAst, fakeBoundParameters)
    interface IArgumentCompleterFactory with
        member this.Create () = this