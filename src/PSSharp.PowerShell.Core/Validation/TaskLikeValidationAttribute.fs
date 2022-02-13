namespace PSSharp
    open System
    open System.Collections
    open System.Management.Automation
    open TaskLike

    /// Validate that elements passed to the parameter are "Task-Like".
    type TaskLikeValidationAttribute () =
        inherit ValidateEnumeratedArgumentsAttribute ()

        override _.ValidateElement (element : obj) =
            match element |> psbase with
            | IsTaskLike -> ()
            | NotTaskLike -> new ValidationMetadataException (ErrorMessages.NotAwaitable) |> raise
