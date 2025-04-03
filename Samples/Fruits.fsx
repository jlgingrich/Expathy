#r "../src/bin/Debug/net9.0/Expathy.dll"

open Expathy
open System

(*
    This sample demonstrates how to parse XML while handling differences in heirarchy
*)

type CommonInfo =
    {
        Id: int
        Date: DateTime
        Route: string option
    }

let decodeInfo: Decoder<CommonInfo> =
    Decode.object (fun get ->
        {
            Id = get.Required.Single "supplyNumber/text()" Decode.int
            Date = get.Required.Single (XPath.union [ "supplyDate/text()"; "date/text()" ]) Decode.dateTime
            Route = get.Optional.Single "supplyNumber/@route" Decode.string
        })

type FruitInfo =
    {
        Name: string
        Price: float
        Currency: string
    }

let decodeFruit: Decoder<FruitInfo> =
    Decode.object (fun get ->
        {
            Name = get.Required.Single "name/text()" Decode.string
            Price = get.Required.Single "price/text()" Decode.float
            Currency = get.Required.Single "currency/text()" Decode.string
        })

type Supply =
    {
        Common: CommonInfo
        Fruit: FruitInfo list
    }

[ 1..3 ]
|> List.map (sprintf "Fruits%i.xml")
|> List.map Load.fromFile
|> List.map (
    Decode.object (fun get ->
        {
            Common = get.Required.Single "//doc/commonInfo" decodeInfo

            Fruit =
                get.Required.Many
                    (XPath.union
                        [
                            "//doc/lots/lot/objects/object"
                            "//doc/lots/lot/objects/obj"
                            "//doc/lots/lot/object"
                        ])
                    decodeFruit
        })
)
|> List.map (Decode.assertOk "Unknown file structure, exiting")
|> List.iter (printfn "%A")
