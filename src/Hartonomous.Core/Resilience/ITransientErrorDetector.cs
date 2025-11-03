using System;

namespace Hartonomous.Core.Resilience;

public interface ITransientErrorDetector
{
    bool IsTransient(Exception exception);
}
