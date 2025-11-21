<#
.SYNOPSIS
    Automatically generates SQL wrapper functions for all CLR methods with [SqlFunction], [SqlProcedure], or [SqlAggregate] attributes.

.DESCRIPTION
    Scans all C# files in src/Hartonomous.Database/CLR for decorated CLR methods and generates corresponding SQL wrapper files.
    Only creates wrappers that don't already exist.

.PARAMETER OutputDir
    Directory where SQL wrapper files will be created (default: src/Hartonomous.Database/Functions)

.PARAMETER Overwrite
    If specified, overwrites existing SQL wrapper files

.PARAMETER DryRun
    If specified, shows what would be generated without creating files

.EXAMPLE
    .\scripts\generate-clr-wrappers.ps1 -DryRun
    Shows all missing SQL wrappers without creating them

.EXAMPLE
    .\scripts\generate-clr-wrappers.ps1 -OutputDir "src\Hartonomous.Database\Functions"
    Generates all missing SQL wrapper files
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)]
    [string]$OutputDir = "src\Hartonomous.Database\Functions",
    
    [Parameter(Mandatory=$false)]
    [switch]$Overwrite,
    
    [Parameter(Mandatory=$false)]
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

# Resolve paths relative to script location
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$clrSourceDir = Join-Path $repoRoot "src\Hartonomous.Database\CLR"
$outputDirPath = Join-Path $repoRoot $OutputDir

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "CLR SQL Wrapper Generator" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "CLR Source Directory: $clrSourceDir"
Write-Host "Output Directory: $outputDirPath"
Write-Host "Dry Run: $DryRun"
Write-Host ""

# Verify CLR source directory exists
if (-not (Test-Path $clrSourceDir)) {
    Write-Error "CLR source directory not found: $clrSourceDir"
    exit 1
}

# Create output directory if it doesn't exist
if (-not $DryRun -and -not (Test-Path $outputDirPath)) {
    New-Item -ItemType Directory -Path $outputDirPath -Force | Out-Null
    Write-Host "Created output directory: $outputDirPath" -ForegroundColor Green
}

# Function to parse C# files and extract CLR methods
function Get-ClrMethods {
    param([string]$FilePath)
    
    $content = Get-Content $FilePath -Raw
    $methods = @()
    
    # Regex to match [SqlFunction], [SqlProcedure], [SqlAggregate] attributes with method signatures
    $pattern = '(?s)\[Sql(Function|Procedure|Aggregate).*?\]\s+public\s+static\s+(.*?)\s+(\w+)\s*\((.*?)\)'
    
    $matches = [regex]::Matches($content, $pattern)
    
    foreach ($match in $matches) {
        $attributeType = $match.Groups[1].Value  # Function, Procedure, or Aggregate
        $returnType = $match.Groups[2].Value.Trim()
        $methodName = $match.Groups[3].Value
        $parameters = $match.Groups[4].Value.Trim()
        
        # Parse parameters
        $paramList = @()
        if ($parameters -ne "") {
            $paramSplits = $parameters -split ',\s*'
            foreach ($param in $paramSplits) {
                if ($param -match '^\s*([\w\[\]<>?]+)\s+(\w+)') {
                    $paramType = $Matches[1]
                    $paramName = $Matches[2]
                    $paramList += @{ Type = $paramType; Name = $paramName }
                }
            }
        }
        
        # Determine namespace and class from file
        $namespace = "Hartonomous.Clr"
        if ($content -match 'namespace\s+([\w\.]+)') {
            $namespace = $Matches[1]
        }
        
        $className = [System.IO.Path]::GetFileNameWithoutExtension($FilePath)
        if ($content -match 'class\s+(\w+)') {
            $className = $Matches[1]
        }
        
        $methods += @{
            AttributeType = $attributeType
            ReturnType = $returnType
            MethodName = $methodName
            Parameters = $paramList
            Namespace = $namespace
            ClassName = $className
            SourceFile = $FilePath
        }
    }
    
    return $methods
}

# Function to map C# types to SQL types
function Get-SqlType {
    param([string]$CSharpType)
    
    switch -Regex ($CSharpType) {
        '^SqlInt32$' { return 'INT' }
        '^SqlInt64$' { return 'BIGINT' }
        '^SqlDouble$' { return 'FLOAT' }
        '^SqlDecimal$' { return 'DECIMAL(18,6)' }
        '^SqlString$' { return 'NVARCHAR(MAX)' }
        '^SqlBytes$' { return 'VARBINARY(MAX)' }
        '^SqlBoolean$' { return 'BIT' }
        '^SqlGuid$' { return 'UNIQUEIDENTIFIER' }
        '^SqlDateTime$' { return 'DATETIME2' }
        '^SqlGeometry$' { return 'GEOMETRY' }
        '^SqlGeography$' { return 'GEOGRAPHY' }
        '^IEnumerable' { return 'TABLE' }
        default { return 'NVARCHAR(MAX)' }  # Fallback
    }
}

# Function to determine SQL object type
function Get-SqlObjectType {
    param([hashtable]$Method)
    
    if ($Method.AttributeType -eq 'Procedure') {
        return 'PROCEDURE'
    }
    elseif ($Method.AttributeType -eq 'Aggregate') {
        return 'AGGREGATE'
    }
    elseif ($Method.ReturnType -match 'IEnumerable') {
        return 'FUNCTION (TABLE-VALUED)'
    }
    else {
        return 'FUNCTION (SCALAR)'
    }
}

# Function to generate SQL wrapper content
function New-SqlWrapperContent {
    param([hashtable]$Method)
    
    $objectType = Get-SqlObjectType -Method $Method
    $sqlName = "dbo.$($Method.MethodName)"
    
    # Build parameter list
    $paramDeclarations = @()
    foreach ($param in $Method.Parameters) {
        $sqlType = Get-SqlType -CSharpType $param.Type
        $paramDeclarations += "    @$($param.Name) $sqlType"
    }
    $paramList = $paramDeclarations -join ",`n"
    
    # Build external name
    $externalName = "[$($Method.Namespace)].[Hartonomous.Clr.$($Method.ClassName)].[$($Method.MethodName)]"
    
    # Generate appropriate SQL DDL based on object type
    if ($objectType -eq 'PROCEDURE') {
        # Stored Procedure
        $sql = @"
-- ==================================================
-- CLR Stored Procedure: $sqlName
-- Source: $($Method.SourceFile | Split-Path -Leaf)
-- Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
-- ==================================================

CREATE PROCEDURE $sqlName
$(if ($paramList) { $paramList } else { '    -- No parameters' })
AS EXTERNAL NAME $externalName
GO
"@
    }
    elseif ($objectType -eq 'AGGREGATE') {
        # Aggregate Function
        $sql = @"
-- ==================================================
-- CLR Aggregate Function: $sqlName
-- Source: $($Method.SourceFile | Split-Path -Leaf)
-- Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
-- ==================================================

CREATE AGGREGATE $sqlName
(
$(if ($paramList) { $paramList } else { '    -- No parameters' })
)
RETURNS $(Get-SqlType -CSharpType $Method.ReturnType)
EXTERNAL NAME $externalName
GO
"@
    }
    elseif ($objectType -eq 'FUNCTION (TABLE-VALUED)') {
        # Table-Valued Function
        $sql = @"
-- ==================================================
-- CLR Table-Valued Function: $sqlName
-- Source: $($Method.SourceFile | Split-Path -Leaf)
-- Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
-- ==================================================

CREATE FUNCTION $sqlName
(
$(if ($paramList) { $paramList } else { '    -- No parameters' })
)
RETURNS TABLE
AS EXTERNAL NAME $externalName
GO
"@
    }
    else {
        # Scalar Function
        $returnType = Get-SqlType -CSharpType $Method.ReturnType
        $sql = @"
-- ==================================================
-- CLR Scalar Function: $sqlName
-- Source: $($Method.SourceFile | Split-Path -Leaf)
-- Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
-- ==================================================

CREATE FUNCTION $sqlName
(
$(if ($paramList) { $paramList } else { '    -- No parameters' })
)
RETURNS $returnType
AS EXTERNAL NAME $externalName
GO
"@
    }
    
    return $sql
}

# Scan all C# files for CLR methods
Write-Host "Scanning CLR source files..." -ForegroundColor Yellow
$clrFiles = Get-ChildItem -Path $clrSourceDir -Filter "*.cs" -Recurse -File

$allMethods = @()
foreach ($file in $clrFiles) {
    $methods = Get-ClrMethods -FilePath $file.FullName
    $allMethods += $methods
}

Write-Host "Found $($allMethods.Count) CLR methods with SQL attributes" -ForegroundColor Green
Write-Host ""

# Check which wrappers already exist
$existingWrappers = @()
if (Test-Path $outputDirPath) {
    $existingWrappers = Get-ChildItem -Path $outputDirPath -Filter "dbo.*.sql" -File | 
        ForEach-Object { $_.BaseName }
}

Write-Host "Existing SQL wrappers: $($existingWrappers.Count)" -ForegroundColor Cyan
Write-Host ""

# Generate missing wrappers
$generatedCount = 0
$skippedCount = 0

foreach ($method in $allMethods) {
    $sqlFileName = "dbo.$($method.MethodName).sql"
    $sqlFilePath = Join-Path $outputDirPath $sqlFileName
    
    # Check if wrapper already exists
    if ((Test-Path $sqlFilePath) -and -not $Overwrite) {
        Write-Host "[SKIP] $sqlFileName (already exists)" -ForegroundColor Gray
        $skippedCount++
        continue
    }
    
    # Generate SQL wrapper content
    $sqlContent = New-SqlWrapperContent -Method $method
    
    if ($DryRun) {
        Write-Host "[DRY RUN] Would create: $sqlFileName" -ForegroundColor Yellow
        Write-Host $sqlContent -ForegroundColor DarkGray
        Write-Host ""
        $generatedCount++
    }
    else {
        # Write SQL file
        $sqlContent | Out-File -FilePath $sqlFilePath -Encoding UTF8 -Force
        Write-Host "[CREATED] $sqlFileName" -ForegroundColor Green
        $generatedCount++
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Total CLR methods found:   $($allMethods.Count)"
Write-Host "Existing wrappers:         $($existingWrappers.Count)"
Write-Host "Generated wrappers:        $generatedCount"
Write-Host "Skipped (already exist):   $skippedCount"
Write-Host ""

if ($DryRun) {
    Write-Host "This was a DRY RUN. No files were created." -ForegroundColor Yellow
    Write-Host "Run without -DryRun to generate SQL wrapper files." -ForegroundColor Yellow
}
else {
    Write-Host "SQL wrapper files generated successfully!" -ForegroundColor Green
    Write-Host "Output directory: $outputDirPath" -ForegroundColor Green
}

# List missing critical functions (from audit)
$criticalFunctions = @(
    'clr_VectorCosineSimilarity',
    'clr_VectorEuclideanDistance',
    'clr_VectorAverage',
    'clr_GenerateCodeAstVector',
    'fn_ProjectTo3D',
    'clr_ProjectToPoint',
    'clr_GenerateRandomVector',
    'clr_NormalizeVector',
    'clr_BlendVectors'
)

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Critical Functions Status:" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

foreach ($funcName in $criticalFunctions) {
    $wrapperPath = Join-Path $outputDirPath "dbo.$funcName.sql"
    $exists = Test-Path $wrapperPath
    $status = if ($exists) { "? EXISTS" } else { "? MISSING" }
    $color = if ($exists) { "Green" } else { "Red" }
    Write-Host "$status : $funcName" -ForegroundColor $color
}

Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Review generated SQL wrapper files in $outputDirPath"
Write-Host "2. Deploy wrappers to database: sqlcmd -S localhost -d Hartonomous -i <wrapper>.sql"
Write-Host "3. Verify stored procedures no longer call non-existent CLR functions"
Write-Host "4. Run DACPAC build to ensure no errors"

