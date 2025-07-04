// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using System.Text.Json;
using Duende.IdentityModel.Client;

var handler = new HttpClientHandler();
// WARNING: Only use this in development!
handler.ServerCertificateCustomValidationCallback = 
    (httpRequestMessage, cert, cetChain, policyErrors) => true;

var client = new HttpClient(handler);

var disco = await client.GetDiscoveryDocumentAsync("http://localhost:5001");
if (disco.IsError)
{
    Console.WriteLine(disco.Error);
    Console.WriteLine(disco.Exception?.Message);
}

// request token
var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
{
    Address = "http://localhost:5001/connect/token",
    ClientId = "client",
    ClientSecret = "secret",
    Scope = "api1"
});

if (tokenResponse.IsError)
{
    Console.WriteLine(tokenResponse.Error);
    Console.WriteLine(tokenResponse.ErrorDescription);
    return 1;
}

Console.WriteLine(tokenResponse.AccessToken);

// call api
var apiClient = new HttpClient();
apiClient.SetBearerToken(tokenResponse.AccessToken!); // AccessToken is always non-null when IsError is false

var response = await apiClient.GetAsync("http://localhost:6001/identity");
if (!response.IsSuccessStatusCode)
{
    Console.WriteLine(response.StatusCode);
    return 1;
}

var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
Console.WriteLine(JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true }));
return 0;