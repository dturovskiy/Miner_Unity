using System.Collections.Generic;

public interface IDiagSnapshotProvider
{
    void AppendSnapshot(List<DiagField> fields);
}
