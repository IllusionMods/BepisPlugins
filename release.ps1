if ($PSScriptRoot -match '.+?\\bin\\?') {
    $dir = $PSScriptRoot + "\"
}
else {
    $dir = $PSScriptRoot + "\bin\"
}

$copy = $dir + "\copy\BepInEx" 
Remove-Item -Force -Path ($copy) -Recurse -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path ($copy + "\plugins") | Out-Null

New-Item -ItemType Directory -Force -Path ($dir + "\out") | Out-Null

function CreateZip ($element)
{
    Write-Output ("Processing " + $element)

    Remove-Item -Force -Path ($copy) -Recurse
    New-Item -ItemType Directory -Force -Path ($copy + "\plugins") | Out-Null

    Copy-Item -Path ($dir + "\BepInEx\plugins\" + $element + "_BepisPlugins") -Destination ($copy + "\plugins\" + $element + "_BepisPlugins") -Recurse -Force  | Out-Null
    
    Copy-Item -Path ($dir + "\..\README.md") -Destination ($copy + "\plugins\" + $element + "_BepisPlugins") -Recurse -Force | Out-Null
    Copy-Item -Path ($dir + "\..\LICENSE") -Destination ($copy + "\plugins\" + $element + "_BepisPlugins") -Recurse -Force | Out-Null

    try
    {
        $pluginFiles = (Get-ChildItem -Path ($copy + "\plugins") -Filter ($element + "_*.dll") -Recurse -Force)
        if($pluginFiles.Length.Equals(0))
        {
            $pluginFiles = (Get-ChildItem -Path ($copy + "\plugins") -Filter ("*Sideloader.dll") -Recurse -Force)
        }
        $pluginFile = $pluginFiles[0]

        $ver = "r" + $pluginFile.VersionInfo.FileVersion.ToString() -replace "([\d+\.]+?\d+)[\.0]*$", '${1}'
        if(!$ver.Contains("."))
        {
            $ver = $ver + ".0"
        }
    }
    catch 
    {   
        Write-Warning ("Failed to extract version number - " + $_)
        $pluginFile = (Get-ChildItem -Path ($copy) -Filter ("*.dll") -Recurse -Force)[0]
        $ver = "r" + $pluginFile.VersionInfo.FileVersion.ToString()
    }

    Write-Output ("Version " + $ver + " extracted from " + $pluginFile)
    
    & robocopy ($dir + "\BepInEx\patchers\") ($copy + "\patchers") ($element + "_*.*") /R:5 /W:5 /nfl /ndl /njh /njs /ns /nc /np

    Compress-Archive -Path $copy -Force -CompressionLevel "Optimal" -DestinationPath ($dir + "out\" + $element + "_BepisPlugins_" + $ver + ".zip")
}

$array = Get-ChildItem -Path ($dir + "\BepInEx\plugins\") -Filter "*_BepisPlugins"

foreach ($element in $array) 
{
    $element = $element.Name.Substring(0, $element.Name.IndexOf("_"))

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