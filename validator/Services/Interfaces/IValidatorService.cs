using System.Collections.Generic;

public interface IValidatorService
{
    List<LineResult> ProcessFileWithConfig(string filePath, FileConfig fileConfig, Summary summary);
}
