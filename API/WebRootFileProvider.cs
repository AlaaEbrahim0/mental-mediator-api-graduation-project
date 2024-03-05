using Application.Contracts;

public class WebRootFileProvider : IWebRootFileProvider
{
    private readonly IWebHostEnvironment _webHostEnvironment;

    public WebRootFileProvider(IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    public string ReadFromWebRoot(string relativePath)
    {
        string filePath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath);
        return File.ReadAllText(filePath);
    }
}