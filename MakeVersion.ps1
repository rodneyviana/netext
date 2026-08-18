function Sign-File ([string]$FilePath, [PSDefaultValue()][string]$TimeStampServer = 'http://timestamp.comodoca.com')
{
   if (Test-Path "$PSScriptRoot\.metadata.json")
   {
      try
      {
         & "$PSScriptRoot\Sign-Artifact.ps1" $FilePath
         Write-Host "Signature was applied succesfully to $($FilePath)"
         return $true
      }
      catch
      {
         Write-Host "Failed to sign $($FilePath): $($_.Exception.Message)"
         return $false
      }
   }

    $cert = Get-ChildItem cert:\CurrentUser\My -CodeSigning

    if($cert)
    {
      $sign = Set-AuthenticodeSignature -FilePath $FilePath -Certificate $cert -IncludeChain All -TimestampServer $TimeStampServer
      if($sign)
      {
        Write-Host "Signature was applied succesfully to $($FilePath)"
        $sign
        return $true
      }
    }
    else
    {
      Write-Host "Unable to find certificate"
    }
    return $false
}




$files = New-Object "System.Collections.Generic.List[string]"
$files.Add('.\ClrMemDiagExt\bin\x64\Release64\NetExtShim.dll');
$files.Add('.\ClrMemDiagExt\bin\x64\Release64\NetExtShim.pdb');
$files.Add('.\ClrMemDiagExt\bin\x86\Release32\NetExtShim.dll');
$files.Add('.\ClrMemDiagExt\bin\x86\Release32\NetExtShim.pdb');
$files.Add('.\x64\Release64\NetExt.dll');
$files.Add('.\x64\Release64\NetExt.pdb');
$files.Add('.\Release32\NetExt.pdb');
$files.Add('.\x86\Release32\NetExt.dll');
$files.Add('.\Binaries\NetExt.tl');
$files.Add('.\Binaries\readme.txt');

$created = $false;
$badversion = $false;
[string]$version = '';
# Test if versions match

foreach($file in $files)
{
   if($file.EndsWith('.dll'))
   {
       $tmpVer = (Get-Item $file).VersionInfo.FileVersion;
       Write-Host "($($tmpVer)) $($file)";
       if($version -eq '')
       {
          $version = $tmpVer;

          continue;
       }
       if($version.ToString() -ne $tmpVer.ToString())
       {
          $badversion = $true;
          break;

       }
   }
}

$OldZipfile = (Get-Item '.\Binaries\*.zip')
$oldVersion = $OldZipfile.Name.Split('-')[1].Replace(".zip", "");

if($OldZipFile.Name.Contains($version.ToString()))
{
   Write-Host "The current version is the same in the zip";
}

if($badversion)
{
  Write-Host "Failed the version test. Aborted";
} else
{
  Write-Host "Old Version: $oldVersion New Version: $version"
  $tmpFolder = "$($env:TEMP)\NetExt-$($version)"
  if([System.IO.Directory]::Exists($tmpFolder))
  {
     [System.IO.Directory]::Delete($tmpFolder, $true);
  }

  $x86 = "$($tmpFolder)\x86";
  $x64 = "$($tmpFolder)\x64";
  [System.IO.Directory]::CreateDirectory($tmpFolder);
  [System.IO.Directory]::CreateDirectory($x86);
  [System.IO.Directory]::CreateDirectory($x64);

  foreach($file in $files)
  {
      if($file.Contains(".dll") -or $file.Contains(".exe"))
      {
         Sign-File -FilePath $file
      }
      if(-not $file.Contains("x86"))
      {
         Copy-Item -Path $file -Destination $x64
         Copy-Item -Path $file -Destination .\Binaries\x64
         Write-Host "Copying $file to $x64";
      }

      if(-not $file.Contains("x64"))
      {
         Copy-Item -Path $file -Destination $x86
         Copy-Item -Path $file -Destination .\Binaries\x86

         Write-Host "Copying $file to $x86";
      }

      if($file.EndsWith('.tl') -or $file.EndsWith('.txt'))
      {
         Copy-Item -Path $file -Destination $tmpFolder
         Write-Host "Copying $file to $tmpFolder";
      }

  }

  Compress-Archive -Path "$($tmpFolder)" -CompressionLevel Optimal -DestinationPath "$($tmpFolder).zip" -Force -Verbose
  Write-Host "Zip created at $($tmpFolder).zip"
  Copy-Item "$($tmpFolder).zip" .\Binaries\
  Copy-Item $OldZipfile .\Binaries\Archive -Force
  
  Remove-Item $OldZipfile -Verbose
  Remove-Item "$($tmpFolder).zip" -Verbose
  Remove-Item "$($tmpFolder)\*" -Recurse -Verbose
  

  $help = Get-Content .\README.md
  if(($help -match $oldVersion).Count -gt 0)
  {
    Copy-Item .\README.md .\README.md.bak -Force -Verbose

    # Out-File defaults to UTF-16LE with a BOM in Windows PowerShell, which silently
    # corrupts README.md's encoding (it must stay UTF-8, no BOM). Write it manually instead.
    $updated = ($help -replace $oldVersion,$version) -join "`r`n"
    [System.IO.File]::WriteAllText((Resolve-Path .\README.md), "$updated`r`n", (New-Object System.Text.UTF8Encoding($false)))
    Copy-Item .\README.md .\Binaries\README.md -Force -Verbose

  }

  

  
}

& pause



# SIG # Begin signature block
# MII2ngYJKoZIhvcNAQcCoII2jzCCNosCAQExDzANBglghkgBZQMEAgEFADB5Bgor
# BgEEAYI3AgEEoGswaTA0BgorBgEEAYI3AgEeMCYCAwEAAAQQH8w7YFlLCE63JNLG
# KX7zUQIBAAIBAAIBAAIBAAIBADAxMA0GCWCGSAFlAwQCAQUABCB1LL87Gobwir2n
# fYchZ+646WYFoEexvlpo+opzyBAp4qCCGzkwggXMMIIDtKADAgECAhBUmNLR1FsZ
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
# 03u4aUoqlmZpxJTG9F9urJh4iIAGXKKy7aIwggaXMIIEf6ADAgECAhMzAATh58If
# dJy489mqAAAABOHnMA0GCSqGSIb3DQEBDAUAMFoxCzAJBgNVBAYTAlVTMR4wHAYD
# VQQKExVNaWNyb3NvZnQgQ29ycG9yYXRpb24xKzApBgNVBAMTIk1pY3Jvc29mdCBJ
# RCBWZXJpZmllZCBDUyBBT0MgQ0EgMDQwHhcNMjYwODE2MDA1NjIyWhcNMjYwODE5
# MDA1NjIyWjBYMQswCQYDVQQGEwJVUzELMAkGA1UECBMCdHgxDjAMBgNVBAcTBVBs
# YW5vMRUwEwYDVQQKEwxSb2RuZXkgVmlhbmExFTATBgNVBAMTDFJvZG5leSBWaWFu
# YTCCAaIwDQYJKoZIhvcNAQEBBQADggGPADCCAYoCggGBALzv1T9i1c/ZUmFOUnm/
# giZX3kv8zFADasSWNhOoppED5WL1eeX43Cbd+I/jRTDWsIJzhftduw46ykYevwu6
# 2PK9Vi6L1u3eWZrAww1zqa/kjHz+tcm/t+QkAnP5i+iHFULO7jozkEm8rBPKwujc
# Duhe325e01oJFgoTN1qXdPrRUxGF2prldv1uxAJj/3mYPkBIxteQy06F27k1P0Hd
# eXP4iZI0dDTFJrvO+MwbwTFZDv6JnIA/je18FAh1R6r0QvJ8vh4RqsY+ma0qXsD8
# TctgWbavLS3/jP1kyMWbh3pGe1Kk+V7rJPmJrXZDhXM2ygxpftUwT2ftCgAVd7CE
# MJczQ/sOK4hI9bi7EzOJkJ6j0meKvX6JF3oqmSgvIZxGmjKhYMrw4tX0clhTdYVM
# LZKnDtj4kmwMQaIsnRWHZETmZa7FdENsqy2LsxfLQ9G5J2BRAAFR/tObXUEWC7dN
# +PzXigzHArb/XbcjtBK7Lo4dNaMeAyx6/5wknSjRvGPRGwIDAQABo4IB1jCCAdIw
# DAYDVR0TAQH/BAIwADAOBgNVHQ8BAf8EBAMCB4AwPQYDVR0lBDYwNAYKKwYBBAGC
# N2EBAAYIKwYBBQUHAwMGHCsGAQQBgjdhgqLkwHyDpOGbVYLgup4Pgt+V8TwwHQYD
# VR0OBBYEFKqo3I9NOgBHAMx59QOvz/+8quD+MB8GA1UdIwQYMBaAFGslQd77a3z9
# GIAKLX+Pdl2qcz24MGcGA1UdHwRgMF4wXKBaoFiGVmh0dHA6Ly93d3cubWljcm9z
# b2Z0LmNvbS9wa2lvcHMvY3JsL01pY3Jvc29mdCUyMElEJTIwVmVyaWZpZWQlMjBD
# UyUyMEFPQyUyMENBJTIwMDQuY3JsMHQGCCsGAQUFBwEBBGgwZjBkBggrBgEFBQcw
# AoZYaHR0cDovL3d3dy5taWNyb3NvZnQuY29tL3BraW9wcy9jZXJ0cy9NaWNyb3Nv
# ZnQlMjBJRCUyMFZlcmlmaWVkJTIwQ1MlMjBBT0MlMjBDQSUyMDA0LmNydDBUBgNV
# HSAETTBLMEkGBFUdIAAwQTA/BggrBgEFBQcCARYzaHR0cDovL3d3dy5taWNyb3Nv
# ZnQuY29tL3BraW9wcy9Eb2NzL1JlcG9zaXRvcnkuaHRtMA0GCSqGSIb3DQEBDAUA
# A4ICAQCBCgQLEL633M0Gooo/ogxGFVE0XlTWvBCkihJ8rRn78lUePZBRYr5xvGhR
# gq2rVKsrNIfYbPyCybeyfENxeuHpJQMBeERMced1aIqCiuSBaPkCzbXjpStxqwrz
# 7THoLKZAOjgmRw2FCt6KHkjCbkKcugGYZlJj6HTrNNfNYCXdjUHzk4LUnfpg0tvq
# Eq0R4S04p/EGGgJyw8Lr7IP8Z+OatEvD7E+YoqVKt95d0Ox170zKbms8RtzZA8/9
# Lu80WYoUy4paMDcrEbXLuE9Gq4uuBIJRO2I4diaicbjKbx2pYqQvOPRyuN93g6Te
# VWGZBusZDIJTNiEAXo7FglYBxiep5bgVbbqZ5knBH18zcCZQv/579B9+F0kepomN
# +aDMIDbBgNCxS9sCwaAcJFHzvAoThTEXYxIP43TQPl5UtwMW7R3RXpOrnqRyH+cp
# Bz1YEHfsFk0tHdUiYr13xhs8l1Rbb7X84IU03jbCqgqXUkmS3N/JQHSYefWuZa2L
# PcCu1BY3ZJVPUg9lVrLR9TjUKQeeb/xuULeN1L5HReqvy5WqEA6e4JGwZqt4RdA/
# 7xlW29d4eW6WmQ9UlVnZigNpygJRvS7vY9EHE+V4qB0g98Ei3dcFY8uAZ9LoRuLK
# Prx4RtcZYcmi9R7jhE6Mjbf7Aj/0iQof3WANSw0l1+RW3xRY0DCCBygwggUQoAMC
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
# BOHnwh90nLjz2aoAAAAE4ecwDQYJYIZIAWUDBAIBBQCggYQwGAYKKwYBBAGCNwIB
# DDEKMAigAoAAoQKAADAZBgkqhkiG9w0BCQMxDAYKKwYBBAGCNwIBBDAcBgorBgEE
# AYI3AgELMQ4wDAYKKwYBBAGCNwIBFTAvBgkqhkiG9w0BCQQxIgQgAxbBLsEVbZfq
# TsxJ4DJ6DGpyrt8yoBgAkVFMTGpqaC0wDQYJKoZIhvcNAQEBBQAEggGALbxnkb/Q
# TiMoJHEElL6bJxVtE5yClVUtxSYdH93RoSmQW2u5I21KKUa+iYpIxlQzdtYCDI7f
# 1moI7dnZoDHsRBaSwplQePi71SmMiWpKq0LWsCWqK9i7uJWwev9AtH9IRQBdj5W+
# 0fLOyZVFYz18cGj5rc6g7CqYw8LAOSaWdZOGdAcirJomGOThJ3xcO1IoRHvqeuRt
# 2nYZ6BKZBHdOv7bzSnnaf6KmH2i6UyQgY3SoqRFQjvl2tU35hY2mbcru66PvKNbF
# L/aAIgllyyVwxzPX/gQoVLzJzqUvPwziXoeM5ityknSVZn4omvqjSamfiQ8iKJga
# wnK54aHvysqo22OXRqnNq1/0gYrK4XWYj/SR2Kl5MSDLcdPbId9T/bCDQz9HlCyI
# +AD9d4O/mXyxr/ZLtERISM1/AkFHWbRU727qa0XVmod33JDveg6GXQqrSSQ8/y0z
# ubpqABtzUPu6Z4ETlMYp47pL8I1pZCME4hAmPCBebXpWMX4ORTTNX6YOoYIYFDCC
# GBAGCisGAQQBgjcDAwExghgAMIIX/AYJKoZIhvcNAQcCoIIX7TCCF+kCAQMxDzAN
# BglghkgBZQMEAgEFADCCAWIGCyqGSIb3DQEJEAEEoIIBUQSCAU0wggFJAgEBBgor
# BgEEAYRZCgMBMDEwDQYJYIZIAWUDBAIBBQAEICbbd9KEa2aWsUlGn25uuVn4WDBW
# SGVMEACXf4cZCTn+AgZqaJ6JcYEYEzIwMjYwODE4MDAyNDE5Ljg4M1owBIACAfSg
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
# MjYwODE4MDAyNDE5WjAvBgkqhkiG9w0BCQQxIgQgA5WyVmVrW4/RF4U8jjQHzUDh
# 5flye+elX3/mv0dyhxQwgbkGCyqGSIb3DQEJEAIvMYGpMIGmMIGjMIGgBCC2DDMl
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
# 9w0BAQsFAAIFAO4uI6YwIhgPMjAyNjA4MTgwMDE5NTBaGA8yMDI2MDgxOTAwMTk1
# MFowdzA9BgorBgEEAYRZCgQBMS8wLTAKAgUA7i4jpgIBADAKAgEAAgIN4gIB/zAH
# AgEAAgISXjAKAgUA7i91JgIBADA2BgorBgEEAYRZCgQCMSgwJjAMBgorBgEEAYRZ
# CgMCoAowCAIBAAIDB6EgoQowCAIBAAIDAYagMA0GCSqGSIb3DQEBCwUAA4IBAQA+
# OVzkL9B10Bb9gepyI5Kgm1GRVdlblc1aqslTFFP8nsgkZpVvfPrbaEiPnRiZdhhZ
# vYl68VcF59JBerC+m6ZQQ2jJb5rpNSTWE+KF64LIOhV3aQjCITXgPFYF4YbWPXzf
# EvnHD6R8GRLKfIjV5ClCUgcDQW/K8tZ1M2G8qHFgJRIwwYF7fYzyYMnZVAA6Ixn4
# 5mgZEd/AeHEj9/S/vYq8aFqGyDX7Jl0I/uEK71pR1BMVcoIzvhf4y+W8xQsXdCYD
# o0WMgNGBAxdUwuVv8BHLH71hhQBi+M3NnyikPHrWICMXld6XgNJl4RF8pg6oSK+R
# NiZlRkm4kXEgi58OyTq7MA0GCSqGSIb3DQEBAQUABIICACAgo5hqnUYvQCX5zPp8
# IfcNp+V4o5WbXl/09nT7d0A6yvfRQ1ciLfXLc9yoi/19ORg146qZfnZ+eOx23bsO
# 8ESbb2zodftum5FkWqHUTOWFj1O37FyqtbYXtZd5PWlFntVviT4WVEInNeKH82Ep
# Dmhe9oQ3JorkgYf/lR4OlRcPLhj5pD1IeubfT03OgIz5mJj/h1tRwr53vzOPqbm5
# SzCM1Yu61dzjU7nlvvPSKsytNhxGTXuYWiMq7NfhjJqqc7ZDGB8h+Xu6eXHdTBCG
# +PfQdvCgXD7xpRwUfRTmcdguvilN862wKZ1cZKNFnXorw/+IM/aOd/5wYW/+NLOf
# 8Dqh8EZhV3+XVtJt5EN1IkscjE7HI2UihtoruXWSn/idfh3i4N3GwSPY4sal0MFH
# /pawHCCUYaDFFLwgfb/mBtTNzGo3qiH9+34vnc7jrjlp5HuRgQmcqJis5fuBv0ud
# WLHo4MU//4bDuGJtIV+eYr9z/eAkgiHI/HLdMzXAifq2ec2BpzbJ3FeeBxj40+ez
# tAsZ5oHW9P5gAVUE/Ki3E6igIjkMn3ogCFlOXSZE6KMw2bPzBH7RWO9Ms+772YKK
# vf096/IT3TJG1BwNEL0tZ1JJpOnAPdpKp/U9bJF5lAP7KW6t8T5mAbxozV0FFb8D
# QQX1ASFKTcW4n/ne+/31ROg1
# SIG # End signature block
