$temp = "temp/"
$url = "https://ci.appveyor.com/api/projects/pipe01/lcu-api-generator/artifacts/LCU%20API%20Generator%2Fbin%2FDebug%2Fnetcoreapp2.1%2Fwin-x86%2FLCU%20API%20Generator%20full%20package.zip"
$output = $temp + "package.zip"
$exe = $temp + "LCU API Generator.exe"

Add-Type -AssemblyName System.IO.Compression.FileSystem
function Unzip
{
    param([string]$zipfile, [string]$outpath)

    [System.IO.Compression.ZipFile]::ExtractToDirectory($zipfile, $outpath)
}

if (-not (Test-Path $temp))
{
    echo "Creating temp folder"
    md -Path $temp
}

if (-not (Test-Path $output))
{
    echo "Downloading generator package"
    [Environment]::CurrentDirectory = (Get-Location -PSProvider FileSystem).ProviderPath
    try {
        (New-Object System.Net.WebClient).DownloadFile($url, $output)
    }
    catch {
        echo $_.Exception.ToString()
        exit
    }
}

if (-not (Test-Path $exe))
{
    echo "Extracting package"
    Unzip $output $temp

    rm $output
}

$choice = Read-Host "Load swagger from LoL client? (make sure Swagger is enabled) (y/N)"

$swagger = $false

if ($choice -eq "y")
{
    & $exe -cli -client
}
else
{
    & $exe -cli
}