using System.Collections.Generic;

public interface IDiagContextProvider
{
    void AppendContext(List<DiagField> fields);
}
