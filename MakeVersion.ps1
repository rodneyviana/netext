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

