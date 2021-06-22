module TestProject1.StubHttpClient

open System
open System.Net.Http

let create (baseAddress:string) handler =
    let bookingSystemHttpClient = new HttpClient(handler)
    bookingSystemHttpClient.BaseAddress <- baseAddress |> Uri
    bookingSystemHttpClient