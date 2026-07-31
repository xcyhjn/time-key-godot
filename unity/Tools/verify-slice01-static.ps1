param(
    [string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.4.10f1\Editor\Unity.exe'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = Split-Path -Parent $projectRoot
$editorData = Join-Path (Split-Path -Parent $UnityEditorPath) 'Data'
$tempDirectory = Join-Path $projectRoot 'Temp'
New-Item -ItemType Directory -Force -Path $tempDirectory | Out-Null

$dotnet = Join-Path $editorData 'NetCoreRuntime\dotnet.exe'
$csc = Join-Path $editorData 'DotNetSdkRoslyn\csc.dll'
$mono = Join-Path $editorData 'MonoBleedingEdge\bin\mono.exe'
$monoCsc = Join-Path $editorData 'MonoBleedingEdge\lib\mono\4.5\csc.exe'
$nunit = Join-Path $editorData 'Resources\PackageManager\BuiltInPackages\com.unity.ext.nunit\net40\unity-custom\nunit.framework.dll'
$mscorlib = Join-Path $editorData 'NetStandard\compat\2.1.0\shims\netfx\mscorlib.dll'
$templateAssemblies = Join-Path $editorData 'Resources\PackageManager\ProjectTemplates\libcache\com.unity.template.3d-cross-platform-17.0.14\ScriptAssemblies'
$uiAssembly = Join-Path $templateAssemblies 'UnityEngine.UI.dll'
$testRunnerAssembly = Join-Path $templateAssemblies 'UnityEngine.TestRunner.dll'
$editorCoreAssembly = Join-Path $editorData 'Managed\UnityEngine\UnityEditor.CoreModule.dll'

$netstandardReferences = Get-ChildItem (Join-Path $editorData 'NetStandard\ref\2.1.0') -Filter '*.dll' |
    ForEach-Object { '/reference:' + $_.FullName }
$unityReferences = Get-ChildItem (Join-Path $editorData 'Managed\UnityEngine') -Filter 'UnityEngine*.dll' |
    ForEach-Object { '/reference:' + $_.FullName }

function Invoke-RoslynCompile {
    param(
        [string]$Name,
        [string]$Output,
        [string[]]$Sources,
        [string[]]$References
    )

    & $dotnet $csc /nologo /langversion:latest /target:library /warnaserror+ /nostdlib+ `
        ('/out:' + $Output) $netstandardReferences $References $Sources
    if ($LASTEXITCODE -ne 0) {
        throw "$Name static compilation failed with exit code $LASTEXITCODE."
    }

    Write-Output "PASS static-compile $Name"
}

$domainAssembly = Join-Path $tempDirectory 'TimeKey.Domain.dll'
$infrastructureAssembly = Join-Path $tempDirectory 'TimeKey.Infrastructure.dll'
$presentationAssembly = Join-Path $tempDirectory 'TimeKey.Presentation.dll'
$domainTestsAssembly = Join-Path $tempDirectory 'TimeKey.Tests.EditMode.dll'
$infrastructureTestsAssembly = Join-Path $tempDirectory 'TimeKey.Tests.Infrastructure.dll'
$playModeTestsAssembly = Join-Path $tempDirectory 'TimeKey.Tests.PlayMode.dll'
$editorAssembly = Join-Path $tempDirectory 'TimeKey.Editor.dll'

Invoke-RoslynCompile -Name 'TimeKey.Domain' -Output $domainAssembly `
    -Sources (Get-ChildItem (Join-Path $projectRoot 'Assets\_Project\Runtime\Domain') -Filter '*.cs' | ForEach-Object FullName) `
    -References @()
Invoke-RoslynCompile -Name 'TimeKey.Infrastructure' -Output $infrastructureAssembly `
    -Sources (Get-ChildItem (Join-Path $projectRoot 'Assets\_Project\Runtime\Infrastructure') -Filter '*.cs' | ForEach-Object FullName) `
    -References (@(('/reference:' + $domainAssembly)) + $unityReferences)
Invoke-RoslynCompile -Name 'TimeKey.Presentation' -Output $presentationAssembly `
    -Sources (Get-ChildItem (Join-Path $projectRoot 'Assets\_Project\Runtime\Presentation') -Filter '*.cs' | ForEach-Object FullName) `
    -References (@(('/reference:' + $domainAssembly), ('/reference:' + $infrastructureAssembly), ('/reference:' + $uiAssembly)) + $unityReferences)
Invoke-RoslynCompile -Name 'TimeKey.Tests.EditMode' -Output $domainTestsAssembly `
    -Sources (Get-ChildItem (Join-Path $projectRoot 'Assets\_Project\Tests\EditMode') -Filter '*.cs' | ForEach-Object FullName) `
    -References @(('/reference:' + $domainAssembly), ('/reference:' + $nunit), ('/reference:' + $mscorlib))
Invoke-RoslynCompile -Name 'TimeKey.Tests.Infrastructure' -Output $infrastructureTestsAssembly `
    -Sources (Get-ChildItem (Join-Path $projectRoot 'Assets\_Project\Tests\Infrastructure') -Filter '*.cs' | ForEach-Object FullName) `
    -References (@(('/reference:' + $domainAssembly), ('/reference:' + $infrastructureAssembly), ('/reference:' + $nunit), ('/reference:' + $mscorlib)) + $unityReferences)
Invoke-RoslynCompile -Name 'TimeKey.Tests.PlayMode' -Output $playModeTestsAssembly `
    -Sources (Get-ChildItem (Join-Path $projectRoot 'Assets\_Project\Tests\PlayMode') -Filter '*.cs' | ForEach-Object FullName) `
    -References (@(('/reference:' + $domainAssembly), ('/reference:' + $infrastructureAssembly), ('/reference:' + $presentationAssembly), ('/reference:' + $uiAssembly), ('/reference:' + $testRunnerAssembly), ('/reference:' + $nunit), ('/reference:' + $mscorlib)) + $unityReferences)
Invoke-RoslynCompile -Name 'TimeKey.Editor' -Output $editorAssembly `
    -Sources (Get-ChildItem (Join-Path $projectRoot 'Assets\_Project\Editor') -Filter '*.cs' | ForEach-Object FullName) `
    -References (@(('/reference:' + $domainAssembly), ('/reference:' + $infrastructureAssembly), ('/reference:' + $presentationAssembly), ('/reference:' + $uiAssembly), ('/reference:' + $editorCoreAssembly)) + $unityReferences)

$asmdefs = Get-ChildItem (Join-Path $projectRoot 'Assets\_Project') -Recurse -Filter '*.asmdef'
foreach ($asmdef in $asmdefs) {
    Get-Content -Raw -LiteralPath $asmdef.FullName | ConvertFrom-Json | Out-Null
}
Write-Output "PASS asmdef-json count=$($asmdefs.Count)"

$sourceFixture = Join-Path $repositoryRoot 'card_data\lighting.json'
$unityFixture = Join-Path $projectRoot 'Assets\_Project\Content\Cards\lighting.json'
$sourceHash = (Get-FileHash -LiteralPath $sourceFixture -Algorithm SHA256).Hash
$unityHash = (Get-FileHash -LiteralPath $unityFixture -Algorithm SHA256).Hash
if ($sourceHash -ne $unityHash -or $sourceHash -ne '7DEC6590C409E3A5C9BB2E83B8E833FC1E6D9106EF5706E223F909D1E763FB0B') {
    throw 'The lighting fixture hash does not match the frozen contract.'
}
Write-Output "PASS lighting-sha256 $unityHash"

$reflectionRunner = Join-Path $tempDirectory 'ReflectionTestRunner.exe'
& $mono $monoCsc -nologo -langversion:latest -target:exe -warnaserror+ -reference:$nunit `
    -out:$reflectionRunner (Join-Path $PSScriptRoot 'ReflectionTestRunner.cs')
if ($LASTEXITCODE -ne 0) {
    throw 'The reflection test runner failed to compile.'
}

$env:MONO_PATH = "$tempDirectory;$(Split-Path -Parent $nunit)"
& $mono $reflectionRunner $domainTestsAssembly
if ($LASTEXITCODE -ne 0) {
    throw 'The license-independent Domain tests failed.'
}

Write-Output 'PASS slice01-static-verification'
