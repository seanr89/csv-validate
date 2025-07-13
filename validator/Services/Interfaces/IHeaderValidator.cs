

using validator.Models;

namespace validator.Services.Interfaces;

public interface IHeaderValidator
{

    List<RecordResult> Validate(int lineCount, string line, FileConfig fileConfig);
}