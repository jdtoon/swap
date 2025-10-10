# NetMX Command-Line Interface (CLI)

This project contains the `netmx` command-line tool, a .NET Global Tool for managing and scaffolding NetMX applications and modules.

## Purpose

The NetMX CLI is the primary tool for developers to create new NetMX solutions and to automate common development tasks, ensuring that all generated code adheres to the framework's architecture and best practices.

## Local Development

To test changes to the CLI locally:

1.  Navigate to this directory.
2.  Pack the tool: `dotnet pack`
3.  Update the globally installed tool: `dotnet tool update --global --add-source ./nupkg NetMX.CLI`