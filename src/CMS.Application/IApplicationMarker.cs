namespace CMS.Application;

/// <summary>
/// Assembly marker used for reflection-based registration (MediatR handlers,
/// FluentValidation validators, Mapperly mappers) so scanning targets this
/// assembly without hard-coding its name.
/// </summary>
public interface IApplicationMarker;
