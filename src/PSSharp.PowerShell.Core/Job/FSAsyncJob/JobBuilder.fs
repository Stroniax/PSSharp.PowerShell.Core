namespace PSSharp
//    module Job =
//        type IntermediateBuilderJob (step) =
//            member _.Step = step
//        type JobBuilderJob (builder : IntermediateBuilderJob) =
//            inherit Job2Base ()
//            override _.StopJob(force, reason) = ()
//            override _.StartJob() = ()

//        type JobBuilder () =
//            member _.Zero f =
//                new IntermediateBuilderJob(f)
//            member _.Run builderjob =
//                new JobBuilderJob(builderjob)
//            member _.Yield f =
//                new IntermediateBuilderJob(f)
//            member _.Combine l r =
//                new IntermediateBuilderJob(fun () -> l(); r())
//            member _.Delay f =
//                new IntermediateBuilderJob f
                
//        let psjob = new JobBuilder()

//        let firstJob = 
//            psjob {
//                printfn "This is a message I will print"
//                let x = 3
//                printfn "The next value is %i" x
//            }