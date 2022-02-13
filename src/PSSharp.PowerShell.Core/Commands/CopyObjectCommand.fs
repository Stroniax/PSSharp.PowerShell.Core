namespace PSSharp.Commands
open System
open System.Collections
open System.Runtime.Serialization
open System.Runtime.Serialization.Formatters
open System.Runtime.Serialization.Formatters.Binary
open System.IO
open System.Management.Automation
open PSSharp

module CopyObjectCommand =
    let (|Serializable|NotSerializable|) obj =
        match obj = null with
        | true -> NotSerializable
        | false ->
            let t = obj.GetType()
            match t.IsSerializable with
            | true -> Serializable
            | false -> NotSerializable
    [<return : Struct>]
    let (|CSharpRecordType|_|) obj =
        ValueSome obj
    [<return : Struct>]
    let (|FSharpRecordType|_|) obj =
        ValueSome obj

    let private serializer = new BinaryFormatter()
    let copyCloneable logfn (cloneable : ICloneable) =
        sprintf "Processing cloneable input %A" cloneable
        |> logfn
        cloneable.Clone()
    let copySerializable logfn (serializable : 'a) : 'a =
        sprintf "Processing serializable input %A" serializable
        |> logfn
        use ms = new MemoryStream()
        serializer.Serialize(ms, serializable)
        ms.Position <- 0
        serializer.Deserialize(ms) :?> 'a
    let copyCSharpRecord logfn record =
        sprintf "Processing C# record %A" record
        |> logfn
        record
    let copyFSharpRecord logfn record =
        sprintf "Processing F# record %A" record
        |> logfn
        record
    let copyDictionary logfn (dictionary : IDictionary) =
        sprintf "Processing IDictionary %A" dictionary
        |> logfn
        dictionary
    let copy logfn (value: 'a) : 'a =
        match value |> psbase with
        | null -> value
        | :? ICloneable as cloneable -> copyCloneable logfn cloneable :?> 'a
        | Serializable -> copySerializable logfn value
        | :? ValueType -> sprintf "Processing ValueType %A" value |> logfn ; value
        | CSharpRecordType record -> copyCSharpRecord logfn record :?> 'a
        | FSharpRecordType record -> copyFSharpRecord logfn record :?> 'a
        | :? IDictionary as dictionary -> copyDictionary logfn dictionary :?> 'a
        | _ -> failwith "The provided value cannot be copied."


open CopyObjectCommand

[<Cmdlet(VerbsCommon.Copy, Nouns.Object)>]
type CopyObjectCommand () =
    inherit Cmdlet ()

    member val InputObject = psnull with get, set

    override this.ProcessRecord () =
        let b = psbase this.InputObject
        match b with
        | null ->
            this.WriteDebug "Processing null input"
            this.WriteObject b
        | :? ICloneable as clonable ->
            this.WriteDebug "Processing ICloneable input"
        | Serializable ->
            this.WriteDebug "Processing serializable input"
        | :? ValueType ->
            this.WriteDebug "Processing ValueType input"
        | CSharpRecordType record ->
            this.WriteDebug "Processing C# record type input"
        | FSharpRecordType record ->
            this.WriteDebug "Processing F# record type input"
        | :? IDictionary as dictionary ->
            this.WriteDebug "Processing IDictionary input"
