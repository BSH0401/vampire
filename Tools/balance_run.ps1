<#
.SYNOPSIS
  Core Overclock 자동 밸런스 테스트 (로컬 전용).

.DESCRIPTION
  빌드를 화면 없이(-batchmode) 여러 판 자동 플레이하고, 판마다 게임이 남긴 JSON 리포트(-report)를 모아
  runs.csv / summary.json / summary.md 를 만든다.
  원본 로그(Player.log 등)는 PC 경로·하드웨어 정보가 들어 있으므로 결과 폴더의 logs\ 에만 두고,
  -WikiRaw 를 주면 수치 파일(리포트·CSV·요약)만 그쪽으로 복사한다.

.EXAMPLE
  .\Tools\balance_run.ps1 -Runs 3
  .\Tools\balance_run.ps1 -Cores Furnace,Glass -Runs 5 -Meta 2 -Label meta2
  .\Tools\balance_run.ps1 -Runs 3 -WikiRaw "D:\Vault\위키\raw"
#>
param(
    [string]$Exe = (Join-Path $PSScriptRoot "..\CoreOverclock\Builds\Windows\CoreOverclock.exe"),
    [string[]]$Cores = @("Standard", "Furnace", "Collector", "Glacier", "Twin", "Glass"),
    [int]$Runs = 3,
    [double]$TimeScale = 6,
    [int]$Parallel = 3,
    [int]$Meta = -1,            # 0 이상이면 모든 영구 강화를 그 단계로 가정 (-meta N)
    [switch]$God,               # 무적 (사망 없이 끝까지 진행, 피해량만 기록)
    [switch]$NoAutobuy,
    [switch]$Windowed,          # 창을 띄워서 실행 (기본은 화면 없이 -batchmode)
    [int]$TimeoutSec = 900,     # 한 판 최대 실제 시간
    [string]$OutRoot = (Join-Path $env:LOCALAPPDATA "CoreOverclock\balance"),
    [string]$WikiRaw = "",
    [string]$Label = "",
    [string]$Reaggregate = ""   # 이미 돌린 결과 폴더를 다시 집계만 (실행 생략)
)

$ErrorActionPreference = "Stop"
$Exe = [IO.Path]::GetFullPath($Exe)
if (-not (Test-Path $Exe)) { throw "빌드를 찾을 수 없습니다: $Exe" }

$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$name = if ($Label) { "${stamp}_$Label" } else { $stamp }
$outDir = Join-Path $OutRoot $name
if ($Reaggregate) { $outDir = [IO.Path]::GetFullPath($Reaggregate); $name = Split-Path $outDir -Leaf }
$reportDir = Join-Path $outDir "reports"
$logDir = Join-Path $outDir "logs"
New-Item -ItemType Directory -Force $reportDir, $logDir | Out-Null
$started = Get-Date
if (-not $Reaggregate) {

# ─────────────── 실행 ───────────────
$jobs = foreach ($core in $Cores) { for ($i = 1; $i -le $Runs; $i++) { [pscustomobject]@{ Core = $core; Index = $i } } }
$total = @($jobs).Count
Write-Host "밸런스 테스트: 코어 $($Cores.Count)종 x ${Runs}판 = ${total}판, 배속 x$TimeScale, 동시 ${Parallel}개"
Write-Host "결과 폴더: $outDir"

$running = New-Object System.Collections.ArrayList
$queue = New-Object System.Collections.Queue
$jobs | ForEach-Object { $queue.Enqueue($_) }
$done = 0

function Start-Run($job) {
    $log = Join-Path $logDir ("{0}_{1}.log" -f $job.Core, $job.Index)
    $argv = @("-autopilot", "-timescale", $TimeScale, "-core", $job.Core, "-report", "`"$reportDir`"",
              "-quitonend", "-quitafter", $TimeoutSec, "-logFile", "`"$log`"")
    if (-not $NoAutobuy) { $argv += "-autobuy" }
    if ($God) { $argv += "-god" }
    if ($Meta -ge 0) { $argv += @("-meta", $Meta) }
    if ($Windowed) { $argv += @("-screen-fullscreen", "0", "-screen-width", "960", "-screen-height", "540") }
    else { $argv += @("-batchmode", "-nographics") }
    $p = Start-Process -FilePath $Exe -ArgumentList $argv -PassThru -WindowStyle Minimized
    [pscustomobject]@{ Job = $job; Process = $p; Start = Get-Date }
}

while ($queue.Count -gt 0 -or $running.Count -gt 0) {
    while ($queue.Count -gt 0 -and $running.Count -lt $Parallel) { [void]$running.Add((Start-Run $queue.Dequeue())) }
    Start-Sleep -Milliseconds 500
    foreach ($r in @($running)) {
        if ($r.Process.HasExited) {
            $running.Remove($r); $done++
            Write-Host ("  [{0}/{1}] {2} #{3} 완료 ({4:N0}초)" -f $done, $total, $r.Job.Core, $r.Job.Index, ((Get-Date) - $r.Start).TotalSeconds)
        }
        elseif (((Get-Date) - $r.Start).TotalSeconds -gt $TimeoutSec + 60) {
            $r.Process.Kill(); $running.Remove($r); $done++
            Write-Warning "$($r.Job.Core) #$($r.Job.Index) 시간 초과로 종료"
        }
    }
}

# 테스트 실행이 남긴 설정(창 크기 등) 정리
Remove-Item -Path "HKCU:\Software\CoreOverclock\Core Overclock" -Recurse -ErrorAction SilentlyContinue
}

# ─────────────── 집계 ───────────────
$reports = @(Get-ChildItem $reportDir -Filter *.json | ForEach-Object { Get-Content $_.FullName -Raw -Encoding UTF8 | ConvertFrom-Json })
if ($reports.Count -eq 0) { throw "리포트가 하나도 없습니다. logs 폴더를 확인하세요: $logDir" }

function Avg($values) { $v = @($values); if ($v.Count -eq 0) { return 0 } return [math]::Round(($v | Measure-Object -Average).Average, 2) }

$rows = foreach ($r in $reports) {
    $dmg = @($r.waves | ForEach-Object { $_.damageTaken })
    [pscustomobject]@{
        core = $r.core; result = $r.result; waveReached = $r.waveReached; totalWaves = $r.totalWaves
        kills = $r.totalKills; bossKills = $r.bossKills; fragments = $r.fragments; finalScrap = $r.finalScrap
        merges = $r.merges; upgrades = $r.upgrades; marketBuys = $r.marketBuys; meltdowns = $r.meltdowns
        avgDamagePerWave = Avg $dmg
        weapons = (($r.finalWeapons | ForEach-Object { "$($_.id):$($_.grade)" }) -join ";")
        fusions = ($r.fusions -join ";"); chips = $r.chips.Count
        version = $r.version; build = $r.build; god = $r.god; meta = $r.metaLevel; seed = $r.seed
        realSeconds = [math]::Round($r.realSeconds, 1)
    }
}
$rows | Sort-Object core, waveReached | Export-Csv (Join-Path $outDir "runs.csv") -NoTypeInformation -Encoding UTF8

$byCore = foreach ($g in ($rows | Group-Object core)) {
    $w = @($g.Group | ForEach-Object { [int]$_.waveReached })
    [pscustomobject]@{
        core = $g.Name; runs = $g.Count
        victories = @($g.Group | Where-Object result -eq "victory").Count
        avgWave = Avg $w; minWave = ($w | Measure-Object -Minimum).Minimum; maxWave = ($w | Measure-Object -Maximum).Maximum
        avgKills = Avg ($g.Group | ForEach-Object { $_.kills })
        avgDamagePerWave = Avg ($g.Group | ForEach-Object { $_.avgDamagePerWave })
        avgMeltdowns = Avg ($g.Group | ForEach-Object { $_.meltdowns })
        avgMerges = Avg ($g.Group | ForEach-Object { $_.merges })
        avgFragments = Avg ($g.Group | ForEach-Object { $_.fragments })
    }
}

$weaponStats = @{}
foreach ($r in $reports) {
    foreach ($id in ($r.finalWeapons | ForEach-Object { $_.id } | Select-Object -Unique)) {
        if (-not $weaponStats[$id]) { $weaponStats[$id] = New-Object System.Collections.ArrayList }
        [void]$weaponStats[$id].Add([int]$r.waveReached)
    }
}
$byWeapon = foreach ($k in $weaponStats.Keys) {
    [pscustomobject]@{ weapon = $k; runs = $weaponStats[$k].Count; avgWave = Avg $weaponStats[$k] }
}
$byWeapon = @($byWeapon | Sort-Object avgWave -Descending)

$fusionCounts = @($reports | ForEach-Object { $_.fusions } | Where-Object { $_ } | Group-Object |
    ForEach-Object { [pscustomobject]@{ fusion = $_.Name; runs = $_.Count } } | Sort-Object runs -Descending)
$deathWaves = @($rows | Where-Object result -eq "death" | Group-Object waveReached |
    ForEach-Object { [pscustomobject]@{ wave = [int]$_.Name; deaths = $_.Count } } | Sort-Object wave)

$info = [pscustomobject]@{
    label = $Label; date = (Get-Date -Format "yyyy-MM-dd HH:mm"); version = $reports[0].version; build = $reports[0].build
    runs = $reports.Count; timeScale = $TimeScale; god = [bool]$God; autobuy = -not $NoAutobuy; meta = $Meta
    minutes = [math]::Round(((Get-Date) - $started).TotalMinutes, 1)
}
[pscustomobject]@{ meta = $info; byCore = $byCore; byWeapon = $byWeapon; fusions = $fusionCounts; deathWaves = $deathWaves } |
    ConvertTo-Json -Depth 6 | Out-File (Join-Path $outDir "summary.json") -Encoding UTF8

# 사람이 읽는 요약 (위키 raw 용)
$md = New-Object System.Text.StringBuilder
[void]$md.AppendLine("# 밸런스 테스트 $name")
[void]$md.AppendLine("")
[void]$md.AppendLine("- 버전 $($info.version) ($($info.build)), $($info.runs)판, 배속 x$TimeScale, 무적 $([bool]$God), 자동 구매 $(-not $NoAutobuy), 영구 강화 $(if ($Meta -ge 0) { "${Meta}단계" } else { '없음' })")
[void]$md.AppendLine("")
[void]$md.AppendLine("| 코어 | 판 | 클리어 | 평균 웨이브 | 최저~최고 | 평균 처치 | 웨이브당 피해 | 멜트다운 | 합치기 |")
[void]$md.AppendLine("|---|---|---|---|---|---|---|---|---|")
foreach ($c in $byCore) { [void]$md.AppendLine("| $($c.core) | $($c.runs) | $($c.victories) | $($c.avgWave) | $($c.minWave)~$($c.maxWave) | $($c.avgKills) | $($c.avgDamagePerWave) | $($c.avgMeltdowns) | $($c.avgMerges) |") }
[void]$md.AppendLine("")
[void]$md.AppendLine("| 무기 (최종 보유) | 판 | 평균 도달 웨이브 |")
[void]$md.AppendLine("|---|---|---|")
foreach ($w in $byWeapon) { [void]$md.AppendLine("| $($w.weapon) | $($w.runs) | $($w.avgWave) |") }
[void]$md.AppendLine("")
[void]$md.AppendLine("- 퓨전 발동: " + (($fusionCounts | ForEach-Object { "$($_.fusion) $($_.runs)" }) -join ", "))
[void]$md.AppendLine("- 사망 웨이브: " + (($deathWaves | ForEach-Object { "W$($_.wave) x$($_.deaths)" }) -join ", "))
$md.ToString() | Out-File (Join-Path $outDir "summary.md") -Encoding UTF8

# ─────────────── 위키로 복사 (수치 파일만) ───────────────
if ($WikiRaw) {
    $dest = Join-Path $WikiRaw $name
    New-Item -ItemType Directory -Force (Join-Path $dest "reports") | Out-Null
    Copy-Item (Join-Path $outDir "runs.csv"), (Join-Path $outDir "summary.json"), (Join-Path $outDir "summary.md") $dest
    Copy-Item (Join-Path $reportDir "*.json") (Join-Path $dest "reports")
    Write-Host "위키 raw로 복사 (로그 제외): $dest"
}

Write-Host ""
$byCore | Format-Table core, runs, victories, avgWave, minWave, maxWave, avgDamagePerWave, avgMeltdowns -AutoSize
Write-Host ("완료: {0}판, {1:N1}분. 요약: {2}" -f $reports.Count, $info.minutes, (Join-Path $outDir "summary.md"))
