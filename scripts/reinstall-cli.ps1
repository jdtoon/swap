#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Reinstalls the NetMX CLI tool locally for development.

.DESCRIPTION
    This script uninstalls the current NetMX CLI, builds the latest version,
    and reinstalls it as a global .NET tool. Useful during development.

.EXAMPLE
    .\reinstall-cli.ps1
    Reinstalls the CLI with the latest local changes.

.EXAMPLE
    .\reinstall-cli.ps1 -SkipBuild
    Reinstalls without rebuilding (uses existing build).
#>

param(
    [switch]$SkipBuild,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

# Colors
function Write-Step { 
    Write-Host "▶ $args" -ForegroundColor Cyan 
}

function Write-Success { 
    Write-Host "✓ $args" -ForegroundColor Green 
}

function Write-Error { 
    Write-Host "✗ $args" -ForegroundColor Red 
}

function Write-Info { 
    Write-Host "ℹ $args" -ForegroundColor Yellow 
}

# Start
Write-Host ""
Write-Host " NetMX CLI Reinstaller" -ForegroundColor Cyan
Write-Host " =====================" -ForegroundColor Cyan
Write-Host ""

try {
    # Find solution root
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $rootDir = (Get-Item $scriptDir).Parent.FullName
    $cliProjectDir = Join-Path $rootDir "tools\NetMX.CLI"
    $cliProjectFile = Join-Path $cliProjectDir "NetMX.CLI.csproj"

    if (-not (Test-Path $cliProjectFile)) {
        Write-Error "CLI project not found at: $cliProjectFile"
        exit 1
    }

    Write-Info "CLI project: $cliProjectDir"
    Write-Host ""

    # Step 1: Uninstall existing tool
    Write-Step "Uninstalling existing NetMX CLI..."
    
    $uninstallOutput = dotnet tool uninstall -g NetMX.CLI 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Uninstalled existing NetMX CLI"
    } else {
        Write-Info "NetMX CLI not currently installed (this is okay)"
    }

    # Step 2: Build (optional)
    if (-not $SkipBuild) {
        Write-Step "Building CLI project..."
        
        Push-Location $cliProjectDir
        try {
            if ($Verbose) {
                dotnet build --configuration Release
            } else {
                dotnet build --configuration Release --nologo --verbosity quiet
            }
            
            if ($LASTEXITCODE -ne 0) {
                Write-Error "Build failed"
                exit 1
            }
            
            Write-Success "Build completed"
        } finally {
            Pop-Location
        }
    } else {
        Write-Info "Skipping build (using existing binaries)"
    }

    # Step 3: Pack
    Write-Step "Packing NuGet package..."
    
    Push-Location $cliProjectDir
    try {
        if ($Verbose) {
            dotnet pack --configuration Release --no-build
        } else {
            dotnet pack --configuration Release --no-build --nologo --verbosity quiet
        }
        
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Pack failed"
            exit 1
        }
        
        Write-Success "Package created"
    } finally {
        Pop-Location
    }

    # Step 4: Find the package
    $nupkgDir = Join-Path $cliProjectDir "nupkg"
    $nupkgFiles = Get-ChildItem -Path $nupkgDir -Filter "NetMX.CLI.*.nupkg" | 
                  Sort-Object LastWriteTime -Descending
    
    if ($nupkgFiles.Count -eq 0) {
        Write-Error "No NuGet package found in: $nupkgDir"
        exit 1
    }

    $nupkgFile = $nupkgFiles[0].FullName
    Write-Info "Package: $($nupkgFiles[0].Name)"

    # Step 5: Install from local package
    Write-Step "Installing NetMX CLI from local package..."
    
    dotnet tool install -g NetMX.CLI --add-source $nupkgDir --version "*-*" 2>&1 | Out-Null
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Installation failed"
        exit 1
    }
    
    Write-Success "NetMX CLI installed successfully"

    # Step 6: Verify installation
    Write-Step "Verifying installation..."
    Write-Host ""
    
    netmx --version
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Success "NetMX CLI is ready to use!"
        Write-Host ""
        Write-Info "Try: netmx --help"
    } else {
        Write-Error "Verification failed"
        exit 1
    }

} catch {
    Write-Error "An error occurred: $_"
    exit 1
}
