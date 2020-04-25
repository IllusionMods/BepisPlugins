if ($PSScriptRoot -match '.+?\\bin\\?') {
    $dir = $PSScriptRoot + "\"
}
else {
    $dir = $PSScriptRoot + "\bin\"
}

$array = @("KK", "EC", "AI", "HS", "PH")

$copy = $dir + "\copy\BepInEx" 

New-Item -ItemType Directory -Force -Path ($dir + "\out")

foreach ($element in $array) 
{
    try
    {
        Remove-Item -Force -Path ($dir + "\copy") -Recurse -ErrorAction SilentlyContinue
        New-Item -ItemType Directory -Force -Path ($copy + "\plugins")

        Copy-Item -Path ($dir + "\BepInEx\plugins\" + $element + "_BepisPlugins") -Destination ($copy + "\plugins\" + $element + "_BepisPlugins") -Recurse -Force 

        $ver = "r" + (Get-ChildItem -Path ($copy) -Filter "*.dll" -Recurse -Force)[0].VersionInfo.FileVersion.ToString()

        Compress-Archive -Path $copy -Force -CompressionLevel "Optimal" -DestinationPath ($dir + "out\" + $element + "_BepisPlugins_" + $ver + ".zip")
    }
    catch 
    {
        # retry
        Remove-Item -Force -Path ($dir + "\copy") -Recurse -ErrorAction SilentlyContinue
        New-Item -ItemType Directory -Force -Path ($copy + "\plugins")

        Copy-Item -Path ($dir + "\BepInEx\plugins\" + $element + "_BepisPlugins") -Destination ($copy + "\plugins\" + $element + "_BepisPlugins") -Recurse -Force 

        $ver = "r" + (Get-ChildItem -Path ($copy) -Filter "*.dll" -Recurse -Force)[0].VersionInfo.FileVersion.ToString()

        Compress-Archive -Path $copy -Force -CompressionLevel "Optimal" -DestinationPath ($dir + "out\" + $element + "_BepisPlugins_" + $ver + ".zip")
    }
}

Remove-Item -Force -Path ($dir + "\copy") -Recurse