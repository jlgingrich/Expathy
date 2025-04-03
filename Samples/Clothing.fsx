#r "../src/bin/Debug/net9.0/Expathy.dll"

(*
    This sample demonstrates how to parse nested XML with lots of attributes
*)

open Expathy

type Size =
    {
        Description: string
        ColorImages: Map<string, string>
    }

module Size =
    let decoder: Decoder<Size> =
        Decode.object (fun get ->
            {
                Description = get.Required.Single "@description" Decode.string
                ColorImages =
                    get.Required.Many
                        "color_swatch"
                        (Decode.object (fun get ->
                            get.Required.Single "text()" Decode.string, get.Required.Single "@image" Decode.string))
                    |> Map.ofList
            })

type Item =
    {
        ItemNumber: string
        Gender: string
        Price: decimal
        Sizes: Size list
    }

module Item =
    let decoder: Decoder<Item> =
        Decode.object (fun get ->
            {
                Gender = get.Required.Single "@gender" Decode.string
                ItemNumber = get.Required.Single "item_number/text()" Decode.string
                Price = get.Required.Single "price/text()" Decode.decimal
                Sizes = get.Required.Many "size" Size.decoder
            })

type Product =
    {
        Description: string
        ProductImage: string
        Items: Item list
    }

module Product =
    let decoder: Decoder<Product> =
        Decode.object (fun get ->
            {
                Description = get.Required.Single "@description" Decode.string
                ProductImage = get.Required.Single "@product_image" Decode.string
                Items = get.Required.Many "catalog_item" Item.decoder
            })

// Script

Load.fromFile "Clothing.xml"
|> Decode.object (fun get -> get.Required.Many "//catalog/product" Product.decoder)
|> Decode.assertOk "Unknown file structure, exiting"
|> List.iter (printfn "%A")
