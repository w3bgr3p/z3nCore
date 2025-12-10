$ilrepack = "$env:USERPROFILE\.nuget\packages\ilrepack\2.0.44\tools\ILRepack.exe"
$outDir = "bin\Release"

# Полный путь к bin\Release
$fullOutDir = Join-Path $PSScriptRoot $outDir

# Собираем все DLL кроме исключений (и без Newtonsoft.Json/NBitcoin - добавим отдельно)
$allDlls = Get-ChildItem "$outDir\*.dll" | 
    Where-Object { 
        $_.Name -notmatch '^(z3nCore|CapMonsterCloud|Global|ZennoLab|Newtonsoft|NBitcoin)' 
    } | 
    ForEach-Object { "`"$($_.FullName)`"" }

# Newtonsoft.Json первым, потом NBitcoin
$newtonsoft = "`"$fullOutDir\Newtonsoft.Json.dll`""
$nbitcoin = "`"$fullOutDir\NBitcoin.dll`""

# Формируем команду с /lib для поиска зависимостей
$cmd = "& `"$ilrepack`" /out:`"$fullOutDir\z3nCore.Merged.dll`" /lib:`"$fullOutDir`" /internalize /union `"$fullOutDir\z3nCore.dll`" $newtonsoft $nbitcoin $($allDlls -join ' ')"

Write-Host "Merging assemblies..." -ForegroundColor Green
Invoke-Expression $cmd

if ($LASTEXITCODE -eq 0) {
    Write-Host "[SUCCESS] Created z3nCore.Merged.dll" -ForegroundColor Green
    exit 0
} else {
    Write-Host "[ERROR] Failed with exit code $LASTEXITCODE" -ForegroundColor Red
    exit 1
}