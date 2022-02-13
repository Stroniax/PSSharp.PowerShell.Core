namespace PSSharp

module internal PSCodeMethod =
    open System
    open System.Management.Automation
    
    let cast (obj: pso) =
        match obj |> psbase with
        | :? 'a as a -> a
        | _ ->
            let message = String.Format(ErrorMessages.PSCodeMethodInvalidSourceInterpolated, typeof<'a>)
            let exn = new ArgumentException(message, "this")
            raise exn

