open System
open System.Xml

(*
## References:
- <https://en.wikipedia.org/wiki/XPath>
- <https://xpather.com/>
*)

type Decoder<'t> = XmlNode -> 't option

type SelectionError =
    | NodeNotFound
    | DecoderError
    | XPathError of string

module Decode =
    let fromXml xml : XmlNode =
        let doc = new XmlDocument()
        doc.LoadXml xml
        doc

    module XPath =
        let inline union paths = String.concat "|" paths

    // Try to get a node by xpath expression and convert it to a value
    let singleNode decoder xpath (node: XmlNode) =
        try
            match node.SelectSingleNode xpath with
            | null -> Error NodeNotFound
            | node' ->
                match decoder node' with
                | Some v -> Ok v
                | None -> Error DecoderError
        with :? XPath.XPathException as e ->
            XPathError e.Message |> Error

    let singleNodeRaw xpath node = singleNode Some xpath node

    // Try to get a set of nodes by xpath expression and get all that successfully convert to a value
    let manyNodes decoder xpath (node: XmlNode) =
        try
            node.SelectNodes xpath
            |> Seq.cast<XmlNode>
            |> Seq.choose decoder
            |> Seq.toList
            |> Ok
        with :? XPath.XPathException as e ->
            XPathError e.Message |> Error

    let converter f : Decoder<'t> =
        fun node ->
            try
                f node.Value |> Some
            with :? FormatException ->
                None

    let string: Decoder<string> = converter string
    let int: Decoder<int> = converter int
    let float: Decoder<float> = converter float
    let decimal: Decoder<decimal> = converter decimal
    let dateTime: Decoder<DateTime> = converter DateTime.Parse
