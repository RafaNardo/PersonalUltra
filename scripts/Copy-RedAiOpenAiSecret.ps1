[CmdletBinding()]
param(
    [string]$SourceUserSecretsId = 'red-ai-api-local',
    [string]$TargetUserSecretsId = 'personal-ultra-exercise-catalog-factory'
)

$ErrorActionPreference = 'Stop'
$secretRoot = Join-Path ([Environment]::GetFolderPath('ApplicationData')) 'Microsoft\UserSecrets'
$sourcePath = Join-Path (Join-Path $secretRoot $SourceUserSecretsId) 'secrets.json'
$targetDirectory = Join-Path $secretRoot $TargetUserSecretsId
$targetPath = Join-Path $targetDirectory 'secrets.json'

if (-not (Test-Path -LiteralPath $sourcePath)) {
    throw "Store de origem não encontrado para o UserSecretsId informado."
}

$source = Get-Content -LiteralPath $sourcePath -Raw | ConvertFrom-Json -AsHashtable
if (-not $source.ContainsKey('ai-api-key') -or [string]::IsNullOrWhiteSpace([string]$source['ai-api-key'])) {
    throw "A chave ai-api-key não existe no store de origem."
}

New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
$target = if (Test-Path -LiteralPath $targetPath) {
    Get-Content -LiteralPath $targetPath -Raw | ConvertFrom-Json -AsHashtable
} else {
    @{}
}

$target['ai-api-key'] = $source['ai-api-key']
$temporaryPath = Join-Path $targetDirectory ("secrets.{0}.tmp" -f [Guid]::NewGuid().ToString('N'))

try {
    $target | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $temporaryPath -Encoding utf8NoBOM
    Move-Item -LiteralPath $temporaryPath -Destination $targetPath -Force
    Write-Output 'ai-api-key copiada para a Factory sem exibir o valor.'
} finally {
    if (Test-Path -LiteralPath $temporaryPath) {
        Remove-Item -LiteralPath $temporaryPath -Force
    }
    $source = $null
    $target = $null
}
