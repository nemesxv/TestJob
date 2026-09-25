$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
docker compose up -d --build
if ($LASTEXITCODE -ne 0) { throw 'Не удалось запустить Compose.' }
$ready = $false
for ($attempt = 0; $attempt -lt 120; $attempt++) {
    try {
        Invoke-WebRequest http://localhost:8090/swagger/v1/swagger.json | Out-Null
        $ready = $true
        break
    } catch { Start-Sleep -Seconds 2 }
}
if (-not $ready) { throw 'API не запустился вовремя. Проверьте вывод docker compose logs api.' }
$before = docker compose exec -T db psql -U testjob -d testjob -Atc 'SELECT COUNT(*) FROM elements'
if ($LASTEXITCODE -ne 0) { throw 'Не удалось выполнить запрос к базе данных.' }
$added = 0
foreach ($number in 1, 2) {
    $response = Invoke-WebRequest -Uri http://localhost:8090/api/process-page -Method Post -ContentType 'application/json; charset=utf-8' -InFile "json_payload_$number.txt"
    $result = $response.Content | ConvertFrom-Json
    if ($result.is_error -ne 0) { throw $result.error_message }
    $expected = if ($number -eq 1) { 238 } else { 9 }
    if ($result.elements_count -ne $expected) { throw 'Получено неожиданное количество элементов.' }
    [IO.File]::WriteAllText((Join-Path $PWD "json_result_$number.txt"), $response.Content, [Text.UTF8Encoding]::new($false))
    $added += $result.elements_count
}
$after = docker compose exec -T db psql -U testjob -d testjob -Atc 'SELECT COUNT(*) FROM elements'
if ([int]$after -ne ([int]$before + $added)) { throw 'Количество сохранённых строк не совпало с ожидаемым.' }
docker compose restart db
if ($LASTEXITCODE -ne 0) { throw 'Не удалось перезапустить базу данных.' }
for ($attempt = 0; $attempt -lt 30; $attempt++) {
    docker compose exec -T db pg_isready -U testjob -d testjob *> $null
    if ($LASTEXITCODE -eq 0) { break }
    Start-Sleep -Seconds 2
}
$persisted = docker compose exec -T db psql -U testjob -d testjob -Atc 'SELECT COUNT(*) FROM elements'
if ($LASTEXITCODE -ne 0 -or [int]$persisted -ne [int]$after) { throw 'Проверка сохранности данных завершилась ошибкой.' }
Invoke-WebRequest http://localhost:8080/ | Out-Null
Write-Output "Проверено новых строк: $added. Подтверждены сохранность данных после перезапуска и доступность API и pgAdmin по HTTP. Откройте pgAdmin и убедитесь, что база доступна без ручного ввода пароля."
