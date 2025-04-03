#r "../src/bin/Debug/net9.0/Expathy.dll"

open Expathy
open System

(*
    This sample demonstrates a simple mapping from table-like XML to domain types
*)

type Book =
    {
        Id: string
        Author: string
        Title: string
        Genre: string
        Price: float
        PublishDate: DateTime
        Description: string
    }

Load.fromFile "Books.xml"
|> Decode.object (fun get ->
    get.Required.Many
        "//catalog/book"
        (Decode.object (fun get ->
            {
                Id = get.Required.Single "@id" Decode.string
                Author = get.Required.Single "author/text()" Decode.string
                Title = get.Required.Single "title/text()" Decode.string
                Genre = get.Required.Single "genre/text()" Decode.string
                Price = get.Required.Single "price/text()" Decode.float
                PublishDate = get.Required.Single "publish_date/text()" Decode.dateTime
                Description = get.Required.Single "description/text()" Decode.string
            })))
|> Decode.assertOk "Unknown file structure, exiting"
|> List.iter (printfn "%A")
