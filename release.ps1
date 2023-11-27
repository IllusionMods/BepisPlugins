$array = @("KK", "EC", "AI", "HS", "HS2", "PH", "KKS", "HC")

if ($PSScriptRoot -match '.+?\\bin\\?') {
    $dir = $PSScriptRoot + "\"
}
else {
    $dir = $PSScriptRoot + "\bin\"
}

$copy = $dir + "\copy\BepInEx" 
Remove-Item -Force -Path ($copy) -Recurse -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path ($copy + "\plugins")

New-Item -ItemType Directory -Force -Path ($dir + "\out")

function CreateZip ($element)
{
    Remove-Item -Force -Path ($copy) -Recurse
    New-Item -ItemType Directory -Force -Path ($copy + "\plugins")

    Copy-Item -Path ($dir + "\BepInEx\plugins\" + $element + "_BepisPlugins") -Destination ($copy + "\plugins\" + $element + "_BepisPlugins") -Recurse -Force 
    
    Copy-Item -Path ($dir + "\..\README.md") -Destination ($copy + "\plugins\" + $element + "_BepisPlugins") -Recurse -Force
    Copy-Item -Path ($dir + "\..\LICENSE") -Destination ($copy + "\plugins\" + $element + "_BepisPlugins") -Recurse -Force

    try
    {
        $ver = "r" + (Get-ChildItem -Path ($copy + "\plugins") -Filter ($element + "_*.dll") -Recurse -Force)[0].VersionInfo.FileVersion.ToString() -replace "([\d+\.]+?\d+)[\.0]*$", '${1}'
    }
    catch 
    {
        $ver = "r" + (Get-ChildItem -Path ($copy) -Filter ("*.dll") -Recurse -Force)[0].VersionInfo.FileVersion.ToString()
    }
    
    & robocopy ($dir + "\BepInEx\patchers\") ($copy + "\patchers") ($element + "_*.*") /R:5 /W:5 

    Compress-Archive -Path $copy -Force -CompressionLevel "Optimal" -DestinationPath ($dir + "out\" + $element + "_BepisPlugins_" + $ver + ".zip")
}

foreach ($element in $array) 
{
    try
    {
        CreateZip ($element)
    }
    catch 
    {
        # retry
        CreateZip ($element)
    }
}

Remove-Item -Force -Path ($dir + "\copy") -Recurse