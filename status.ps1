# Function to count video files and calculate folder size
function Count-Files-And-Size {
    param (
        [string]$folder
    )

    $videoCount = 0
    $folderSize = 0

    Get-ChildItem -Path $folder -File -Recurse | ForEach-Object {
        if ($_.Extension -match '\.(mp4|mkv|avi|mov)$') {
            $videoCount++
            $folderSize += $_.length
        }
    }

    # Convert folder size to GB
    $folderSizeGB = "{0:N2}" -f ($folderSize / 1GB)

    # Get the relative path from the script location
    $relativePath = (Get-Item -Path $folder).FullName.Replace((Get-Item -Path $PSScriptRoot).FullName, "").TrimStart("\")
    
    $date = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $result = "$relativePath | $videoCount | $folderSizeGB GB | $date"
    Write-Output $result
    $result
}

# Main script
$outputFile = "history.csv"

# Check if history.csv exists
if (Test-Path $outputFile) {
    # Iterate through folders, count files, and calculate size
    $results = Get-ChildItem -Path "." -Directory -Recurse | ForEach-Object {
        Count-Files-And-Size $_.FullName
    }

    # Output results in the shell
    $results | ForEach-Object {
        Write-Output $_
    }

    # Append only the data to existing history.csv
    $results | ForEach-Object {
        $_ | Out-File -FilePath $outputFile -Encoding utf8 -Append
    }
}
else {
    # Create new history.csv with header and date column
    "folder | number of videofiles | size of folder | date" | Out-File -FilePath $outputFile -Encoding utf8
}
