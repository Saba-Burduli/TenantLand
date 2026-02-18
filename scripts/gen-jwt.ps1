param(
    [Parameter(Mandatory = $true)]
    [string]$TenantId,
    [Parameter(Mandatory = $true)]
    [string]$UserId,
    [string]$Role = "Owner",
    [string]$Scope = "tenant.api",
    [switch]$SkipScope
)

$ErrorActionPreference = "Stop"

$config = Get-Content "C:\Users\User\PostyLand\src\PostyLand.API\appsettings.Development.json" -Raw | ConvertFrom-Json

function ConvertTo-Base64Url([byte[]]$bytes) {
    [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

$header = @{
    alg = "HS256"
    typ = "JWT"
}

$now = [DateTimeOffset]::UtcNow
$payload = @{
    iss = $config.Jwt.Issuer
    aud = $config.Jwt.Audience
    nbf = $now.AddMinutes(-1).ToUnixTimeSeconds()
    exp = $now.AddHours(1).ToUnixTimeSeconds()
    iat = $now.ToUnixTimeSeconds()
    UserId = $UserId
    TenantId = $TenantId
    Role = $Role
}

if (-not $SkipScope) {
    $payload.Scope = $Scope
}

$headerJson = ($header | ConvertTo-Json -Compress)
$payloadJson = ($payload | ConvertTo-Json -Compress)

$headerEncoded = ConvertTo-Base64Url([System.Text.Encoding]::UTF8.GetBytes($headerJson))
$payloadEncoded = ConvertTo-Base64Url([System.Text.Encoding]::UTF8.GetBytes($payloadJson))
$unsignedToken = "$headerEncoded.$payloadEncoded"

$hmac = [System.Security.Cryptography.HMACSHA256]::new([System.Text.Encoding]::UTF8.GetBytes($config.Jwt.SigningKey))
$signatureBytes = $hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($unsignedToken))
$signatureEncoded = ConvertTo-Base64Url($signatureBytes)

"$unsignedToken.$signatureEncoded"
