namespace PSSharp
open System
open System.Management.Automation
open System.Collections.Generic

type EnumWildcardExpansionAttribute (``type`` : Type) =
    inherit WildcardExpansionAttribute(false)

    let names = Enum.GetNames(``type``)

    override _.Expand(_, pattern) =
        let wc = WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase)
        let expandedValues = new List<obj>(names.Length)
        for n in names do
            if wc.IsMatch n then
                expandedValues.Add n
        match expandedValues.Count with
        | 0 -> NotTransformed
        | _ -> Collection expandedValues