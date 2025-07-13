using System.Collections.Generic;

using validator.Models;

namespace validator.Services.Interfaces;

public interface IValidatorService
{
    List<LineResult> ProcessFileWithConfig(string filePath, FileConfig fileConfig, Summary summary);
}
