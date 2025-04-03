namespace Expathy

open System
open System.Xml
open System.Xml.XPath

type DecoderError =
    | EmptySelection
    | DecodingFailure
    | XPathError of string
//  | SubError of DecoderError list

type Decoder<'t> = XmlNode -> 't option

module XPath =
    let inline union paths = String.concat "|" paths

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

    type Getters<'T>(node: XmlNode) =
        let mutable errors = ResizeArray<DecoderError>()

        let requiredGetters =
            { new IRequiredGetters with
                member __.Single (xpath: string) (decoder: Decoder<'a>) : 'a =
                    try
                        match node.SelectSingleNode xpath with
                        | null ->
                            errors.Add <| EmptySelection
                            Unchecked.defaultof<'a>
                        | node' ->
                            match decoder node' with
                            | Some v -> v
                            | None ->
                                errors.Add <| DecodingFailure
                                Unchecked.defaultof<'a>
                    with :? XPathException as e ->
                        XPathError e.Message |> errors.Add
                        Unchecked.defaultof<'a>

                member __.Many (xpath: string) (decoder: Decoder<'b>) : 'b list =
                    try
                        match node.SelectNodes xpath with
                        | null ->
                            errors.Add <| EmptySelection
                            Unchecked.defaultof<'b list>
                        | nodes -> nodes |> Seq.cast<XmlNode> |> Seq.choose decoder |> Seq.toList
                    with :? XPathException as e ->
                        XPathError e.Message |> errors.Add
                        Unchecked.defaultof<'b list>
            }

        let optionalGetters =
            { new IOptionalGetters with
                member __.Single (xpath: string) (decoder: Decoder<_>) =
                    try
                        match node.SelectSingleNode xpath with
                        | null -> None
                        | node' -> decoder node'
                    with :? XPathException as e ->
                        XPathError e.Message |> errors.Add
                        None

                member __.Many (xpath: string) (decoder: Decoder<_>) =
                    try
                        match node.SelectNodes xpath with
                        | null -> None
                        | nodes -> nodes |> Seq.cast<XmlNode> |> Seq.choose decoder |> Seq.toList |> Some
                    with :? XPathException as e ->
                        XPathError e.Message |> errors.Add
                        None
            }

        member __.Errors: _ list = Seq.toList errors

        interface IGetters with
            member __.Required = requiredGetters
            member __.Optional = optionalGetters

    /// Decode an arbitrary object by manually mapping XML elements
    let object (builder: IGetters -> 'value) : Decoder<'value> =
        fun node ->
            let getters = Getters node
            let result = builder getters

            match getters.Errors with
            | [] -> Some result
            | _ -> None

    // Primative decoders
    /// Assert that the decoder handles all supported data and should fail if it encounters an unrecognized structure
    let assertOk errMsg decodingResult =
        match decodingResult with
        | Some v -> v
        | None -> failwithf "Decoder assertion failed: %s" errMsg

    /// Creates a decoder from standard type conversion functions
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
