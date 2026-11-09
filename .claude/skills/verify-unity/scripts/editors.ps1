Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" |
  Where-Object { $_.CommandLine -match '-projectpath' -and $_.CommandLine -notmatch 'AssetImportWorker' } |
  ForEach-Object {
    if ($_.CommandLine -match '-projectpath\s+("([^"]+)"|(\S+))') {
      if ($Matches[2]) { $Matches[2] } else { $Matches[3] }
    }
  }
