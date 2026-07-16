namespace NomoAI.API.Features.Children.AssignChildToParent;

public sealed record AssignChildToParentResponse(
    int ChildId,
    int ParentId,
    string ParentFullName,
    string Message);