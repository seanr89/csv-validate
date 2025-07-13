using validator.Models;

namespace validator.Services.Interfaces;

public interface ISpecificationSelector
{
    FileConfig? GetFileConfig(string fileType);
}
