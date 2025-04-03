#r "../src/bin/Debug/net9.0/Expathy.dll"

open Expathy
open System

(*
    This sample demonstrates error reporting. Uncomment any of the block comments and comment out the correct version to see how Expathy reports the error
*)

type Book =
    {
        Id: string
        Author: string
        Title: string
        Genre: string
        PublishDate: DateTime
    }

Load.fromFile "Books.xml"
|> Decode.object (fun get ->
    get.Required.Many
        "//catalog/book"
        (Decode.object (fun get ->
            {
                Id = get.Required.Single "@id" Decode.string
                // FailedToSelectValue
                (* Author = get.Required.Single "author" Decode.string *)
                Author = get.Required.Single "author/text()" Decode.string
                // FailedToSelectSingle
                (* Title = get.Required.Single "errornode" Decode.string *)
                Title = get.Required.Single "title/text()" Decode.string
                // XPathSyntaxError
                (* Genre = get.Required.Single "genre/text)" Decode.string *)
                Genre = get.Required.Single "genre/text()" Decode.string
                // FormatError
                (* PublishDate =
                   get.Required.Single "publish_date/text()" Decode.float |> ignore
                   DateTime.Now *)
                PublishDate = get.Required.Single "publish_date/text()" Decode.dateTime
            })))
|> Decode.assertOk
|> List.iter (printfn "%A")
