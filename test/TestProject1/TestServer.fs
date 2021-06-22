module TestProject1.TestServer

open System.Net
open System.Net.Http
open System.Threading
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.TestHost
open Microsoft.Extensions.DependencyInjection
open Giraffe.Startup
open Giraffe.Middleware

type RequestInterceptorHandler(stubbedResponse:unit -> HttpResponseMessage) =
    inherit DelegatingHandler()
    
    let _getStubbedResponse = stubbedResponse
    
    override _.SendAsync (_:HttpRequestMessage, _:CancellationToken) =
        _getStubbedResponse () |> Task.FromResult

let createStub responseBuilder =
    new RequestInterceptorHandler(responseBuilder)

let configureServices (services: IServiceCollection) =
  services.AddGiraffe() |> ignore
    
let configureApp (app: IApplicationBuilder) =
     
     let bookingSystemHttpClient =
             createStub (fun () -> new HttpResponseMessage(HttpStatusCode.Created))
             |> StubHttpClient.create "http://www.booking-tables.com"
         
     webApp bookingSystemHttpClient |> app.UseGiraffe  
     

let create (webHostBuilder:IWebHostBuilder) =
    new TestServer(webHostBuilder)
        
let createClient (testServer:TestServer) =
    testServer.CreateClient()
    
let createWebHostBuilder (confApp:IApplicationBuilder -> unit) =
      WebHostBuilder()
          .ConfigureServices(configureServices)
          .Configure(confApp)