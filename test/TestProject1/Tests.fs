module Tests

open System
open System.Net
open System.Net.Http
open System.Text
open Microsoft.AspNetCore.Builder
open Newtonsoft.Json
open TestProject1
open Xunit
open Giraffe.Startup
open Giraffe.Middleware
open FsUnit.Xunit

let createClient configureApp =
    configureApp
    |> TestServer.createWebHostBuilder
    |> TestServer.create
    |> TestServer.createClient

let toHttpContent jsonString =
    new StringContent(jsonString, Encoding.UTF8, "application/json")
    
let toJsonContent (req:BookingRequest) =
    req
    |> JsonConvert.SerializeObject
    |> toHttpContent
    
let createBooking () =
    {
        name = "cook"
        partySize = 4
        bookingDate = DateTime(2022, 2, 17, 18, 30, 0, 0)
    }
    
let toHttpRequest booking =
    let req = new HttpRequestMessage(HttpMethod.Post, "/processMsg")
    req.Content <- booking |> toJsonContent
    req

let postTo (client:HttpClient) request =
    client.SendAsync(request).Result
    
let createBookingRequest () =
    createBooking ()
    |> toHttpRequest

[<Fact>]
let ``Outside in test - downstream service returns success`` () =
    
    let webServiceUnderTest = createClient TestServer.configureApp
    
    let result =
        createBookingRequest ()
        |> postTo webServiceUnderTest
    
    result.StatusCode |> should equal HttpStatusCode.Created
    
[<Fact>]
let ``Outside in test - downstream service returns failure`` () =
    
    let configureApp (app: IApplicationBuilder) =
         let bookingSystemHttpClient =
             TestServer.createStub (fun () -> new HttpResponseMessage(HttpStatusCode.NotFound))
             |> StubHttpClient.create "http://www.booking-tables.com"
         
         webApp bookingSystemHttpClient |> app.UseGiraffe
    
    let webServiceUnderTest = createClient configureApp
    
    let result =
        createBookingRequest ()
        |> postTo webServiceUnderTest
    
    result.StatusCode |> should equal HttpStatusCode.InternalServerError