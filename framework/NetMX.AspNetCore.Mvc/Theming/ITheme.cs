namespace NetMX.AspNetCore.Mvc.Theming;

public interface ITheme
{
    string[] GetStyles();
    string[] GetScripts();
}