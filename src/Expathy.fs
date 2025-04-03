namespace Expathy

open System
open System.Xml
open System.Xml.XPath

type DecoderError =
    | FailedToSelectSingle
    | FailedToSelectMany
    | FailedToSelectValue
    | FormatError of input: string
    | XPathSyntaxError of msg: string

/// Used to capture errors in the object decoder
type internal DecoderException(xpath: string, error: DecoderError) =
    inherit Exception()

    member __.XPath = xpath
    member __.Error = error

type DecoderResult<'t> = Result<'t, string * DecoderError>

type Decoder<'t> = XmlNode -> DecoderResult<'t>

module XPath =
    let inline union paths = String.concat "|" paths

    let inline join (path1: string) (path2: string) =
        if path2 = "" then
            path1
        else
            match path1.EndsWith '/', path2.StartsWith '/' with
            | true, true -> path1 + path2[1..]
            | true, false -> path1 + path2
            | false, true -> path1[.. path1.Length - 2] + path2
            | false, false -> path1 + "/" + path2

module Load =
    open System.IO
    // Loaders
    let fromXml xml : XmlNode =
        let doc = new XmlDocument()
        doc.LoadXml xml
        doc

    let fromFile path : XmlNode =
        let doc = new XmlDocument()
        File.ReadAllText(path) |> doc.LoadXml
        doc

module Decode =
    // Object decoder
    let fail xpath err = raise <| DecoderException(xpath, err)

    type IRequiredGetters =
        /// Apply a decoder to the node selected by an XPath expression
        abstract Single: string -> Decoder<'a> -> 'a
        /// Apply a decoder to each node selected by an XPath expression
        abstract Many: string -> Decoder<'a> -> 'a list

    type IOptionalGetters =
        /// Apply a decoder to the node selected by an XPath expression if found
        abstract Single: string -> Decoder<'a> -> 'a option
        /// Apply a decoder to each node selected by an XPath expression if found
        abstract Many: string -> Decoder<'a> -> 'a list option

    type IGetters =
        /// Requires the XPath expression to successfully select target nodes
        abstract Required: IRequiredGetters
        /// Allows the XPath expression to fail to select target nodes
        abstract Optional: IOptionalGetters

    type Getters(node: XmlNode) =
        let requiredGetters =
            { new IRequiredGetters with
                member __.Single (xpath: string) (decoder: Decoder<'a>) : 'a =
                    try
                        match node.SelectSingleNode xpath with
                        | null -> fail xpath FailedToSelectSingle
                        | node' ->
                            match decoder node' with
                            | Ok v -> v
                            | Error(decoderPath, e) -> fail (XPath.join xpath decoderPath) e
                    with :? XPathException as e ->
                        fail xpath (XPathSyntaxError e.Message)

                member __.Many (xpath: string) (decoder: Decoder<'b>) : 'b list =
                    try
                        match node.SelectNodes xpath with
                        | null -> fail xpath FailedToSelectMany
                        | nodes ->
                            nodes
                            |> Seq.cast<XmlNode>
                            |> Seq.map decoder
                            |> Seq.choose (fun r ->
                                match r with
                                | Ok s -> Some s
                                | Error(decoderPath, e) -> fail (XPath.join xpath decoderPath) e)
                            |> Seq.toList
                    with :? XPathException as e ->
                        fail xpath (XPathSyntaxError e.Message)
            }

        let optionalGetters =
            { new IOptionalGetters with
                member __.Single (xpath: string) (decoder: Decoder<_>) =
                    try
                        match node.SelectSingleNode xpath with
                        | null -> None
                        | node' ->
                            match decoder node' with
                            | Ok r -> Some r
                            | Error _ -> None
                    with :? XPathException as e ->
                        fail xpath (XPathSyntaxError e.Message)

                member __.Many (xpath: string) (decoder: Decoder<_>) =
                    try
                        match node.SelectNodes xpath with
                        | null -> None
                        | nodes ->
                            nodes
                            |> Seq.cast<XmlNode>
                            |> Seq.map decoder
                            |> Seq.choose (fun r ->
                                match r with
                                | Ok s -> Some s
                                | Error _ -> None)
                            |> Seq.toList
                            |> Some
                    with :? XPathException as e ->
                        fail xpath (XPathSyntaxError e.Message)
            }

        interface IGetters with
            member __.Required = requiredGetters
            member __.Optional = optionalGetters

    /// Decode an arbitrary object by manually mapping XML elements
    let object (builder: IGetters -> 'value) : Decoder<'value> =
        fun node ->
            let getters = Getters node

            try
                builder getters |> Ok
            with :? DecoderException as exn ->
                Error <| (exn.XPath, exn.Error)

    // Error handling

    let printError e =
        match e with
        | xpath, FailedToSelectSingle -> $"XPath expression '%s{xpath}' failed to select node"
        | xpath, FailedToSelectMany -> $"XPath expression '%s{xpath}' failed to select any nodes"
        | xpath, FailedToSelectValue -> $"XPath expression '%s{xpath}' selected a node, but it did not have a value"
        | xpath, FormatError s -> $"The input string '%s{s}' at '%s{xpath}' was not in a correct format"
        | _, XPathSyntaxError errMsg -> $"XPath expression " + errMsg

    /// Assert that the decoder should fail if it encounters an unrecognized structure
    let assertOk (decodingResult: DecoderResult<'t>) =
        match decodingResult with
        | Ok v -> v
        | Error e -> printError e |> failwithf "Decoder encountered error: %s"

    // Primative decoders

    let node: Decoder<XmlNode> = Ok

    let outerXml: Decoder<string> = fun node -> Ok node.OuterXml

    let innerXml: Decoder<string> = fun node -> Ok node.InnerXml

    /// Creates a decoder from standard type conversion functions
    let converter f : Decoder<'t> =
        fun node ->
            try
                match node.Value with
                | null -> Error("", FailedToSelectValue)
                | v -> f v |> Ok
            with :? FormatException ->
                Error("", FormatError node.Value)

    let string: Decoder<string> = converter string
    let int: Decoder<int> = converter int
    let float: Decoder<float> = converter float
    let decimal: Decoder<decimal> = converter decimal
    let dateTime: Decoder<DateTime> = converter DateTime.Parse
