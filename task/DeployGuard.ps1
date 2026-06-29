[CmdletBinding()]
param(
	[Parameter(Mandatory = $false)]
	[string]$KeyVaultName,

	[Parameter(Mandatory = $false)]
	[string]$AreaPath,

	[Parameter(Mandatory = $false)]
	[int]$CoverageThreshold = 80,

	[Parameter(Mandatory = $false)]
	[int]$SecretExpiryWarningDays = 14,

	[Parameter(Mandatory = $false)]
	[string]$Environment = "production",

	[Parameter(Mandatory = $false)]
	[string]$Organization,

	[Parameter(Mandatory = $false)]
	[string]$Project,

	[Parameter(Mandatory = $false)]
	[string]$TargetBranch = "main",

	[Parameter(Mandatory = $false)]
	[string]$ConfigPath
)

$ErrorActionPreference = "Stop"

Write-Host "##[section]DeployGuard - Pre-deployment Policy Checker"
Write-Host "##[section]============================================"

# Determine the script directory and solution root
$scriptDir = $PSScriptRoot
$solutionRoot = Split-Path -Parent $scriptDir

# Build arguments
$arguments = @()

if ($KeyVaultName) {
	$arguments += "--KeyVaultName=$KeyVaultName"
}

if ($AreaPath) {
	$arguments += "--AreaPath=$AreaPath"
}

$arguments += "--CoverageThreshold=$CoverageThreshold"
$arguments += "--SecretExpiryWarningDays=$SecretExpiryWarningDays"

if ($Organization) {
	$arguments += "--Organization=$Organization"
}
elseif ($env:SYSTEM_TEAMFOUNDATIONCOLLECTIONURI) {
	# Extract organization from ADO URL
	$uri = [System.Uri]$env:SYSTEM_TEAMFOUNDATIONCOLLECTIONURI
	$org = $uri.AbsolutePath.Trim('/')
	$arguments += "--Organization=$org"
}

if ($Project) {
	$arguments += "--Project=$Project"
}
elseif ($env:SYSTEM_TEAMPROJECT) {
	$arguments += "--Project=$env:SYSTEM_TEAMPROJECT"
}

if ($TargetBranch) {
	$arguments += "--TargetBranch=$TargetBranch"
}

if ($ConfigPath) {
	$arguments += "--config=$ConfigPath"
}

# Set environment variable for environment name
$env:DEPLOYGUARD_ENVIRONMENT = $Environment

# Find the DeployGuard.Runner project
$runnerProject = Join-Path $solutionRoot "src/DeployGuard.Runner/DeployGuard.Runner.csproj"

if (-not (Test-Path $runnerProject)) {
	Write-Host "##[error]DeployGuard.Runner project not found at: $runnerProject"
	exit 1
}

Write-Host "##[section]Running DeployGuard with arguments:"
Write-Host "  Environment: $Environment"
Write-Host "  KeyVaultName: $KeyVaultName"
Write-Host "  AreaPath: $AreaPath"
Write-Host "  CoverageThreshold: $CoverageThreshold%"
Write-Host "  SecretExpiryWarningDays: $SecretExpiryWarningDays"
Write-Host "  TargetBranch: $TargetBranch"
Write-Host ""

# Run the .NET application
try {
	$argString = $arguments -join " "
	Write-Host "##[command]dotnet run --project `"$runnerProject`" -- $argString"

	$process = Start-Process -FilePath "dotnet" `
		-ArgumentList "run", "--project", "`"$runnerProject`"", "--", $arguments `
		-NoNewWindow -PassThru -Wait

	exit $process.ExitCode
}
catch {
	Write-Host "##[error]Failed to run DeployGuard: $_"
	exit 1
}
