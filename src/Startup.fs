module Giraffe.Startup

open System
open System.Net
open System.Net.Http
open System.Text
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Newtonsoft.Json

[<CLIMutable>]
type BookingRequest = {
    partySize: int
    name: string
    bookingDate: DateTime
}

let toHttpContent jsonString =
    new StringContent(jsonString, Encoding.UTF8, "application/json")
    
let toJsonContent (req:BookingRequest) =
    req
    |> JsonConvert.SerializeObject
    |> toHttpContent

let reserveTable (downstreamClient:HttpClient) (request:BookingRequest) : HttpHandler =
    fun (_ : HttpFunc) (ctx : HttpContext) ->

        let content = request |> toJsonContent
        
        let result =
            async {
                return! (downstreamClient.PostAsync("/reserveTable", content)
                        |> Async.AwaitTask)
                } |> Async.RunSynchronously
        
        match result.StatusCode with
        | HttpStatusCode.Created ->
            setStatusCode 201 earlyReturn ctx
        | _ -> 
            setStatusCode 500 earlyReturn ctx
            
let bind<'t> =
    bindModel<BookingRequest> None
    
let webApp (bookingSystemHttpClient:HttpClient) =
    
    choose [
        GET >=>
            route "/ping" >=> text "pong"
        POST >=>
            route "/processMsg" >=> bind<BookingRequest> (reserveTable bookingSystemHttpClient)
           ]

type Startup() =
    member _.ConfigureServices (services : IServiceCollection) =
        services.AddGiraffe() |> ignore
        
    member _.Configure (app : IApplicationBuilder)
                        (_ : IHostEnvironment)
                        (_ : ILoggerFactory) =
       
        let bookingSystemHttpClient = new HttpClient()
        bookingSystemHttpClient.BaseAddress <- "www.booking-tables.com" |> Uri
        
        webApp bookingSystemHttpClient |> app.UseGiraffe
