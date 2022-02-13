namespace PSSharp
open System
open System.Management.Automation
open System.Collections.Generic

type ValueSetWildcardExpansionAttribute ([<ParamArray>] values : string array) =
    inherit WildcardExpansionAttribute (false)

    override _.Expand(_, pattern) =
        let wc = WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase)
        let matches = new List<obj>(values.Length)
        for v in values do
            if wc.IsMatch v then
                matches.Add v
        match matches.Count with
        | 0 -> NotTransformed
        | _ -> Collection matches