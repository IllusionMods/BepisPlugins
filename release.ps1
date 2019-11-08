if ($PSScriptRoot -match '.+?\\bin\\?') {
    $dir = $PSScriptRoot + "\"
}
else {
    $dir = $PSScriptRoot + "\bin\"
}

$array = @("KK", "EC", "AI", "HS", "PH")


$copy = $dir + "\copy\BepInEx" 

$ver = "r" + (Get-ChildItem -Path ($dir + "\BepInEx\") -Filter "*.dll" -Recurse -Force)[0].VersionInfo.FileVersion.ToString()


New-Item -ItemType Directory -Force -Path ($dir + "\out")  

foreach ($element in $array) {

Remove-Item -Force -Path ($dir + "\copy") -Recurse -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path ($copy + "\plugins")

Copy-Item -Path ($dir + "\BepInEx\plugins\" + $element + "_BepisPlugins") -Destination ($copy + "\plugins\" + $element + "_BepisPlugins") -Recurse -Force 


Compress-Archive -Path $copy -Force -CompressionLevel "Optimal" -DestinationPath ($dir + "out\" + $element + "_BepisPlugins_" + $ver + ".zip")

}

Remove-Item -Force -Path ($dir + "\copy") -Recurse