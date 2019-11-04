function Sign-File ([string]$FilePath, [PSDefaultValue()][string]$TimeStampServer = 'http://timestamp.comodoca.com')
{

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

    $help -replace $oldVersion,$version | Out-File .\README.md -Force
    Copy-Item .\README.md .\Binaries\README.md -Force -Verbose

  }

  

  
}

& pause


# SIG # Begin signature block
# MIIj0QYJKoZIhvcNAQcCoIIjwjCCI74CAQExCzAJBgUrDgMCGgUAMGkGCisGAQQB
# gjcCAQSgWzBZMDQGCisGAQQBgjcCAR4wJgIDAQAABBAfzDtgWUsITrck0sYpfvNR
# AgEAAgEAAgEAAgEAAgEAMCEwCQYFKw4DAhoFAAQUq/zrAfpKjf3Zb/6FevOzukcp
# 6miggh8IMIIEhDCCA2ygAwIBAgIQQhrylAmEGR9SCkvGJCanSzANBgkqhkiG9w0B
# AQUFADBvMQswCQYDVQQGEwJTRTEUMBIGA1UEChMLQWRkVHJ1c3QgQUIxJjAkBgNV
# BAsTHUFkZFRydXN0IEV4dGVybmFsIFRUUCBOZXR3b3JrMSIwIAYDVQQDExlBZGRU
# cnVzdCBFeHRlcm5hbCBDQSBSb290MB4XDTA1MDYwNzA4MDkxMFoXDTIwMDUzMDEw
# NDgzOFowgZUxCzAJBgNVBAYTAlVTMQswCQYDVQQIEwJVVDEXMBUGA1UEBxMOU2Fs
# dCBMYWtlIENpdHkxHjAcBgNVBAoTFVRoZSBVU0VSVFJVU1QgTmV0d29yazEhMB8G
# A1UECxMYaHR0cDovL3d3dy51c2VydHJ1c3QuY29tMR0wGwYDVQQDExRVVE4tVVNF
# UkZpcnN0LU9iamVjdDCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBAM6q
# gT+jo2F4qjEAVZURnicPHxzfOpuCaDDASmEd8S8O+r5596Uj71VRloTN2+O5bj4x
# 2AogZ8f02b+U60cEPgLOKqJdhwQJ9jCdGIqXsqoc/EHSoTbL+z2RuufZcDX65OeQ
# w5ujm9M89RKZd7G3CeBo5hy485RjiGpq/gt2yb70IuRnuasaXnfBhQfdDWy/7gbH
# d2pBnqcP1/vulBe3/IW+pKvEHDHd17bR5PDv3xaPslKT16HUiaEHLr/hARJCHhrh
# 2JU022R5KP+6LhHC5ehbkkj7RwvCbNqtMoNB86XlQXD9ZZBt+vpRxPm9lisZBCzT
# bafc8H9vg2XiaquHhnUCAwEAAaOB9DCB8TAfBgNVHSMEGDAWgBStvZh6NLQm9/rE
# JlTvA73gJMtUGjAdBgNVHQ4EFgQU2u1kdBScFDyr3ZmpvVsoTYs8ydgwDgYDVR0P
# AQH/BAQDAgEGMA8GA1UdEwEB/wQFMAMBAf8wEQYDVR0gBAowCDAGBgRVHSAAMEQG
# A1UdHwQ9MDswOaA3oDWGM2h0dHA6Ly9jcmwudXNlcnRydXN0LmNvbS9BZGRUcnVz
# dEV4dGVybmFsQ0FSb290LmNybDA1BggrBgEFBQcBAQQpMCcwJQYIKwYBBQUHMAGG
# GWh0dHA6Ly9vY3NwLnVzZXJ0cnVzdC5jb20wDQYJKoZIhvcNAQEFBQADggEBAE1C
# L6bBiusHgJBYRoz4GTlmKjxaLG3P1NmHVY15CxKIe0CP1cf4S41VFmOtt1fcOyu9
# 08FPHgOHS0Sb4+JARSbzJkkraoTxVHrUQtr802q7Zn7Knurpu9wHx8OSToM8gUmf
# ktUyCepJLqERcZo20sVOaLbLDhslFq9s3l122B9ysZMmhhfbGN6vRenf+5ivFBjt
# pF72iZRF8FUESt3/J90GSkD2tLzx5A+ZArv9XQ4uKMG+O18aP5cQhLwWPtijnGMd
# ZstcX9o+8w8KCTUi29vAPwD55g1dZ9H9oB4DK9lA977Mh2ZUgKajuPUZYtXSJrGY
# Ju6ay0SnRVqBlRUa9VEwggTUMIIDvKADAgECAhEAoW8/qfSF4+FmAEEQ9HZ4FjAN
# BgkqhkiG9w0BAQsFADBRMQswCQYDVQQGEwJVUzEQMA4GA1UEChMHU1NMLmNvbTEU
# MBIGA1UECxMLd3d3LnNzbC5jb20xGjAYBgNVBAMTEVNTTC5jb20gT2JqZWN0IENB
# MB4XDTE5MTEwMTAwMDAwMFoXDTIwMTAzMTIzNTk1OVowgYkxCzAJBgNVBAYTAlVT
# MQ4wDAYDVQQRDAU3NTA3NDEOMAwGA1UECAwFVGV4YXMxDjAMBgNVBAcMBVBsYW5v
# MRgwFgYDVQQJDA8zNjQ1IERhbmJ1cnkgTG4xFzAVBgNVBAoMDlJvZG5leSBIIFZp
# YW5hMRcwFQYDVQQDDA5Sb2RuZXkgSCBWaWFuYTCCASIwDQYJKoZIhvcNAQEBBQAD
# ggEPADCCAQoCggEBALZevtrd8jdOadFR2IQHpc3c/7LOew67jiPa8tetB69iyAkc
# oGfUDtzPdlMiBCwYKrZIOhLz9TS1XAT3cC1kLmM3q9jwxa98QE11uOfthtGkuqvS
# PCjxXo6wylGIWA5Z0mZ7V2YFJuZTePzabccIGPQWoucNh2jFe9lrKQQDAjeQsbPF
# Odw6ZMXHmielR29ZE/8HyFsvFzGFYFxFGcukeYsxFRUo/fKKYDloIjE0t6DHLStb
# iilEbf1Y254NNFR/jkDtUBvadLBdN7tqnswU4jV+V9qaKWrK0l0AgAbhy5CqvqSh
# WWd9RhJg3cNND39yeI9OJeiVaSTGwNuyNE/qu00CAwEAAaOCAWwwggFoMB8GA1Ud
# IwQYMBaAFHW0d+TXN84WZ4YfFyBLEmHKOJcqMB0GA1UdDgQWBBSyfPMf+T2BtYg3
# kiQ7c21RALNKvDAOBgNVHQ8BAf8EBAMCB4AwDAYDVR0TAQH/BAIwADATBgNVHSUE
# DDAKBggrBgEFBQcDAzARBglghkgBhvhCAQEEBAMCBBAwQAYDVR0gBDkwNzA1Bgor
# BgEEAYKpMAEBMCcwJQYIKwYBBQUHAgEWGWh0dHBzOi8vY3BzLnVzZXJ0cnVzdC5j
# b20wOAYDVR0fBDEwLzAtoCugKYYnaHR0cDovL2NybC5zc2wuY29tL1NTTGNvbU9i
# amVjdENBXzIuY3JsMGQGCCsGAQUFBwEBBFgwVjAzBggrBgEFBQcwAoYnaHR0cDov
# L2NydC5zc2wuY29tL1NTTGNvbU9iamVjdENBXzIuY3J0MB8GCCsGAQUFBzABhhNo
# dHRwOi8vb2NzcC5zc2wuY29tMA0GCSqGSIb3DQEBCwUAA4IBAQAoU+KXXCZSC5Sr
# fI0mb+Qogc2UdBYMmPYcwfHvQszCIYiPGkpUUU7C0iq2ihGqUyRursPlOHynVWav
# d1wBtUCk70dyF52676t7hUejhhBeRGZohg/QQG/cFyljyiWI/0/p2yotqhgZT/By
# SuAydp5Ya8jf0B+q8siAfFjcBAmLcwoImjXsRC0nt2UzGt6ehlQhoT1J2UTqnRfB
# uhtSB2Ld6icn9j1Nz1aWNxJqm/Ka1a6jJ1NIm3tB4+eW+F2c3FPiQ7ktAE8wlgRv
# Z5reWbOq36FU0CYZpB1P95uyzXK6pAI+fHb8Suy/lD03VI2cdmbk3D8/4Lcak/HI
# 5o8I7VQjMIIE5jCCA86gAwIBAgIQYlxNkIzVQvurLqVzP/FUGTANBgkqhkiG9w0B
# AQUFADCBlTELMAkGA1UEBhMCVVMxCzAJBgNVBAgTAlVUMRcwFQYDVQQHEw5TYWx0
# IExha2UgQ2l0eTEeMBwGA1UEChMVVGhlIFVTRVJUUlVTVCBOZXR3b3JrMSEwHwYD
# VQQLExhodHRwOi8vd3d3LnVzZXJ0cnVzdC5jb20xHTAbBgNVBAMTFFVUTi1VU0VS
# Rmlyc3QtT2JqZWN0MB4XDTExMDQyNzAwMDAwMFoXDTIwMDUzMDEwNDgzOFowejEL
# MAkGA1UEBhMCR0IxGzAZBgNVBAgTEkdyZWF0ZXIgTWFuY2hlc3RlcjEQMA4GA1UE
# BxMHU2FsZm9yZDEaMBgGA1UEChMRQ09NT0RPIENBIExpbWl0ZWQxIDAeBgNVBAMT
# F0NPTU9ETyBUaW1lIFN0YW1waW5nIENBMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8A
# MIIBCgKCAQEAqoLxhKlb2HG10l0r7fQTIAz5m+nRj3Rebu7rKUjYyL4s6wphYMy9
# ko183XS7Cp5oTqp1JHMnrjfzGoKlbbjICUZMfqGD6eIDYKQhECdAmlnhba8+gq8h
# aVltY8zOsfYWmnmDlzodDK/JfdFO2MNGoaWYqmDOVdQSEfM7C2zGiLqVLu+gRkU5
# fKzp463oV5df/vvCCY3+jpqlP1lG/MctXYSH8G9YJsPwQvxV3mW+ZzhjsxGxoSXt
# qZu+SN9Md8CqGQu2/UqPLHlVsf5ZWlptVYscQ/axVXIpiU1AP75/SQFdPJtcCPvK
# 4nUVBZOdCGL/ug3bqvi1vxAyynLqCCbNGQIDAQABo4IBSjCCAUYwHwYDVR0jBBgw
# FoAU2u1kdBScFDyr3ZmpvVsoTYs8ydgwHQYDVR0OBBYEFGQihrZKickED9AEWJIr
# s249HidsMA4GA1UdDwEB/wQEAwIBBjASBgNVHRMBAf8ECDAGAQH/AgEAMBMGA1Ud
# JQQMMAoGCCsGAQUFBwMIMBEGA1UdIAQKMAgwBgYEVR0gADBCBgNVHR8EOzA5MDeg
# NaAzhjFodHRwOi8vY3JsLnVzZXJ0cnVzdC5jb20vVVROLVVTRVJGaXJzdC1PYmpl
# Y3QuY3JsMHQGCCsGAQUFBwEBBGgwZjA9BggrBgEFBQcwAoYxaHR0cDovL2NydC51
# c2VydHJ1c3QuY29tL1VUTkFkZFRydXN0T2JqZWN0X0NBLmNydDAlBggrBgEFBQcw
# AYYZaHR0cDovL29jc3AudXNlcnRydXN0LmNvbTANBgkqhkiG9w0BAQUFAAOCAQEA
# Eck94QXoO2WsyXQxA7fagzjGkrr9zfjbY5t9HpCkmMjZWGg0tfALIVOeWUb9Y4Xf
# /keqcOQ/XgiVKF8U8f0irnDkt/GwtlafsWe4aINeqGDbmDn23EleE6eQZ0vjbufr
# 8EPH0C99/5ZapwPWm1SgI9OlwqCO+U/RsgYh/iFdJ4ygr9mwUu78yO23nPHJJjjW
# pTLtSJeUXj3gPTW0sMlYr8dY/2J0FpJkQdrKqOuLA73BTq4fkTK44SQ7e+0UaAmG
# lijJO8lsKMIlafVKYa3gJ/hTp3UVsFExsPFB/z5aJh5gfuLjajmaxOruP+ayEz9V
# AwRNC5By1Ov7vIeQUbI4GTCCBP4wggPmoAMCAQICECtz23RjEUxaWzJK8jBXckkw
# DQYJKoZIhvcNAQEFBQAwejELMAkGA1UEBhMCR0IxGzAZBgNVBAgTEkdyZWF0ZXIg
# TWFuY2hlc3RlcjEQMA4GA1UEBxMHU2FsZm9yZDEaMBgGA1UEChMRQ09NT0RPIENB
# IExpbWl0ZWQxIDAeBgNVBAMTF0NPTU9ETyBUaW1lIFN0YW1waW5nIENBMB4XDTE5
# MDUwMjAwMDAwMFoXDTIwMDUzMDEwNDgzOFowgYMxCzAJBgNVBAYTAkdCMRswGQYD
# VQQIDBJHcmVhdGVyIE1hbmNoZXN0ZXIxEDAOBgNVBAcMB1NhbGZvcmQxGDAWBgNV
# BAoMD1NlY3RpZ28gTGltaXRlZDErMCkGA1UEAwwiU2VjdGlnbyBTSEEtMSBUaW1l
# IFN0YW1waW5nIFNpZ25lcjCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEB
# AL9SNoI63HQ3DXjVfxZQGWbtuhqZ0WM4mgxmskDbp4BQv2kuNi8KZz9AoUqKYzWb
# 0BnMdKoXGZErtVUgGfvAGjptyjmbgvK6laEa2DTHuIl7cIhwUWZp7Hf+xpRyes8l
# ge00gprfCBU3MbRtrhy/GNUNz/614cT7sUdx6fruY5Hs8ezOWQQRfK4LYFPqKvO+
# LwIP0ExdMRMvHmkPhZtFlsKNEotnrh+vaasGloEA35F8lRzmnhlk33bIYEQvSeOP
# f2BqRTkTRCGluvuR+kFA7U0CtEt/3+LgPbiq8xrYQYOPfzPtQQbifZ4/U4SNKNrd
# dsa9944eOM3Ay2AkA956TKsCAwEAAaOCAXQwggFwMB8GA1UdIwQYMBaAFGQihrZK
# ickED9AEWJIrs249HidsMB0GA1UdDgQWBBSu7tlgul71LAES/Y6NslVyNUZv8TAO
# BgNVHQ8BAf8EBAMCBsAwDAYDVR0TAQH/BAIwADAWBgNVHSUBAf8EDDAKBggrBgEF
# BQcDCDBABgNVHSAEOTA3MDUGDCsGAQQBsjEBAgEDCDAlMCMGCCsGAQUFBwIBFhdo
# dHRwczovL3NlY3RpZ28uY29tL0NQUzBCBgNVHR8EOzA5MDegNaAzhjFodHRwOi8v
# Y3JsLnNlY3RpZ28uY29tL0NPTU9ET1RpbWVTdGFtcGluZ0NBXzIuY3JsMHIGCCsG
# AQUFBwEBBGYwZDA9BggrBgEFBQcwAoYxaHR0cDovL2NydC5zZWN0aWdvLmNvbS9D
# T01PRE9UaW1lU3RhbXBpbmdDQV8yLmNydDAjBggrBgEFBQcwAYYXaHR0cDovL29j
# c3Auc2VjdGlnby5jb20wDQYJKoZIhvcNAQEFBQADggEBAHp/qUrSsKQcHQ2dLVzG
# rlrdj0Ud8J5ckPZerHD+09nN5BmkCkN1YGqDpMOZhCAxutb+TM8T+BD3VAl+6tzS
# LnnXB0xUt7XJnbLw8h4kFNCcx8hnqgtit7TxBuTn5CFLGTKZNLkZYXcKM5BnbMCI
# SpL1oUMB866ib8mVvZY494P3rXwoH/M4344hyHFoUy3LrqriMBeDIikYteGMietu
# +H44u5BPuV8HNBJrl9XmO5G+ABchbuJt/FJ574Elus8T0PvdK82BtleJTvDd8wtK
# NMqF/wi5ll/rERPg4cUDrVcc4V2SBr4byDw/pSCfadBpwcXCyT7nxXL36huW4pSv
# hi4wggXWMIIDvqADAgECAhAwWIJ6Yty/e/dfQyKyGOxSMA0GCSqGSIb3DQEBDAUA
# MIGIMQswCQYDVQQGEwJVUzETMBEGA1UECBMKTmV3IEplcnNleTEUMBIGA1UEBxML
# SmVyc2V5IENpdHkxHjAcBgNVBAoTFVRoZSBVU0VSVFJVU1QgTmV0d29yazEuMCwG
# A1UEAxMlVVNFUlRydXN0IFJTQSBDZXJ0aWZpY2F0aW9uIEF1dGhvcml0eTAeFw0x
# NDA3MDQwMDAwMDBaFw0yNDA3MDMyMzU5NTlaMFExCzAJBgNVBAYTAlVTMRAwDgYD
# VQQKEwdTU0wuY29tMRQwEgYDVQQLEwt3d3cuc3NsLmNvbTEaMBgGA1UEAxMRU1NM
# LmNvbSBPYmplY3QgQ0EwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQC8
# ORoKMNmUkbqdTdZ7RZvUMX8M/2wGlxt+jrcYpQrMG/NgkAjL8xb2RpxJkty5bty5
# 5DGFG0lVc7VtlMjMeXY2j5O4F32QOXRhgu04TDdDSeqc57W59wX5b6MwJSYOWnM8
# ni+mm5gpANqe4EtZSLDnQRgyoI6p4sj3qaPliMrVsoVgTV9qlkkrDj0ihzQC2ORY
# CqLYtjHo+z4swmIjgPXkNljT+lWssWQU75q/P8GiZ+7lpO219sq6eMnJ2+fhDaWU
# 5ffeDT99sfMgVoRpwTNroQKaAmavoPKQdBk2daCCm8fvA1HBs6xqNiQLLVFM8JOG
# +wFCteYGNCM0FzEw83VzAgMBAAGjggFwMIIBbDAfBgNVHSMEGDAWgBRTeb9aqitK
# z1SA4dibwJ3ysgNmyzAdBgNVHQ4EFgQUdbR35Nc3zhZnhh8XIEsSYco4lyowDgYD
# VR0PAQH/BAQDAgGGMBIGA1UdEwEB/wQIMAYBAf8CAQAwEwYDVR0lBAwwCgYIKwYB
# BQUHAwMwFwYDVR0gBBAwDjAMBgorBgEEAYKpMAEBMFUGA1UdHwROMEwwSqBIoEaG
# RGh0dHA6Ly9jcmwudHJ1c3QtcHJvdmlkZXIuY29tL1VTRVJUcnVzdFJTQUNlcnRp
# ZmljYXRpb25BdXRob3JpdHkuY3JsMIGABggrBgEFBQcBAQR0MHIwRAYIKwYBBQUH
# MAKGOGh0dHA6Ly9jcnQudHJ1c3QtcHJvdmlkZXIuY29tL1VTRVJUcnVzdFJTQUFk
# ZFRydXN0Q0EuY3J0MCoGCCsGAQUFBzABhh5odHRwOi8vb2NzcC50cnVzdC1wcm92
# aWRlci5jb20wDQYJKoZIhvcNAQEMBQADggIBAHN9ZykzZeJFhJJa0yzOhDsn9NKe
# 61Tlif3N6MiuFsUcfREhHBC7+zdPJhO/jUkdMkjteVRdCQQ136s58Ah4ukzde39b
# Iw/Yo+vJchOgUqF7vQR23CDj6i2QkmXfMykR1SIVAQTCXRV5+BLXeGGfW/Rg5lyS
# Uirf1REcrDdIU+dneXPh8OqeeN46fBkHKhkk7k8U+Pn5m36qREulHRqio8J58bHg
# TFQqxBth4bdTtHZKjkidZuFTP1CpnmC3n0N8ohAhveuYhzG2QftHVOZczc48YX8Z
# LPeIULh/G92BZ4dkq/g68h0WA5v/uuWQZVp3iCVofTmTa1fO8dldY2mGHy+JCGSi
# ZN+r94tpGJh0DYZeRFtLJbOCxNybhWZHGgS/9HrdCXFZS+e9r/JAM5yaz4lW8uWf
# WqsIZ65PdbiDxXK02pNSUWngytpE8ADe1Xc+8FoH+2ANh0i0f0l5DawcqYhqnFpy
# 5AeozvHn5XmGQo6XqDd2xu2g1+w9fhGY3/v1Tpa8fa4f7xoBEYH7isEpA/9jSQuG
# UQN9J+k+K8Gt270+FryjIhGTAs5Ek7iP1w0L/pwDiF0AIqxsgyTZsj7qPpJYaF87
# fQPGFZkgI9jq1EvQvNeMIYC3wiQjpZ8ufzISslr8FNnHEWOmP6Yn75DPaMR87/Gz
# NR05G+QUdZIH51LKMIIF3jCCA8agAwIBAgIQAf1tMPyjylGoG7xkDjUDLTANBgkq
# hkiG9w0BAQwFADCBiDELMAkGA1UEBhMCVVMxEzARBgNVBAgTCk5ldyBKZXJzZXkx
# FDASBgNVBAcTC0plcnNleSBDaXR5MR4wHAYDVQQKExVUaGUgVVNFUlRSVVNUIE5l
# dHdvcmsxLjAsBgNVBAMTJVVTRVJUcnVzdCBSU0EgQ2VydGlmaWNhdGlvbiBBdXRo
# b3JpdHkwHhcNMTAwMjAxMDAwMDAwWhcNMzgwMTE4MjM1OTU5WjCBiDELMAkGA1UE
# BhMCVVMxEzARBgNVBAgTCk5ldyBKZXJzZXkxFDASBgNVBAcTC0plcnNleSBDaXR5
# MR4wHAYDVQQKExVUaGUgVVNFUlRSVVNUIE5ldHdvcmsxLjAsBgNVBAMTJVVTRVJU
# cnVzdCBSU0EgQ2VydGlmaWNhdGlvbiBBdXRob3JpdHkwggIiMA0GCSqGSIb3DQEB
# AQUAA4ICDwAwggIKAoICAQCAEmUXNg7D2wiz0KxXDXbtzSfTTK1Qg2HiqiBNCS1k
# CdzOiZ/MPans9s/B3PHTsdZ7NygRK0faOca8Ohm0X6a9fZ2jY0K2dvKpOyuR+OJv
# 0OwWIJAJPuLodMkYtJHUYmTbf6MG8YgYapAiPLz+E/CHFHv25B+O1ORRxhFnRghR
# y4YUVD+8M/5+bJz/Fp0YvVGONaanZshyZ9shZrHUm3gDwFA66Mzw3LyeTP6vBZY1
# H1dat//O+T23LLb2VN3I5xI6Ta5MirdcmrS3ID3KfyI0rn47aGYBROcBTkZTmzNg
# 95S+UzeQc0PzMsNT79uq/nROacdrjGCT3sTHDN/hMq7MkztReJVni+49Vv4M0GkP
# Gw/zJSZrM233bkf6c0Plfg6lZrEpfDKEY1WJxA3Bk1QwGROs0303p+tdOmw1XNtB
# 1xLaqUkL39iAigmTYo61Zs8liM2EuLE/pDkP2QKe6xJMlXzzawWpXhaDzLhn4ugT
# ncxbgtNMs+1b/97lc6wjOy0AvzVVdAlJ2ElYGn+SNuZRkg7zJn0cTRe8yexDJtC/
# QV9AqURE9JnnV4eeUB9XVKg+/XRjL7FQZQnmWEIuQxpMtPAlR1n6BB6T1CZGSlCB
# st6+eLf8ZxXhyVeEHg9j1uliutZfVS7qXMYoCAQlObgOK6nyTJccBz8NUvXt7y+C
# DwIDAQABo0IwQDAdBgNVHQ4EFgQUU3m/WqorSs9UgOHYm8Cd8rIDZsswDgYDVR0P
# AQH/BAQDAgEGMA8GA1UdEwEB/wQFMAMBAf8wDQYJKoZIhvcNAQEMBQADggIBAFzU
# fA3P9wF9QZllDHPFUp/L+M+ZBn8b2kMVn54CVVeWFPFSPCeHlCjtHzoBN6J2/FNQ
# wISbxmtOuowhT6KOVWKR82kV2LyI48SqC/3vqOlLVSoGIG1VeCkZ7l8wXEskEVX/
# JJpuXior7gtNn3/3ATiUFJVDBwn7YKnuHKsSjKCaXqeYalltiz8I+8jRRa8YFWSQ
# Eg9zKC7F4iRO/Fjs8PRF/iKz6y+O0tlFYQXBl2+odnKPi4w2r78NBc5xjeambx9s
# pnFixdjQg3IM8WcRiQycE0xyNN+81XHfqnHd4blsjDwSXWXavVcStkNr/+XeTWYR
# Uc+ZruwXtuhxkYzeSf7dNXGiFSeUHM9h4ya7b6NnJSFd5t0dCy5oGzuCr+yDZ4XU
# mFF0sbmZgIn/f3gZXHlKYC6SQK5MNyosycdiyA5d9zZbyuAlJQG03RoHnHcAP9Dc
# 1ew91Pq7P8yF1m9/qS3fuQL39ZeatTXaw2ewh0qpKJ4jjv9cJ2vhsE/zB+4ALtRZ
# h8tSQZXq9EfX7mRBVXyNWQKV3WKdwrnuWih0hKWbt5DHDAff9Yk2dDLWKMGwsAvg
# nEzDHNb842m1R0aBL6KCq9NjRHDEjf8tM7qtj3u1cIiuPhnPQCjY/MiQu12ZIvVS
# 5ljFH4gxQ+6IHdfGjjxDah2nGN59PRbxYvnKkKj9MYIEMzCCBC8CAQEwZjBRMQsw
# CQYDVQQGEwJVUzEQMA4GA1UEChMHU1NMLmNvbTEUMBIGA1UECxMLd3d3LnNzbC5j
# b20xGjAYBgNVBAMTEVNTTC5jb20gT2JqZWN0IENBAhEAoW8/qfSF4+FmAEEQ9HZ4
# FjAJBgUrDgMCGgUAoHgwGAYKKwYBBAGCNwIBDDEKMAigAoAAoQKAADAZBgkqhkiG
# 9w0BCQMxDAYKKwYBBAGCNwIBBDAcBgorBgEEAYI3AgELMQ4wDAYKKwYBBAGCNwIB
# FTAjBgkqhkiG9w0BCQQxFgQUV3iIRDW0cLQ4wMedERJrH5iAfUYwDQYJKoZIhvcN
# AQEBBQAEggEAju9YoY6j6H/DYAtsHBm7I9WysjhxjbT4k/5OeNR9KJbuRGTdQr08
# 6iAfdIt3LsouM5bFB5oEw93Bvl/gDLZSLZMJyykuTlVb2dxotSfYkiVHh28Ro969
# jENgBeHivIQKn9qNOG37soGjDYz8Kkq26Rad7WzIdasvy120Uq3fUh3SVgXbeiV9
# yM9U0arN72cRs2t9cZtGeuYcATY5rE5c4BCTKkt7R00ouVkbLe2lLu2V1kqUg7Jb
# P/RwKDH/FrCXVI5Eg2C++Wn7FeKSlugHlNUtQMy3YoJJ8jFIJb/+Ry9bPBfqzQgk
# Vw5uwpb27v4GK6lIrBeMGf0gMl9tBt6V9qGCAigwggIkBgkqhkiG9w0BCQYxggIV
# MIICEQIBATCBjjB6MQswCQYDVQQGEwJHQjEbMBkGA1UECBMSR3JlYXRlciBNYW5j
# aGVzdGVyMRAwDgYDVQQHEwdTYWxmb3JkMRowGAYDVQQKExFDT01PRE8gQ0EgTGlt
# aXRlZDEgMB4GA1UEAxMXQ09NT0RPIFRpbWUgU3RhbXBpbmcgQ0ECECtz23RjEUxa
# WzJK8jBXckkwCQYFKw4DAhoFAKBdMBgGCSqGSIb3DQEJAzELBgkqhkiG9w0BBwEw
# HAYJKoZIhvcNAQkFMQ8XDTE5MTEwNDIwMDYzNFowIwYJKoZIhvcNAQkEMRYEFMXb
# 9ISzUxNVUyiTzdkSDp9ISEYyMA0GCSqGSIb3DQEBAQUABIIBALLOIMJsXytMkAFA
# iRWK6+lXiozdrPcVUwDWGJu4E76mKbJtJLE1hbvFof5SwS3J+aOw6U2Qr+7O3wvg
# WvrPqfjyUDJYL+FXWkeUk10RQ+Beu+1GLFyCUjIbSGtTHI4kERynXGqLc1b7uhgT
# TVI9DP7LyzVCiY+zdH5Lr9z2dEDjftnluzS8ukdM/29cUk1PXb8wUuMUbCs4RnvV
# 1DQJy8mbiSO1CBkGLQ8UUWOpePqP2dCRvsPLcoQJdyAp2BIxT6qXvy2rpvSljWM6
# x0u2IrARrju9R92M/KUPxE0ka7C2MX47H1Fbk4DZdWZqFFw1fYg/rvqfEPeHBScd
# 6SlJ/fs=
# SIG # End signature block
