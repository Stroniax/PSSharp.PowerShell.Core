namespace PSSharp
open System
open System.Management.Automation
open System.Management.Automation.Language
open System.Collections.Generic
open System.Linq
open System.Runtime.InteropServices

/// Extensions for types defined in the PowerShell library.
[<AutoOpen>]
module PSExtensions =
    let private pseudoDisposable =
        {
            new IDisposable with
                member _.Dispose () = ()
        }

    let flattenOrMutate (a : IReadOnlyCollection<_>) f : obj =
        match a.Count with
        | 0 -> f a
        | 1 -> a.Single()
        | _ -> f a

    let ErrorDetails (message : string, [<ParamArray>] args : obj array ) =
        new ErrorDetails(String.Format(message, args))
    let internal ErrorRecord (
        createException : string -> exn,
        targetObject : obj,
        errorCategory : ErrorCategory,
        resourceName : string,
        [<ParamArray>] args : obj array) =
        let exceptionMessage = ErrorMessages.ResourceManager.GetString(resourceName)
        let e = createException exceptionMessage
        let interpolatedResourceName = resourceName + "Interpolated"
        let interpolatedResourceUnformatted = ErrorMessages.ResourceManager.GetString(interpolatedResourceName)
        let errorMessage =
            match ValueOption.ofObj interpolatedResourceUnformatted with
            | ValueSome m -> String.Format(m, args)
            | ValueNone -> exceptionMessage
        let error = new ErrorRecord(
            e,
            resourceName,
            errorCategory,
            targetObject
            )
        error.ErrorDetails <- new ErrorDetails(errorMessage)
        error

    //type ArgumentCompleterAttribute with
    //    member this.T () =
    //        ()

    type ArgumentTransformationAttribute with
        /// Returns the single element of collection, or the entire collection if the count is not 1.
        member _.MaybeFlatten collection : obj =
            flattenOrMutate collection box
        /// Returns the single element of collection, or an array if the count is not 1
        member _.FlattenOrArray collection : obj =
            flattenOrMutate collection (fun i ->
                match i with
                | :? ('a array) as arr -> arr
                | _ -> i.ToArray()
                )
    type Cmdlet with
        member this.WriteThrough value =
            this.WriteObject value
            value
        member this.WriteThrough enumerateCollection =
            fun value ->
                this.WriteObject(value, enumerateCollection)
                value
        member this.WriteDebug value =
            sprintf value |> this.WriteDebug

    type JobState with
        member this.IsFinished =
            this = JobState.Completed
            || this = JobState.Stopped
            || this = JobState.Failed

    type PathIntrinsics with
        member this.VisitPath(resolvedPath : string, [<Optional; DefaultParameterValue(null : string)>] stackName : string) =
            if this = null then nullArg (nameof this)
            if resolvedPath = null then nullArg (nameof resolvedPath)

            let stack =
                match stackName |> ValueOption.ofObj with
                | ValueSome s -> s
                | ValueNone -> Guid.NewGuid().ToString()

            match this.CurrentLocation.Path.Equals(resolvedPath, StringComparison.OrdinalIgnoreCase) with
            | true -> pseudoDisposable
            | false ->
                this.PushCurrentLocation(stack)
                try this.SetLocation(resolvedPath) |> ignore
                    {
                    new IDisposable with
                        member _.Dispose () =
                            this.PopLocation(stack)
                            |> ignore
                    }
                with e ->
                    this.PopLocation(stack)
                    |> ignore
                    reraise()
        member this.VisitPath(resolvedPath, action, [<Optional; DefaultParameterValue(null : string)>] stackName : string) =
            use disposal = this.VisitPath(resolvedPath, stackName)
            action()