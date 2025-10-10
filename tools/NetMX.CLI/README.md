# NetMX Command-Line Interface (CLI)

This project contains the `netmx` command-line tool, a .NET Global Tool for managing and scaffolding NetMX applications and modules.

## Purpose

The NetMX CLI is the primary tool for developers to create new NetMX solutions and to automate common development tasks, ensuring that all generated code adheres to the framework's architecture and best practices.

## Local Development

The `dotnet tool update` command only works when the version number of the package has changed. Since we are not changing the version number for every local build, you must **uninstall and then reinstall** the tool to see your changes.

This is the standard workflow for local tool development:

1.  Navigate to this directory: `cd tools/NetMX.CLI`
2.  Pack the tool to create the local NuGet package: `dotnet pack`
3.  Uninstall the old version: `dotnet tool uninstall --global NetMX.CLI`
4.  Install the new version from the local package: `dotnet tool install --global --add-source ./nupkg NetMX.CLI`