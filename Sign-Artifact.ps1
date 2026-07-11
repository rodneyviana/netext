<#
.SYNOPSIS
    Signs a single file with Azure Artifact Signing via the dotnet 'sign' tool.

.DESCRIPTION
    Reads signing configuration from a .metadata.json file (searched for upward
    from the current directory unless -MetadataPath is given), ensures a valid
    Azure CLI login for the code-signing scope, pins DefaultAzureCredential to
    developer credentials so it never hangs on the managed-identity IMDS probe,
    signs the file, and verifies the resulting signature.

    Expected .metadata.json shape (same field names as the signtool dlib metadata):
        {
          "Endpoint": "https://scus.codesigning.azure.net",
          "CodeSigningAccountName": "your-account",
          "CertificateProfileName": "your-profile"
        }

.PARAMETER File
    Path of the file to sign. The path is used exactly as entered (not resolved).

.PARAMETER MetadataPath
    Explicit path to the metadata JSON. If omitted, the script walks up from the
    current directory looking for '.metadata.json'.

.PARAMETER TimestampUrl
    RFC 3161 timestamp authority. Defaults to Microsoft's ACS timestamp service.

.PARAMETER ForceLogin
    Run 'az login' even if a cached token for the signing scope is already valid.

.PARAMETER Verbosity
    Verbosity passed to the sign tool (quiet, minimal, normal, information, trace).

.EXAMPLE
    .\Sign-Artifact.ps1 .\x64\Release\NetExt.dll

.EXAMPLE
    .\Sign-Artifact.ps1 -MetadataPath C:\src\.metadata.json .\bin\NetExt.dll
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory, Position = 0)]
    [string] $File,

    [string] $MetadataPath,

    [string] $TimestampUrl = 'http://timestamp.acs.microsoft.com',

    [switch] $ForceLogin,

    [ValidateSet('quiet', 'minimal', 'normal', 'information', 'trace')]
    [string] $Verbosity = 'information'
)

$ErrorActionPreference = 'Stop'
$SigningScope = 'https://codesigning.azure.net/.default'
$MetadataFileName = '.metadata.json'

function Resolve-MetadataFile {
    param([string] $Explicit)

    if ($Explicit) {
        if (-not (Test-Path -LiteralPath $Explicit)) {
            throw "Metadata file not found: $Explicit"
        }
        return (Resolve-Path -LiteralPath $Explicit).Path
    }

    # Walk up from the current directory until we find the metadata file.
    $dir = (Get-Location).Path
    while ($dir) {
        $candidate = Join-Path $dir $MetadataFileName
        if (Test-Path -LiteralPath $candidate) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
        $parent = Split-Path -Path $dir -Parent
        if ($parent -eq $dir) { break }   # reached the drive root
        $dir = $parent
    }

    throw "Could not find '$MetadataFileName' in the current directory or any parent. " +
          "Create one or pass -MetadataPath."
}

function Test-SigningToken {
    # Returns $true if the Azure CLI can mint a token for the signing scope.
    try {
        az account get-access-token --scope $SigningScope --output none 2>$null
        return ($LASTEXITCODE -eq 0)
    }
    catch {
        return $false
    }
}

# --- Preflight: required tools ------------------------------------------------

if (-not (Get-Command sign -ErrorAction SilentlyContinue)) {
    throw "The 'sign' tool is not on PATH. Install it with: " +
          "dotnet tool install --global --prerelease sign"
}
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "Azure CLI ('az') is not on PATH. Install it, then run this script again."
}

# --- Load and validate metadata ----------------------------------------------

$metaPath = Resolve-MetadataFile -Explicit $MetadataPath
Write-Host "Using metadata: $metaPath"

try {
    $meta = Get-Content -LiteralPath $metaPath -Raw | ConvertFrom-Json
}
catch {
    throw "Failed to parse '$metaPath' as JSON: $($_.Exception.Message)"
}

$endpoint = $meta.Endpoint
$account  = $meta.CodeSigningAccountName
$profile  = $meta.CertificateProfileName

$missing = @()
if ([string]::IsNullOrWhiteSpace($endpoint)) { $missing += 'Endpoint' }
if ([string]::IsNullOrWhiteSpace($account))  { $missing += 'CodeSigningAccountName' }
if ([string]::IsNullOrWhiteSpace($profile))  { $missing += 'CertificateProfileName' }
if ($missing.Count) {
    throw "Metadata is missing required field(s): $($missing -join ', ')"
}

# --- Authentication -----------------------------------------------------------

# Pin the credential chain to developer credentials only. This skips the
# managed-identity IMDS probe (169.254.169.254) that otherwise times out and
# fails the whole chain on machines with Docker/WSL/Hyper-V virtual networking.
$env:AZURE_TOKEN_CREDENTIALS = 'dev'

if ($ForceLogin -or -not (Test-SigningToken)) {
    Write-Host "Signing in to Azure (scope: $SigningScope) ..."
    az login --scope $SigningScope --output none
    if ($LASTEXITCODE -ne 0) { throw "az login failed." }

    if (-not (Test-SigningToken)) {
        throw "Still unable to acquire a signing token after login."
    }
}
else {
    Write-Host "Existing Azure CLI token for the signing scope is valid."
}

# --- Sign ---------------------------------------------------------------------

# Keep the path exactly as entered; do not resolve it. Split into the base
# directory and leaf name that the sign tool expects.
if (-not (Test-Path -LiteralPath $File)) {
    throw "File not found: $File"
}

$dir  = Split-Path -Path $File -Parent
$name = Split-Path -Path $File -Leaf
if ([string]::IsNullOrEmpty($dir)) { $dir = '.' }

Write-Host ""
Write-Host "Signing $name in $dir"

& sign code artifact-signing `
    --artifact-signing-endpoint $endpoint `
    --artifact-signing-account $account `
    --artifact-signing-certificate-profile $profile `
    --timestamp-url $TimestampUrl `
    --verbosity $Verbosity `
    $File

if ($LASTEXITCODE -ne 0) {
    throw "sign returned exit code $LASTEXITCODE for $File"
}

# --- Verify -------------------------------------------------------------------

Write-Host ""
Write-Host "Verifying signature:"
$sig = Get-AuthenticodeSignature -LiteralPath $File
$subject = if ($sig.SignerCertificate) { $sig.SignerCertificate.Subject } else { '(none)' }
"{0,-8} {1}  [{2}]" -f $sig.Status, $name, $subject | Write-Host

Write-Host ""
Write-Host "Done."



# SIG # Begin signature block
# MII2ngYJKoZIhvcNAQcCoII2jzCCNosCAQExDzANBglghkgBZQMEAgEFADB5Bgor
# BgEEAYI3AgEEoGswaTA0BgorBgEEAYI3AgEeMCYCAwEAAAQQH8w7YFlLCE63JNLG
# KX7zUQIBAAIBAAIBAAIBAAIBADAxMA0GCWCGSAFlAwQCAQUABCCEf7aZFbRPy7Ii
# 1JY/MfTR2/3eRKC6xmraDOYx9Wg57qCCGzkwggXMMIIDtKADAgECAhBUmNLR1FsZ
# lUgTecgRwIeZMA0GCSqGSIb3DQEBDAUAMHcxCzAJBgNVBAYTAlVTMR4wHAYDVQQK
# ExVNaWNyb3NvZnQgQ29ycG9yYXRpb24xSDBGBgNVBAMTP01pY3Jvc29mdCBJZGVu
# dGl0eSBWZXJpZmljYXRpb24gUm9vdCBDZXJ0aWZpY2F0ZSBBdXRob3JpdHkgMjAy
# MDAeFw0yMDA0MTYxODM2MTZaFw00NTA0MTYxODQ0NDBaMHcxCzAJBgNVBAYTAlVT
# MR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRpb24xSDBGBgNVBAMTP01pY3Jv
# c29mdCBJZGVudGl0eSBWZXJpZmljYXRpb24gUm9vdCBDZXJ0aWZpY2F0ZSBBdXRo
# b3JpdHkgMjAyMDCCAiIwDQYJKoZIhvcNAQEBBQADggIPADCCAgoCggIBALORKgeD
# Bmf9np3gx8C3pOZCBH8Ppttf+9Va10Wg+3cL8IDzpm1aTXlT2KCGhFdFIMeiVPvH
# or+Kx24186IVxC9O40qFlkkN/76Z2BT2vCcH7kKbK/ULkgbk/WkTZaiRcvKYhOuD
# PQ7k13ESSCHLDe32R0m3m/nJxxe2hE//uKya13NnSYXjhr03QNAlhtTetcJtYmrV
# qXi8LW9J+eVsFBT9FMfTZRY33stuvF4pjf1imxUs1gXmuYkyM6Nix9fWUmcIxC70
# ViueC4fM7Ke0pqrrBc0ZV6U6CwQnHJFnni1iLS8evtrAIMsEGcoz+4m+mOJyoHI1
# vnnhnINv5G0Xb5DzPQCGdTiO0OBJmrvb0/gwytVXiGhNctO/bX9x2P29Da6SZEi3
# W295JrXNm5UhhNHvDzI9e1eM80UHTHzgXhgONXaLbZ7LNnSrBfjgc10yVpRnlyUK
# xjU9lJfnwUSLgP3B+PR0GeUw9gb7IVc+BhyLaxWGJ0l7gpPKWeh1R+g/OPTHU3mg
# trTiXFHvvV84wRPmeAyVWi7FQFkozA8kwOy6CXcjmTimthzax7ogttc32H83rwjj
# O3HbbnMbfZlysOSGM1l0tRYAe1BtxoYT2v3EOYI9JACaYNq6lMAFUSw0rFCZE4e7
# swWAsk0wAly4JoNdtGNz764jlU9gKL431VulAgMBAAGjVDBSMA4GA1UdDwEB/wQE
# AwIBhjAPBgNVHRMBAf8EBTADAQH/MB0GA1UdDgQWBBTIftJqhSobyhmYBAcnz1AQ
# T2ioojAQBgkrBgEEAYI3FQEEAwIBADANBgkqhkiG9w0BAQwFAAOCAgEAr2rd5hnn
# LZRDGU7L6VCVZKUDkQKL4jaAOxWiUsIWGbZqWl10QzD0m/9gdAmxIR6QFm3FJI9c
# Zohj9E/MffISTEAQiwGf2qnIrvKVG8+dBetJPnSgaFvlVixlHIJ+U9pW2UYXeZJF
# xBA2CFIpF8svpvJ+1Gkkih6PsHMNzBxKq7Kq7aeRYwFkIqgyuH4yKLNncy2RtNwx
# AQv3Rwqm8ddK7VZgxCwIo3tAsLx0J1KH1r6I3TeKiW5niB31yV2g/rarOoDXGpc8
# FzYiQR6sTdWD5jw4vU8w6VSp07YEwzJ2YbuwGMUrGLPAgNW3lbBeUU0i/OxYqujY
# lLSlLu2S3ucYfCFX3VVj979tzR/SpncocMfiWzpbCNJbTsgAlrPhgzavhgplXHT2
# 6ux6anSg8Evu75SjrFDyh+3XOjCDyft9V77l4/hByuVkrrOj7FjshZrM77nq81YY
# uVxzmq/FdxeDWds3GhhyVKVB0rYjdaNDmuV3fJZ5t0GNv+zcgKCf0Xd1WF81E+Al
# GmcLfc4l+gcK5GEh2NQc5QfGNpn0ltDGFf5Ozdeui53bFv0ExpK91IjmqaOqu/dk
# ODtfzAzQNb50GQOmxapMomE2gj4d8yu8l13bS3g7LfU772Aj6PXsCyM2la+YZr9T
# 03u4aUoqlmZpxJTG9F9urJh4iIAGXKKy7aIwggaXMIIEf6ADAgECAhMzAAL6EtMZ
# rTGHUvSjAAAAAvoSMA0GCSqGSIb3DQEBDAUAMFoxCzAJBgNVBAYTAlVTMR4wHAYD
# VQQKExVNaWNyb3NvZnQgQ29ycG9yYXRpb24xKzApBgNVBAMTIk1pY3Jvc29mdCBJ
# RCBWZXJpZmllZCBDUyBBT0MgQ0EgMDQwHhcNMjYwNzEwMjIyNzE1WhcNMjYwNzEz
# MjIyNzE1WjBYMQswCQYDVQQGEwJVUzELMAkGA1UECBMCdHgxDjAMBgNVBAcTBVBs
# YW5vMRUwEwYDVQQKEwxSb2RuZXkgVmlhbmExFTATBgNVBAMTDFJvZG5leSBWaWFu
# YTCCAaIwDQYJKoZIhvcNAQEBBQADggGPADCCAYoCggGBAL0Hmnd2INfIpZchqcwt
# D6asaAtOmW7BvIEIkesfZK7maY2sChTOi6R3dLt9ZML7vb5bFXay76HLHPCnXBc0
# 9mwv6RG6W8QHFy+SuRGvzauKVKctXR1Dg5ZINVAOLLq0q29Cw3rghDl+BEGh1JCY
# Cp5lEFmofqdWZhQcnoJl4aeqoFTeDpjopVYnLri4y40yKoLOk17px4OWR9werZ67
# bRpl4WliJjr0n83vMsOkY+mbddEaQmLLos/FfkVPL+cmGLjhRZOqMT0R+Lu5yMOK
# rPCT+9VORsBDjxv5jmtXmO1gL4ShtMD+7uk8VBnQmtL6WCta+l7DdiK/g2Fg90kH
# qRZeru22SCSCkTpQ2In8DJbB98xdjU5mdrvMvmPvjoXTDfZ3mGnqjKyZtBqblUqm
# Zxpqa+xcssL1FUkPo+RIK7XBEMw/GuElPVC0QYQay4ZAsr4VN2Wy9jOcMNHNWs3B
# geqLamsRQDnR6BPE98OoJ73WwNCmWMoEPe8ufCVOWjGJswIDAQABo4IB1jCCAdIw
# DAYDVR0TAQH/BAIwADAOBgNVHQ8BAf8EBAMCB4AwPQYDVR0lBDYwNAYKKwYBBAGC
# N2EBAAYIKwYBBQUHAwMGHCsGAQQBgjdhgqLkwHyDpOGbVYLgup4Pgt+V8TwwHQYD
# VR0OBBYEFB+tlDLdSEtNG9CIsexxoCXrO2DkMB8GA1UdIwQYMBaAFGslQd77a3z9
# GIAKLX+Pdl2qcz24MGcGA1UdHwRgMF4wXKBaoFiGVmh0dHA6Ly93d3cubWljcm9z
# b2Z0LmNvbS9wa2lvcHMvY3JsL01pY3Jvc29mdCUyMElEJTIwVmVyaWZpZWQlMjBD
# UyUyMEFPQyUyMENBJTIwMDQuY3JsMHQGCCsGAQUFBwEBBGgwZjBkBggrBgEFBQcw
# AoZYaHR0cDovL3d3dy5taWNyb3NvZnQuY29tL3BraW9wcy9jZXJ0cy9NaWNyb3Nv
# ZnQlMjBJRCUyMFZlcmlmaWVkJTIwQ1MlMjBBT0MlMjBDQSUyMDA0LmNydDBUBgNV
# HSAETTBLMEkGBFUdIAAwQTA/BggrBgEFBQcCARYzaHR0cDovL3d3dy5taWNyb3Nv
# ZnQuY29tL3BraW9wcy9Eb2NzL1JlcG9zaXRvcnkuaHRtMA0GCSqGSIb3DQEBDAUA
# A4ICAQBXBiu6HSj9du5WlZApOje/6rNTe21U7PtHIOQn3h6N9rtVlh7AuNHg1bg2
# L6Iq27CKmm+ZJW0FLy2vn2vLxDPO0cKLXG97mKTCyKAwNE+FxiyNNrvIhskdgwvA
# zSRzDG/PsppwyuuMJmWbzTvJoLRFLA5PoyA/BZRxqlaKof7B6/u4LoavwXY42PGS
# bGpCcEb/XpGhxC77bvrpOYQX/w971/oqtVqWMnHjlc0RqsQeluV6JNoSQnw/4Xbg
# LKy8zk2P0JX1rZW5WuXdpnfFodNu9McjhKXozPQssQBL9k3cQSq75sjKBDrRtoln
# EUxwG9CM0QMeDJDcVbWJSy5jj6xOVetB/OVtxFj4OERhpdWApjOUSVMbtYyuM5sm
# 2Ms3HvtpwYNL/KpPTwp0djU4xfsb4RiQCa+k9Y9MXQoZSRDp3Tnx2wG9IYhWOh2d
# g+VcCnd+WWqVFF9aM3RrvzDHyRYVSodyq8A8R4C++mhj2R7muVvRdcZvu3jG7S+d
# N2trRa158CBwLSoV5NInwZaPQN2Uks5pRiSLa0bG8ir7ry5lVud/QDKqKEtef9yU
# 2YJLgX6ECmyHVz2/2RyivsmwbGlRPRkF7ZebkhVqexmTD3MRKOitBZCoDLhanT/X
# oE1jKFy5iDuVMxqhWrfS0vDV0LVLO/SIt7EEC59L41SX+IsOtzCCBygwggUQoAMC
# AQICEzMAAAAWMZKNkgJle5oAAAAAABYwDQYJKoZIhvcNAQEMBQAwYzELMAkGA1UE
# BhMCVVMxHjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jwb3JhdGlvbjE0MDIGA1UEAxMr
# TWljcm9zb2Z0IElEIFZlcmlmaWVkIENvZGUgU2lnbmluZyBQQ0EgMjAyMTAeFw0y
# NjAzMjYxODExMjlaFw0zMTAzMjYxODExMjlaMFoxCzAJBgNVBAYTAlVTMR4wHAYD
# VQQKExVNaWNyb3NvZnQgQ29ycG9yYXRpb24xKzApBgNVBAMTIk1pY3Jvc29mdCBJ
# RCBWZXJpZmllZCBDUyBBT0MgQ0EgMDQwggIiMA0GCSqGSIb3DQEBAQUAA4ICDwAw
# ggIKAoICAQDKVfrI2+gJMM/0bQ5OVKNdvOASzLbUUMvXuf+Vl7YGuofPaZHVo3gM
# HF5inT+GMSpIcfIZ9qtXU1UG68ry8vNbQtOL4Nm30ifXpqI1+ByiAWLO1YT0WnzG
# 7XPOuoTeeWsNZv5FmjxCsReBZvyzyzCyXZbu1EQfJxWTH4ebUwtAiW9rqMf9eDj/
# wYhiEfNteJV3ZFeibD2ztCHr9JhFdd97XbnCHgQoTIqc02X5xlRKtUGBa++OtHBB
# jiJ/uwBnzTkqu4FjpZjQeJtrmda+ur1CT2jflWIB/ypn7u7V9tvW9wJbJYt/H2Et
# J0GONWxJZ7TEu8jWPindOO3lzPP7UtzS/mVDV94HucWaltmsra6zSG8BoEJ87IM8
# QSb7vfm/O41FhYkUv89WIj5ES2O4kxyiMSfe95CMivCuYrRP2hKvx7egPMrWgDDB
# kxMLgrKZO9hRNUMm8vk3w5b9SogHOyJVhxyFm8aFXfIxgqDF4S0g4bhbhnzljmSl
# CLlumMZcXFGDjpF2tNoAu3VGFGYtHtTSNVKvZpgB3b4ynaoDkbPf+Wg4523jt4Vn
# easBgZhC1srZI2NCnCBBfgjLq04pqEKAWEohyW2K29KSkkHvt5VaE1ac3Yt+oyiO
# zMS57tXwQDJLGvLg/OXFO0VNvczDndfIfXYExB/ab2PuMSwd5VIBOwIDAQABo4IB
# 3DCCAdgwDgYDVR0PAQH/BAQDAgGGMBAGCSsGAQQBgjcVAQQDAgEAMB0GA1UdDgQW
# BBRrJUHe+2t8/RiACi1/j3ZdqnM9uDBUBgNVHSAETTBLMEkGBFUdIAAwQTA/Bggr
# BgEFBQcCARYzaHR0cDovL3d3dy5taWNyb3NvZnQuY29tL3BraW9wcy9Eb2NzL1Jl
# cG9zaXRvcnkuaHRtMBkGCSsGAQQBgjcUAgQMHgoAUwB1AGIAQwBBMBIGA1UdEwEB
# /wQIMAYBAf8CAQAwHwYDVR0jBBgwFoAU2UEpsA8PY2zvadf1zSmepEhqMOYwcAYD
# VR0fBGkwZzBloGOgYYZfaHR0cDovL3d3dy5taWNyb3NvZnQuY29tL3BraW9wcy9j
# cmwvTWljcm9zb2Z0JTIwSUQlMjBWZXJpZmllZCUyMENvZGUlMjBTaWduaW5nJTIw
# UENBJTIwMjAyMS5jcmwwfQYIKwYBBQUHAQEEcTBvMG0GCCsGAQUFBzAChmFodHRw
# Oi8vd3d3Lm1pY3Jvc29mdC5jb20vcGtpb3BzL2NlcnRzL01pY3Jvc29mdCUyMElE
# JTIwVmVyaWZpZWQlMjBDb2RlJTIwU2lnbmluZyUyMFBDQSUyMDIwMjEuY3J0MA0G
# CSqGSIb3DQEBDAUAA4ICAQAG1VBeVHTVRBljlcZD3IiMxwPyMjQyLNaEnVu5mODm
# 2hRBJfH8GsBLATmrHAc8F47jmk5CnpUPiIguCbw6Z/KVj4Dsoiq228NSLMLewFfG
# Mri7uwNGLISC5ccp8vUdADDEIsS2dE+QI9OwkDpv3XuUD7d+hAgcLVcMOl1AsfEZ
# tsZenhGvSYUrm/FuLq0BqEGL9GXM5c+Ho9q8o+Vn/S+GWQN2y+gkRO15s0kI05nU
# pq/dOD4ri9rgVs6tipEd0YZqGgD+CZNiaZWrDTOQbNPncd2F9qOsUa20miYruoT5
# PwJAaI+QQiTE2ZJeMJOkOpzhTUgqVMZwZidEUZKCqudaeQA08WwnkQMfKyHzaU8j
# 48ULcU4hUwvMsv7fSurOe9GAdRQCPvF8WcSK5oDHe8VVJM4tv6KKCm91HqLx9Jam
# BgRI6R2SfY3nu26EGznu0rCg/769z8xWm4PVcC2ZaL6VlKVqFp1NsN8YqMyf5t+b
# bGVb09noFKcJG/UwyGlxRmQBlfeBUQx5/ytlzZzsEnhrJF9fTAfje8j3OdX5lEne
# PTFQLRlvzZFBqUXnIeQKv3fHQjC9m2fo/Z01DII/qp3d8LhGVUW0BCG04fRwHJNH
# 8iqqCG/qofMv+kym2AxBDnHzNgRjL60JOFiBgiurvLhYQNhB95KWojFA6shQnggk
# MTCCB54wggWGoAMCAQICEzMAAAAHh6M0o3uljhwAAAAAAAcwDQYJKoZIhvcNAQEM
# BQAwdzELMAkGA1UEBhMCVVMxHjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jwb3JhdGlv
# bjFIMEYGA1UEAxM/TWljcm9zb2Z0IElkZW50aXR5IFZlcmlmaWNhdGlvbiBSb290
# IENlcnRpZmljYXRlIEF1dGhvcml0eSAyMDIwMB4XDTIxMDQwMTIwMDUyMFoXDTM2
# MDQwMTIwMTUyMFowYzELMAkGA1UEBhMCVVMxHjAcBgNVBAoTFU1pY3Jvc29mdCBD
# b3Jwb3JhdGlvbjE0MDIGA1UEAxMrTWljcm9zb2Z0IElEIFZlcmlmaWVkIENvZGUg
# U2lnbmluZyBQQ0EgMjAyMTCCAiIwDQYJKoZIhvcNAQEBBQADggIPADCCAgoCggIB
# ALLwwK8ZiCji3VR6TElsaQhVCbRS/3pK+MHrJSj3Zxd3KU3rlfL3qrZilYKJNqzt
# A9OQacr1AwoNcHbKBLbsQAhBnIB34zxf52bDpIO3NJlfIaTE/xrweLoQ71lzCHkD
# 7A4As1Bs076Iu+mA6cQzsYYH/Cbl1icwQ6C65rU4V9NQhNUwgrx9rGQ//h890Q8J
# djLLw0nV+ayQ2Fbkd242o9kH82RZsH3HEyqjAB5a8+Ae2nPIPc8sZU6ZE7iRrRZy
# wRmrKDp5+TcmJX9MRff241UaOBs4NmHOyke8oU1TYrkxh+YeHgfWo5tTgkoSMoay
# qoDpHOLJs+qG8Tvh8SnifW2Jj3+ii11TS8/FGngEaNAWrbyfNrC69oKpRQXY9bGH
# 6jn9NEJv9weFxhTwyvx9OJLXmRGbAUXN1U9nf4lXezky6Uh/cgjkVd6CGUAf0K+J
# w+GE/5VpIVbcNr9rNE50Sbmy/4RTCEGvOq3GhjITbCa4crCzTTHgYYjHs1NbOc6b
# rH+eKpWLtr+bGecy9CrwQyx7S/BfYJ+ozst7+yZtG2wR461uckFu0t+gCwLdN0A6
# cFtSRtR8bvxVFyWwTtgMMFRuBa3vmUOTnfKLsLefRaQcVTgRnzeLzdpt32cdYKp+
# dhr2ogc+qM6K4CBI5/j4VFyC4QFeUP2YAidLtvpXRRo3AgMBAAGjggI1MIICMTAO
# BgNVHQ8BAf8EBAMCAYYwEAYJKwYBBAGCNxUBBAMCAQAwHQYDVR0OBBYEFNlBKbAP
# D2Ns72nX9c0pnqRIajDmMFQGA1UdIARNMEswSQYEVR0gADBBMD8GCCsGAQUFBwIB
# FjNodHRwOi8vd3d3Lm1pY3Jvc29mdC5jb20vcGtpb3BzL0RvY3MvUmVwb3NpdG9y
# eS5odG0wGQYJKwYBBAGCNxQCBAweCgBTAHUAYgBDAEEwDwYDVR0TAQH/BAUwAwEB
# /zAfBgNVHSMEGDAWgBTIftJqhSobyhmYBAcnz1AQT2ioojCBhAYDVR0fBH0wezB5
# oHegdYZzaHR0cDovL3d3dy5taWNyb3NvZnQuY29tL3BraW9wcy9jcmwvTWljcm9z
# b2Z0JTIwSWRlbnRpdHklMjBWZXJpZmljYXRpb24lMjBSb290JTIwQ2VydGlmaWNh
# dGUlMjBBdXRob3JpdHklMjAyMDIwLmNybDCBwwYIKwYBBQUHAQEEgbYwgbMwgYEG
# CCsGAQUFBzAChnVodHRwOi8vd3d3Lm1pY3Jvc29mdC5jb20vcGtpb3BzL2NlcnRz
# L01pY3Jvc29mdCUyMElkZW50aXR5JTIwVmVyaWZpY2F0aW9uJTIwUm9vdCUyMENl
# cnRpZmljYXRlJTIwQXV0aG9yaXR5JTIwMjAyMC5jcnQwLQYIKwYBBQUHMAGGIWh0
# dHA6Ly9vbmVvY3NwLm1pY3Jvc29mdC5jb20vb2NzcDANBgkqhkiG9w0BAQwFAAOC
# AgEAfyUqnv7Uq+rdZgrbVyNMul5skONbhls5fccPlmIbzi+OwVdPQ4H55v7VOInn
# mezQEeW4LqK0wja+fBznANbXLB0KrdMCbHQpbLvG6UA/Xv2pfpVIE1CRFfNF4XKO
# 8XYEa3oW8oVH+KZHgIQRIwAbyFKQ9iyj4aOWeAzwk+f9E5StNp5T8FG7/VEURIVW
# ArbAzPt9ThVN3w1fAZkF7+YU9kbq1bCR2YD+MtunSQ1Rft6XG7b4e0ejRA7mB2Io
# X5hNh3UEauY0byxNRG+fT2MCEhQl9g2i2fs6VOG19CNep7SquKaBjhWmirYyANb0
# RJSLWjinMLXNOAga10n8i9jqeprzSMU5ODmrMCJE12xS/NWShg/tuLjAsKP6SzYZ
# +1Ry358ZTFcx0FS/mx2vSoU8s8HRvy+rnXqyUJ9HBqS0DErVLjQwK8VtsBdekBmd
# TbQVoCgPCqr+PDPB3xajYnzevs7eidBsM71PINK2BoE2UfMwxCCX3mccFgx6UsQe
# RSdVVVNSyALQe6PT12418xon2iDGE81OGCreLzDcMAZnrUAx4XQLUz6ZTl65yPUi
# Oh3k7Yww94lDf+8oG2oZmDh5O1Qe38E+M3vhKwmzIeoB1dVLlz4i3IpaDcR+iuGj
# H2TdaC1ZOmBXiCRKJLj4DT2uhJ04ji+tHD6n58vhavFIrmcxghq7MIIatwIBATBx
# MFoxCzAJBgNVBAYTAlVTMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRpb24x
# KzApBgNVBAMTIk1pY3Jvc29mdCBJRCBWZXJpZmllZCBDUyBBT0MgQ0EgMDQCEzMA
# AvoS0xmtMYdS9KMAAAAC+hIwDQYJYIZIAWUDBAIBBQCggYQwGAYKKwYBBAGCNwIB
# DDEKMAigAoAAoQKAADAZBgkqhkiG9w0BCQMxDAYKKwYBBAGCNwIBBDAcBgorBgEE
# AYI3AgELMQ4wDAYKKwYBBAGCNwIBFTAvBgkqhkiG9w0BCQQxIgQgGMy8EcAf2/gM
# rIGhjwkjqZYPy7lKTdJ3SBp0dt1nvw8wDQYJKoZIhvcNAQEBBQAEggGATQP3FybK
# e1qRCkxPfmnEja3QgA8zUehluG3i2K8x0MhJ2hhgu03tKKxltIHqarPKeqKfXBKb
# EKmvVArAwTDDdhE4qTWf5OGUtxZ1Sjd3ziqqKFsEAsBhRF01FZ+v4aZMCFD3b16M
# 8gtnShyhE7cWI0TxBIEPlzD1Xm5YSQ4ljHUQEEQ08Hw1I+DgnD2HgZu1j1zyQtCm
# H52bUvLdsI6X8HcQKinTrz4LeAVnfxp16MJLRhtkZ1e7j31imk/S5hhpxokQ8jKG
# lsQsIzeMjDpmZ0jGfdLgJgwzp3wX61cptufLHg9IAOIrzV7yWfjoKzBWd2gTQM4S
# uPc8vrlo9jBdX0Fdfe/0Pm6Dc3E9SYlayw89WpnM+2R7P8E3VXoSYnZiI9FNqSsw
# mGFC1+zJ7iy9ZYgEcz45UIna6i+Cy5qacYdHFP7HMAVF11cHPuEdJclCwIBrumsO
# 5978R2xxfm/MW80Bms+v15cgT4guiSddAclEU9xdqWJ9wrIzK3RYjPfAoYIYFDCC
# GBAGCisGAQQBgjcDAwExghgAMIIX/AYJKoZIhvcNAQcCoIIX7TCCF+kCAQMxDzAN
# BglghkgBZQMEAgEFADCCAWIGCyqGSIb3DQEJEAEEoIIBUQSCAU0wggFJAgEBBgor
# BgEEAYRZCgMBMDEwDQYJYIZIAWUDBAIBBQAEIMNIUfaAtc8t3zuQ3oE16hnK4XrM
# twhFtxmwJrBYEQGYAgZqT9V/vhoYEzIwMjYwNzExMjEwNDIwLjMwNFowBIACAfSg
# geGkgd4wgdsxCzAJBgNVBAYTAlVTMRMwEQYDVQQIEwpXYXNoaW5ndG9uMRAwDgYD
# VQQHEwdSZWRtb25kMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRpb24xJTAj
# BgNVBAsTHE1pY3Jvc29mdCBBbWVyaWNhIE9wZXJhdGlvbnMxJzAlBgNVBAsTHm5T
# aGllbGQgVFNTIEVTTjpBNTAwLTA1RTAtRDk0NzE1MDMGA1UEAxMsTWljcm9zb2Z0
# IFB1YmxpYyBSU0EgVGltZSBTdGFtcGluZyBBdXRob3JpdHmggg8hMIIHgjCCBWqg
# AwIBAgITMwAAAAXlzw//Zi7JhwAAAAAABTANBgkqhkiG9w0BAQwFADB3MQswCQYD
# VQQGEwJVUzEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMUgwRgYDVQQD
# Ez9NaWNyb3NvZnQgSWRlbnRpdHkgVmVyaWZpY2F0aW9uIFJvb3QgQ2VydGlmaWNh
# dGUgQXV0aG9yaXR5IDIwMjAwHhcNMjAxMTE5MjAzMjMxWhcNMzUxMTE5MjA0MjMx
# WjBhMQswCQYDVQQGEwJVUzEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0aW9u
# MTIwMAYDVQQDEylNaWNyb3NvZnQgUHVibGljIFJTQSBUaW1lc3RhbXBpbmcgQ0Eg
# MjAyMDCCAiIwDQYJKoZIhvcNAQEBBQADggIPADCCAgoCggIBAJ5851Jj/eDFnwV9
# Y7UGIqMcHtfnlzPREwW9ZUZHd5HBXXBvf7KrQ5cMSqFSHGqg2/qJhYqOQxwuEQXG
# 8kB41wsDJP5d0zmLYKAY8Zxv3lYkuLDsfMuIEqvGYOPURAH+Ybl4SJEESnt0MbPE
# oKdNihwM5xGv0rGofJ1qOYSTNcc55EbBT7uq3wx3mXhtVmtcCEr5ZKTkKKE1CxZv
# NPWdGWJUPC6e4uRfWHIhZcgCsJ+sozf5EeH5KrlFnxpjKKTavwfFP6XaGZGWUG8T
# ZaiTogRoAlqcevbiqioUz1Yt4FRK53P6ovnUfANjIgM9JDdJ4e0qiDRm5sOTiEQt
# BLGd9Vhd1MadxoGcHrRCsS5rO9yhv2fjJHrmlQ0EIXmp4DhDBieKUGR+eZ4CNE3c
# tW4uvSDQVeSp9h1SaPV8UWEfyTxgGjOsRpeexIveR1MPTVf7gt8hY64XNPO6iyUG
# sEgt8c2PxF87E+CO7A28TpjNq5eLiiunhKbq0XbjkNoU5JhtYUrlmAbpxRjb9tSr
# eDdtACpm3rkpxp7AQndnI0Shu/fk1/rE3oWsDqMX3jjv40e8KN5YsJBnczyWB4Jy
# eeFMW3JBfdeAKhzohFe8U5w9WuvcP1E8cIxLoKSDzCCBOu0hWdjzKNu8Y5SwB1lt
# 5dQhABYyzR3dxEO/T1K/BVF3rV69AgMBAAGjggIbMIICFzAOBgNVHQ8BAf8EBAMC
# AYYwEAYJKwYBBAGCNxUBBAMCAQAwHQYDVR0OBBYEFGtpKDo1L0hjQM972K9J6T7Z
# PdshMFQGA1UdIARNMEswSQYEVR0gADBBMD8GCCsGAQUFBwIBFjNodHRwOi8vd3d3
# Lm1pY3Jvc29mdC5jb20vcGtpb3BzL0RvY3MvUmVwb3NpdG9yeS5odG0wEwYDVR0l
# BAwwCgYIKwYBBQUHAwgwGQYJKwYBBAGCNxQCBAweCgBTAHUAYgBDAEEwDwYDVR0T
# AQH/BAUwAwEB/zAfBgNVHSMEGDAWgBTIftJqhSobyhmYBAcnz1AQT2ioojCBhAYD
# VR0fBH0wezB5oHegdYZzaHR0cDovL3d3dy5taWNyb3NvZnQuY29tL3BraW9wcy9j
# cmwvTWljcm9zb2Z0JTIwSWRlbnRpdHklMjBWZXJpZmljYXRpb24lMjBSb290JTIw
# Q2VydGlmaWNhdGUlMjBBdXRob3JpdHklMjAyMDIwLmNybDCBlAYIKwYBBQUHAQEE
# gYcwgYQwgYEGCCsGAQUFBzAChnVodHRwOi8vd3d3Lm1pY3Jvc29mdC5jb20vcGtp
# b3BzL2NlcnRzL01pY3Jvc29mdCUyMElkZW50aXR5JTIwVmVyaWZpY2F0aW9uJTIw
# Um9vdCUyMENlcnRpZmljYXRlJTIwQXV0aG9yaXR5JTIwMjAyMC5jcnQwDQYJKoZI
# hvcNAQEMBQADggIBAF+Idsd+bbVaFXXnTHho+k7h2ESZJRWluLE0Oa/pO+4ge/XE
# izXvhs0Y7+KVYyb4nHlugBesnFqBGEdC2IWmtKMyS1OWIviwpnK3aL5JedwzbeBF
# 7POyg6IGG/XhhJ3UqWeWTO+Czb1c2NP5zyEh89F72u9UIw+IfvM9lzDmc2O2END7
# MPnrcjWdQnrLn1Ntday7JSyrDvBdmgbNnCKNZPmhzoa8PccOiQljjTW6GePe5sGF
# uRHzdFt8y+bN2neF7Zu8hTO1I64XNGqst8S+w+RUdie8fXC1jKu3m9KGIqF4aldr
# YBamyh3g4nJPj/LR2CBaLyD+2BuGZCVmoNR/dSpRCxlot0i79dKOChmoONqbMI8m
# 04uLaEHAv4qwKHQ1vBzbV/nG89LDKbRSSvijmwJwxRxLLpMQ/u4xXxFfR4f/gksS
# kbJp7oqLwliDm/h+w0aJ/U5ccnYhYb7vPKNMN+SZDWycU5ODIRfyoGl59BsXR/Hp
# RGtiJquOYGmvA/pk5vC1lcnbeMrcWD/26ozePQ/TWfNXKBOmkFpvPE8CH+EeGGWz
# qTCjdAsno2jzTeNSxlx3glDGJgcdz5D/AAxw9Sdgq/+rY7jjgs7X6fqPTXPmaCAJ
# KVHAP19oEjJIBwD1LyHbaEgBxFCogYSOiUIr0Xqcr1nJfiWG2GwYe6ZoAF1bMIIH
# lzCCBX+gAwIBAgITMwAAAFZ+j51YCI7pYAAAAAAAVjANBgkqhkiG9w0BAQwFADBh
# MQswCQYDVQQGEwJVUzEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMTIw
# MAYDVQQDEylNaWNyb3NvZnQgUHVibGljIFJTQSBUaW1lc3RhbXBpbmcgQ0EgMjAy
# MDAeFw0yNTEwMjMyMDQ2NTFaFw0yNjEwMjIyMDQ2NTFaMIHbMQswCQYDVQQGEwJV
# UzETMBEGA1UECBMKV2FzaGluZ3RvbjEQMA4GA1UEBxMHUmVkbW9uZDEeMBwGA1UE
# ChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMSUwIwYDVQQLExxNaWNyb3NvZnQgQW1l
# cmljYSBPcGVyYXRpb25zMScwJQYDVQQLEx5uU2hpZWxkIFRTUyBFU046QTUwMC0w
# NUUwLUQ5NDcxNTAzBgNVBAMTLE1pY3Jvc29mdCBQdWJsaWMgUlNBIFRpbWUgU3Rh
# bXBpbmcgQXV0aG9yaXR5MIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEA
# tKWfm/ul027/d8Rlb8Mn/g0QUvvLqY2Vsy3tI8U2tFSspTZomZOD3BHT8LkR+Rrh
# MJgb1VjAKFNysaK9cLSXifPGSIBrPCgs9P4y24lrJEmrV6Q5z4BmqMhIPrZhEvZn
# WpCS4HO7jYSei/nxmC7/1Er+l5Lg3PmSxb8d2IVcARxSw1B4mxB6XI0nkel9wa1d
# Yb2wfGpofraFmxZOxT9eNht4LH0RBSVueba6ZNpjS/0gtfm7qiIiyP6p6PRzTTbM
# nVqsHnV/d/rW0zHx+Q+QNZ5wUqKmTZJB9hU853+2pX5rDfK32uNY9/WBOAmzbqgp
# EdQkbiMavUMyUDShmycIvgHdQnS207sTj8M+kJL3tOdahPuPqMwsaCCgdfwwQx0O
# 9TKe7FSvbAEYs1AnldCl/KHGZCOVvUNqjyL10JLe0/+GD9/ynqXGWFpXOjaunvZ/
# cKROhjN4M5e6xx0b2miqcPii4/ii2ZheKallJET7CKlpFShs3wyg6F/fojQxQvPn
# bWD4Nyx6lhjWjwmoLcx6w1FSCtavLCly33BLRSlTU4qKUxaa8d7YN7Eqpn9XO0SY
# 0umOvKFXrWH7rxl+9iaicitdnTTksAnRjvekdKT3lg7lRMfmfZU8vXNiN0UYJzT9
# EjqjRm0uN/h0oXxPhNfPYqeFbyPXGGxzaYUz6zx3qTcCAwEAAaOCAcswggHHMB0G
# A1UdDgQWBBS+tjPyu6tZ/h5GsyLvyz1H+FNIWjAfBgNVHSMEGDAWgBRraSg6NS9I
# Y0DPe9ivSek+2T3bITBsBgNVHR8EZTBjMGGgX6BdhltodHRwOi8vd3d3Lm1pY3Jv
# c29mdC5jb20vcGtpb3BzL2NybC9NaWNyb3NvZnQlMjBQdWJsaWMlMjBSU0ElMjBU
# aW1lc3RhbXBpbmclMjBDQSUyMDIwMjAuY3JsMHkGCCsGAQUFBwEBBG0wazBpBggr
# BgEFBQcwAoZdaHR0cDovL3d3dy5taWNyb3NvZnQuY29tL3BraW9wcy9jZXJ0cy9N
# aWNyb3NvZnQlMjBQdWJsaWMlMjBSU0ElMjBUaW1lc3RhbXBpbmclMjBDQSUyMDIw
# MjAuY3J0MAwGA1UdEwEB/wQCMAAwFgYDVR0lAQH/BAwwCgYIKwYBBQUHAwgwDgYD
# VR0PAQH/BAQDAgeAMGYGA1UdIARfMF0wUQYMKwYBBAGCN0yDfQEBMEEwPwYIKwYB
# BQUHAgEWM2h0dHA6Ly93d3cubWljcm9zb2Z0LmNvbS9wa2lvcHMvRG9jcy9SZXBv
# c2l0b3J5Lmh0bTAIBgZngQwBBAIwDQYJKoZIhvcNAQEMBQADggIBAA4DqAXEsO26
# j/La7Fgn/Qifit8xuZekqZ57+Ye+sH/hRTbEEjGYrZgsqwR/lUUfKCFpbZF8msaZ
# PQJOR4YYUEU8XyjLrn8Y1jCSmoxh9l7tWiSoc/JFBw356JAmzGGxeBA2EWSxRuTr
# 1AuZe6nYaN8/wtFkiHcs8gMadxXBs6DxVhyu5YnhLPQkfumKm3lFftwE7pieV7f1
# lskmlgsC6AeSGCzGPZUgCvcH5Tv/Qe9z7bIImSD3SuzhOIwaP+eKQTYf67TifyJK
# kWQSdGfTA6Kcu41k8LB6oPK+MLk1jbxxK5wPqLSL62xjK04SBXHEJSEnsFt0zxWk
# xP/lgej1DxqUnmrYEdkxvzKSHIAqFWSZul/5hI+vJxvFPhsNQBEk4cSulDkJQpcd
# Vi/gmf/mHFOYhDBjsa15s4L+2sBil3XV/T8RiR66Q8xYvTLRWxd2dVsrOoCwnsU4
# WIeiC0JinCv1WLHEh7Qyzr9RSr4kKJLWdpNYLhgjkojTmEkAjFO774t3xB7enbvI
# F0GOsV19xnCUzq9EGKyt0gMuaphKlNjJ+aTpjWMZDGo+GOKsnp93Hmftml0Syp3F
# 9+M3y+y6WJGUZoIZJq227jDjjEndtpUrh9BdPdVIfVJD/Au81Rzh05UHAivorQ3O
# s8PELHIgiOd9TWzbdgmGzcILt/ddVQERMYIHRjCCB0ICAQEweDBhMQswCQYDVQQG
# EwJVUzEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMTIwMAYDVQQDEylN
# aWNyb3NvZnQgUHVibGljIFJTQSBUaW1lc3RhbXBpbmcgQ0EgMjAyMAITMwAAAFZ+
# j51YCI7pYAAAAAAAVjANBglghkgBZQMEAgEFAKCCBJ8wEQYLKoZIhvcNAQkQAg8x
# AgUAMBoGCSqGSIb3DQEJAzENBgsqhkiG9w0BCRABBDAcBgkqhkiG9w0BCQUxDxcN
# MjYwNzExMjEwNDIwWjAvBgkqhkiG9w0BCQQxIgQg8LZp0DIFv2WpqQojta9dsWhB
# VNjNJA+g760q0/IHdeAwgbkGCyqGSIb3DQEJEAIvMYGpMIGmMIGjMIGgBCC2DDMl
# TaTj8JV3iTg5Xnpe4CSH60143Z+X9o5NBgMMqDB8MGWkYzBhMQswCQYDVQQGEwJV
# UzEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMTIwMAYDVQQDEylNaWNy
# b3NvZnQgUHVibGljIFJTQSBUaW1lc3RhbXBpbmcgQ0EgMjAyMAITMwAAAFZ+j51Y
# CI7pYAAAAAAAVjCCA2EGCyqGSIb3DQEJEAISMYIDUDCCA0yhggNIMIIDRDCCAiwC
# AQEwggEJoYHhpIHeMIHbMQswCQYDVQQGEwJVUzETMBEGA1UECBMKV2FzaGluZ3Rv
# bjEQMA4GA1UEBxMHUmVkbW9uZDEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0
# aW9uMSUwIwYDVQQLExxNaWNyb3NvZnQgQW1lcmljYSBPcGVyYXRpb25zMScwJQYD
# VQQLEx5uU2hpZWxkIFRTUyBFU046QTUwMC0wNUUwLUQ5NDcxNTAzBgNVBAMTLE1p
# Y3Jvc29mdCBQdWJsaWMgUlNBIFRpbWUgU3RhbXBpbmcgQXV0aG9yaXR5oiMKAQEw
# BwYFKw4DAhoDFQD/c/cpFSqQWYBeXggyRJ2ZbvYEEaBnMGWkYzBhMQswCQYDVQQG
# EwJVUzEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMTIwMAYDVQQDEylN
# aWNyb3NvZnQgUHVibGljIFJTQSBUaW1lc3RhbXBpbmcgQ0EgMjAyMDANBgkqhkiG
# 9w0BAQsFAAIFAO389wAwIhgPMjAyNjA3MTExNzA4MTZaGA8yMDI2MDcxMjE3MDgx
# NlowdzA9BgorBgEEAYRZCgQBMS8wLTAKAgUA7fz3AAIBADAKAgEAAgIWpwIB/zAH
# AgEAAgIStDAKAgUA7f5IgAIBADA2BgorBgEEAYRZCgQCMSgwJjAMBgorBgEEAYRZ
# CgMCoAowCAIBAAIDB6EgoQowCAIBAAIDAYagMA0GCSqGSIb3DQEBCwUAA4IBAQAN
# gtfn/gY13LaLjSqg3CV2YW8zCY6K6QA0BrpV1qu1QkxJFpi0Z6Fp9bkj2x5v+XPQ
# YThTmsT42yQZ+hUEHsOKWP7BG7M0UcqHX9qqetCQ+OatiqXoYXQffApR84RTn8ii
# kXFBoNJ0GgZLGGmBGsOP9uV5Md4L47fHSjbMpyekDcFNfynx2ILHzgbREnsd18jV
# xIKcp787LdGgj31yNHGa35U+9K5pRpjZbheHQLllTV06bZ0i3+tcW1Q5u5gUUMzv
# r5pDjYlkFBOpQz8mDzetm+T/HmW62VkNmOvw8sScX0yuszR6yuJCWE8+GY3xiTk5
# DQPIi96a7bikkn//UvOUMA0GCSqGSIb3DQEBAQUABIICACQgE5LcTHYCQnMOeeB3
# jUOyn1JihSu4zk7Yy9uFZrexLhVvVXv95Ha+ikWqo1pYWmxPpFHUSeuHVp947yRE
# M7HySZDWWTGsxg7I2HfhtYRYhlEnFUz7CN2SyNRXkEMm+WCmJ3qqF8ZsUUq86gQi
# rOuNXamqEjAu+LPcCeG4lwC7b67531v+g9/3MpC3MJOLZgb51aimdwlCMQyW7Zrg
# BFvPvpof/ao0Tb8qQ2Q9Xlij0zcf0ZmWrtsC1bd5KgPqKHhv+6y9gr86+kPQjTml
# IhhKR1zCa5esmsgumaf/htKspr2BvtA17BZY3aY0esRu7aRKZYIurSCySULCA89K
# c37wrCm9c79EBWAM7FjwNnohhUc4FRA16Y31yE4Qcc74toR8MyGVTnU8Vys2eB6B
# BFaGt8ajvgnj+SQvoAeQltMHWv59OeIqi0WFsgo3OLluNs+k9eWsihU7dYMrWCZR
# TGS8orzbedMwjLD/EBNYyPeyZQMl9kJqXrw20ly4Hnw+zoRW89yASM1OsvIGrCZ1
# XSPDRuwhZBJIDLwIiWlryO9Pj7DARVp0u5Qy3xtztExpYNkByF22kaEJPO3VsXfL
# t7YhxVn2UI+sjJiHfRv5l7hi2DJrx36h+fhOI82t12LxuCoJiv1rCmDTkcbeFN+Q
# gZn6W10zzakkfvFAdM0PjAzI
# SIG # End signature block
