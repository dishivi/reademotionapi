using System.Net.Http.Headers;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

string key = Environment.GetEnvironmentVariable("face-key") ?? throw new ArgumentNullException("face-key");
string baseUrl = Environment.GetEnvironmentVariable("face-baseurl") ?? throw new ArgumentNullException("face-baseurl");

var client = new HttpClient { BaseAddress = new Uri(baseUrl) };
client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);

var builder = WebApplication.CreateBuilder(args);

string policy = "_allowlocal";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: policy,
                      policy  =>
                      {
                          policy.WithOrigins("http://localhost:8089");
                          policy.AllowAnyHeader();
                          policy.AllowAnyMethod();
                      });
});

var app = builder.Build();

app.UseCors(policy);

app.MapPost("api/readface", async (HttpRequest request) => {
    string faceData = await new StreamReader(request.Body).ReadToEndAsync();
    using (ByteArrayContent content = new ByteArrayContent(Convert.FromBase64String(faceData.Replace("data:image/jpeg;base64,", ""))))
    {
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        HttpResponseMessage apiResponse = await client.PostAsync("face/v1.0/detect?returnFaceId=true&returnFaceLandmarks=false&returnFaceAttributes=age,emotion", content);
        string body = await apiResponse.Content.ReadAsStringAsync();

        return apiResponse.IsSuccessStatusCode
            ? Results.Ok(body)
            : Results.BadRequest($"Failed to get the emotion: {body} using uri {client.BaseAddress}");
    }
});

app.Run();