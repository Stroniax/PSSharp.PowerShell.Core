namespace PSSharp
open TaskLike

/// <summary>
/// Transforms any task-like instance into a task representation.
/// </summary>
type TaskLikeTransformationAttribute () =
    inherit EnumeratedArgumentTransformationAttribute ()

    /// Transforms the taskLike instance to a Task, if possible ; otherwise returns the object as-is.
    override _.TransformElement(_, taskLike) =
        match taskLike |> tryAsTask with
        | ValueSome task -> TransformationResult.Single task
        | ValueNone -> TransformationResult.Single taskLike
