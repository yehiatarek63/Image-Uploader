using ImageUploader;
using System.Text.Json;


var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();


app.MapGet("/", async (context) =>
{
    var path = Path.Combine(Directory.GetCurrentDirectory(), "index.html");
    await context.Response.WriteAsync(File.ReadAllText(path));
});

app.MapPost("/", async (HttpContext context) =>
{
    IFormCollection form = await context.Request.ReadFormAsync();
    string? title = form["title"];
    if (string.IsNullOrEmpty(title))
    {
        return Results.BadRequest("Empty title string");
    }
    if (form.Files.Count == 0)
    {
        return Results.BadRequest("No file uploaded");
    }
    IFormFile imageFormFile = form.Files[0];
    string fileName = imageFormFile.FileName;
    var fileExtension = fileName.Split('.')[1];
    if (fileExtension.ToLower() != "png" && fileExtension != "gif" && fileExtension != "jpeg")
    {
        return Results.BadRequest("Invalid file extension");
    }
    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Images", fileName);
    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await imageFormFile.CopyToAsync(stream);
    }
    Image newImage = new()
    {
        Title = title,
        ImagePath = filePath
    };
    var options = new JsonSerializerOptions
    {
        WriteIndented = true,
        IncludeFields = true,
    };
    string jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "imageInfo.json");
    string allJson = await File.ReadAllTextAsync(jsonPath);
    List<Image>? allImages = new();
    if (!string.IsNullOrEmpty(allJson))
    {
        allImages = JsonSerializer.Deserialize<List<Image>>(allJson);
    }
    allImages.Add(newImage);
    string allImagesJson = JsonSerializer.Serialize(allImages, options);
    await File.WriteAllTextAsync(jsonPath, allImagesJson);
    return Results.RedirectToRoute("picture", new { id = newImage.Id });
});

app.MapGet("/pictures/{id}", async (string id, HttpContext context) =>
{
    string jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "imageInfo.json");
    string allJson = await File.ReadAllTextAsync(jsonPath);
    List<Image>? allImages = JsonSerializer.Deserialize<List<Image>>(allJson);
    if (allImages is null)
    {
        return Results.NotFound("No images stored");
    }
    Image? image = allImages.Find(i => i.Id == id);
    if (image is null)
    {
        return Results.NotFound("Image not Found");
    }
    else
    {
        byte[] imageBytes = await File.ReadAllBytesAsync(image.ImagePath);
        string imageBase64Data = Convert.ToBase64String(imageBytes);
        var html = $@"<!DOCTYPE html>
                    <html>
                        <head>
                            <meta charset=""utf-8"" />
                            <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                            <title>Image Uploader</title>
                            <link href=""https://fastly.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css"" rel=""stylesheet"" integrity=""sha384-9ndCyUaIbzAi2FUVXJi0CjmCapSmO7SnpJef0486qhLnuZ2cdeRhO02iuK6FUUVM"" crossorigin=""anonymous"">
                        </head>
                        <body>
                            <div class=""container card shadow p-0 d-flex justify-content-center mt-5"" style=""width: 25rem;"">
                                <img src=""data:image/png;base64,{imageBase64Data}"" alt=""{image.Title}"" class=""card-img-top"" style=""width: 100%;"" >
                                <div class=""card-body"">
                                    <h4 class=""card-title"">Title:</h4>
                                    <h5 class=""card-title"">{image.Title}</h5>
                                </div>
                                <div class=""card-body"">
                                    <a href=""/"" class=""btn btn-primary"">Back to form</a>
                                </div>
                            </div>
                        </body>
                    </html>";
        return Results.Content(html, "text/html", System.Text.Encoding.UTF8);
    }
}).WithName("picture");

app.Run();

