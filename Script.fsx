#load "XPaths.fsx"

open XPaths
open System.IO

File.ReadAllText "Samples/Example1.xml"
|> Decode.fromXml
|> Decode.singleNode Decode.string "//doc/commonInfo/potemkpins"
