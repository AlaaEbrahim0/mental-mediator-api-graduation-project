namespace Application.Contracts;

public interface IWebRootFileProvider
{
    string ReadFromWebRoot(string relativePath);
}

