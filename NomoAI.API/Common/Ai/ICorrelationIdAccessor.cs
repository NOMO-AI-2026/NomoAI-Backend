namespace NomoAI.API.Common.Ai;

public interface ICorrelationIdAccessor
{
    string GetOrCreate();
}
