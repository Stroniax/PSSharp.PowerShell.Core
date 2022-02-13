namespace PSSharp
    open System.Management.Automation
    open System.Collections

    /// Implmeentation of the ArgumentTransformationAttribute class designed to process
    /// input that may be a collection (such as an array) when each element should be processed
    /// individually.
    [<AbstractClass>]
    type EnumeratedArgumentTransformationAttribute () =
        inherit FlatteningTransformationAttribute<obj>()

        /// Set to <see langword="true"/> to avoid enumerating the input when it is a collection during processing.
        member val NoEnumerateCollection = false with get, set
        /// Gets a value option of the collection if obj is considered enumerable by the PowerShell language.
        member private this.TryGetEnumerable obj =
            match this.NoEnumerateCollection with
            | true -> ValueNone
            | false -> PSLanguagePrimitives.tryGetEnumerable obj

        /// <summary>
        /// Determine if a specific collection should be enumerated. Default value is to always return <see langword="true"/>.
        /// </summary>
        abstract ShouldEnumerate : collection: IEnumerable -> bool
        default _.ShouldEnumerate _ = true

        /// Process an element that was passed for transformation.
        abstract TransformElement : engineIntrinsics : EngineIntrinsics * inputData : obj -> TransformationResult<obj>

        /// <summary>
        /// Transform an object. If the object is a collection and <see cref="NoEnumerateCollection"/>
        /// is not <see langword="false"/>, and <see cref="ShouldEnumerate"/> returns <see langword="true"/>,
        /// each element will be transformed. The transformation result will be an array or the single
        /// transformed value.
        /// </summary>
        override this.TransformArgument (engineIntrinsics: EngineIntrinsics, inputData: obj) =
            match this.TryGetEnumerable inputData with
            | ValueSome coll ->
                [|
                    for item in coll do
                        this.TransformElement(engineIntrinsics, item)
                        |> TransformationResult.box (fun () -> Single item)
                |]
                |> TransformationResult.concat
            | ValueNone ->
                this.TransformElement(engineIntrinsics, inputData)

