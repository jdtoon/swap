using NetMX.DependencyInjection;

namespace NetMX.AspNetCore.Mvc.Theming;

public interface IThemeManager : IScopedDependency
{
    ITheme CurrentTheme { get; }
}