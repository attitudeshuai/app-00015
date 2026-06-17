$baseUrl = "http://localhost:8085"
$token = $null
$newCropId = $null
$newTaskId = $null
$newHarvestId = $null
$newPestId = $null

function Write-TestResult {
    param(
        [string]$TestName,
        [int]$ExpectedCode,
        [int]$ActualCode,
        [string]$Content
    )
    
    $body = $null
    if ($Content) {
        try {
            $body = $Content | ConvertFrom-Json -ErrorAction SilentlyContinue
        } catch {
            $body = $null
        }
    }
    
    $codeMatch = $true
    if ($body) {
        if ($body.code -and $body.code -ne 0 -and $body.code -ne 200 -and $body.code -ne $ExpectedCode) {
            $codeMatch = $false
        }
    }
    
    if ($ActualCode -eq $ExpectedCode -and $codeMatch) {
        Write-Host "PASS: $TestName" -ForegroundColor Green
    } else {
        Write-Host "FAIL: $TestName (Expected: $ExpectedCode, Got: $ActualCode)" -ForegroundColor Red
        if ($body) {
            Write-Host "  Code: $($body.code), Message: $($body.message)" -ForegroundColor Gray
        }
        if ($Content) {
            $preview = $Content
            if ($preview.Length -gt 150) { $preview = $preview.Substring(0, 147) + "..." }
            Write-Host "  $preview" -ForegroundColor Gray
        }
    }
    Write-Host ""
    return $body
}

function Invoke-CurlTest {
    param(
        [string]$Method,
        [string]$Endpoint,
        [string]$BodyFile = $null,
        [int]$ExpectedCode = 200,
        [string]$TestName,
        [bool]$RequireAuth = $true
    )
    
    $tempFile = $null
    $headerFile = $null
    
    try {
        $curlArgs = @("-s", "-w", "\n%{http_code}", "-X", $Method)
        $curlArgs += "$baseUrl$Endpoint"
        $curlArgs += @("-H", "Content-Type: application/json")
        
        if ($RequireAuth -and $token) {
            $curlArgs += @("-H", "Authorization: Bearer $token")
        }
        
        if ($BodyFile) {
            $curlArgs += @("-d", "@$BodyFile")
        }
        
        $output = & curl.exe $curlArgs 2>&1
        $outputLines = $output -split "`n"
        $statusCode = [int]($outputLines[-1])
        $content = ($outputLines[0..($outputLines.Count - 2)] -join "`n").Trim()
        
        return Write-TestResult -TestName $TestName -ExpectedCode $ExpectedCode -ActualCode $statusCode -Content $content
    } catch {
        Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
        return Write-TestResult -TestName $TestName -ExpectedCode $ExpectedCode -ActualCode 500 -Content ""
    }
}

function New-JsonFile {
    param(
        [string]$Content,
        [string]$FileName
    )
    $filePath = Join-Path $env:TEMP $FileName
    $Content | Out-File -FilePath $filePath -Encoding utf8
    return $filePath
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  BalconyFarm API Test Suite (curl)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# ========== AUTH MODULE ==========
Write-Host "--- AUTH MODULE ---" -ForegroundColor Yellow
Write-Host ""

# 1. Register
$bodyFile = New-JsonFile -Content '{"username":"testuser002","email":"testuser002@example.com","password":"password123"}' -FileName "register.json"
$result = Invoke-CurlTest -Method "POST" -Endpoint "/api/auth/register" -BodyFile $bodyFile -TestName "[01] Register new user" -RequireAuth $false
if ($result -and ($result.code -eq 0 -or $result.code -eq 200)) { $token = $result.data.token; Write-Host "  Token acquired" -ForegroundColor Cyan; Write-Host "" }

# 2. Login with seed user
$bodyFile = New-JsonFile -Content '{"usernameOrEmail":"gardener1@example.com","password":"password123"}' -FileName "login.json"
$result = Invoke-CurlTest -Method "POST" -Endpoint "/api/auth/login" -BodyFile $bodyFile -TestName "[02] Login (gardener1)" -RequireAuth $false
if ($result -and ($result.code -eq 0 -or $result.code -eq 200)) { $token = $result.data.token; Write-Host "  Token: $($token.Substring(0, 30))..." -ForegroundColor Cyan; Write-Host "" }

# 3. Get current user
Invoke-CurlTest -Method "GET" -Endpoint "/api/auth/me" -TestName "[03] Get current user info"

# 4. Update user
$bodyFile = New-JsonFile -Content '{"username":"gardener1_updated","avatar":"https://example.com/new-avatar.png"}' -FileName "updateuser.json"
Invoke-CurlTest -Method "PUT" -Endpoint "/api/auth/me" -BodyFile $bodyFile -TestName "[04] Update user info"

# 5. Unauthorized test
Invoke-CurlTest -Method "GET" -Endpoint "/api/auth/me" -TestName "[05] Unauthorized access test" -ExpectedCode 401 -RequireAuth $false

# ========== CROPS MODULE ==========
Write-Host "--- CROPS MODULE ---" -ForegroundColor Yellow
Write-Host ""

# 6. Create crop
$bodyFile = New-JsonFile -Content '{"name":"Test Cucumber","variety":"Fruit Cucumber","plantingDate":"2026-05-15","location":"South Balcony","containerType":"Plastic Pot","photoUrl":"https://example.com/cucumber.jpg"}' -FileName "createcrop.json"
$result = Invoke-CurlTest -Method "POST" -Endpoint "/api/crops" -BodyFile $bodyFile -TestName "[06] Create crop" -ExpectedCode 201
if ($result -and ($result.code -eq 0 -or $result.code -eq 200)) { $newCropId = $result.data.id; Write-Host "  Crop ID: $newCropId" -ForegroundColor Cyan; Write-Host "" }

# 7. Get crops list
Invoke-CurlTest -Method "GET" -Endpoint "/api/crops?pageNumber=1&pageSize=10" -TestName "[07] Get crops list (paged)" -RequireAuth $false

# 8. Get crop by ID
if ($newCropId) {
    Invoke-CurlTest -Method "GET" -Endpoint "/api/crops/$newCropId" -TestName "[08] Get crop by ID" -RequireAuth $false
}

# 9. Search crops
Invoke-CurlTest -Method "GET" -Endpoint "/api/crops?searchKeyword=tomato&pageNumber=1&pageSize=10" -TestName "[09] Search crops (tomato)" -RequireAuth $false

# 10. Filter by status
Invoke-CurlTest -Method "GET" -Endpoint "/api/crops?status=Growing&pageNumber=1&pageSize=10" -TestName "[10] Filter crops by status (Growing)" -RequireAuth $false

# 11. Update crop
if ($newCropId) {
    $bodyFile = New-JsonFile -Content '{"name":"Test Cucumber (updated)","location":"East Balcony"}' -FileName "updatecrop.json"
    Invoke-CurlTest -Method "PUT" -Endpoint "/api/crops/$newCropId" -BodyFile $bodyFile -TestName "[11] Update crop"
}

# 12. Update crop status
if ($newCropId) {
    $bodyFile = New-JsonFile -Content '{"status":"Harvesting"}' -FileName "cropstatus.json"
    Invoke-CurlTest -Method "PATCH" -Endpoint "/api/crops/$newCropId/status" -BodyFile $bodyFile -TestName "[12] Update crop status (Harvesting)"
}

# ========== TASKS MODULE ==========
Write-Host "--- TASKS MODULE ---" -ForegroundColor Yellow
Write-Host ""

# 13. Create task
if ($newCropId) {
    $bodyFile = New-JsonFile -Content "{`"cropId`":`"$newCropId`",`"taskType`":`"Water`",`"scheduledDate`":`"2026-06-17`",`"note`":`"Water 500ml`"}" -FileName "createtask.json"
    $result = Invoke-CurlTest -Method "POST" -Endpoint "/api/cropcaretasks" -BodyFile $bodyFile -TestName "[13] Create care task" -ExpectedCode 201
    if ($result -and ($result.code -eq 0 -or $result.code -eq 200)) { $newTaskId = $result.data.id; Write-Host "  Task ID: $newTaskId" -ForegroundColor Cyan; Write-Host "" }
}

# 14. Get tasks list
Invoke-CurlTest -Method "GET" -Endpoint "/api/cropcaretasks?pageNumber=1&pageSize=10" -TestName "[14] Get tasks list"

# 15. Get tasks by crop
if ($newCropId) {
    Invoke-CurlTest -Method "GET" -Endpoint "/api/cropcaretasks?cropId=$newCropId&pageNumber=1&pageSize=10" -TestName "[15] Get tasks by crop"
}

# 16. Update task status
if ($newTaskId) {
    $bodyFile = New-JsonFile -Content '{"status":"Completed"}' -FileName "taskstatus.json"
    Invoke-CurlTest -Method "PATCH" -Endpoint "/api/cropcaretasks/$newTaskId/status" -BodyFile $bodyFile -TestName "[16] Update task status (Completed)"
}

# 17. Delete task
if ($newTaskId) {
    Invoke-CurlTest -Method "DELETE" -Endpoint "/api/cropcaretasks/$newTaskId" -TestName "[17] Delete task"
}

# ========== HARVEST MODULE ==========
Write-Host "--- HARVEST MODULE ---" -ForegroundColor Yellow
Write-Host ""

# 18. Create harvest
if ($newCropId) {
    $bodyFile = New-JsonFile -Content "{`"cropId`":`"$newCropId`",`"harvestDate`":`"2026-06-15`",`"quantity`":0.5,`"unit`":`"kg`",`"qualityNote`":`"First harvest, good quality`",`"photoUrl`":`"https://example.com/harvest.jpg`"}" -FileName "createharvest.json"
    $result = Invoke-CurlTest -Method "POST" -Endpoint "/api/harvestrecords" -BodyFile $bodyFile -TestName "[18] Create harvest record" -ExpectedCode 201
    if ($result -and ($result.code -eq 0 -or $result.code -eq 200)) { $newHarvestId = $result.data.id; Write-Host "  Harvest ID: $newHarvestId" -ForegroundColor Cyan; Write-Host "" }
}

# 19. Get harvest list
Invoke-CurlTest -Method "GET" -Endpoint "/api/harvestrecords?pageNumber=1&pageSize=10" -TestName "[19] Get harvest list"

# 20. Get harvest by ID
if ($newHarvestId) {
    Invoke-CurlTest -Method "GET" -Endpoint "/api/harvestrecords/$newHarvestId" -TestName "[20] Get harvest by ID"
}

# 21. Update harvest
if ($newHarvestId) {
    $bodyFile = New-JsonFile -Content '{"quantity":0.6,"qualityNote":"First harvest, good quality (updated)"}' -FileName "updateharvest.json"
    Invoke-CurlTest -Method "PUT" -Endpoint "/api/harvestrecords/$newHarvestId" -BodyFile $bodyFile -TestName "[21] Update harvest record"
}

# ========== PEST MODULE ==========
Write-Host "--- PEST MODULE ---" -ForegroundColor Yellow
Write-Host ""

# 22. Create pest record
if ($newCropId) {
    $bodyFile = New-JsonFile -Content "{`"cropId`":`"$newCropId`",`"issueType`":`"Aphids`",`"symptoms`":`"Green bugs on leaf back, leaves curling`",`"treatment`":`"Spray soapy water`",`"detectedDate`":`"2026-06-15`"}" -FileName "createpest.json"
    $result = Invoke-CurlTest -Method "POST" -Endpoint "/api/pestrecords" -BodyFile $bodyFile -TestName "[22] Create pest record" -ExpectedCode 201
    if ($result -and ($result.code -eq 0 -or $result.code -eq 200)) { $newPestId = $result.data.id; Write-Host "  Pest ID: $newPestId" -ForegroundColor Cyan; Write-Host "" }
}

# 23. Get pest list
Invoke-CurlTest -Method "GET" -Endpoint "/api/pestrecords?pageNumber=1&pageSize=10" -TestName "[23] Get pest records list"

# 24. Update pest status
if ($newPestId) {
    $bodyFile = New-JsonFile -Content '{"status":"Resolved"}' -FileName "peststatus.json"
    Invoke-CurlTest -Method "PATCH" -Endpoint "/api/pestrecords/$newPestId/status" -BodyFile $bodyFile -TestName "[24] Update pest status (Resolved)"
}

# 25. Update pest record
if ($newPestId) {
    $bodyFile = New-JsonFile -Content '{"treatment":"Spray soapy water, once daily for 3 days"}' -FileName "updatepest.json"
    Invoke-CurlTest -Method "PUT" -Endpoint "/api/pestrecords/$newPestId" -BodyFile $bodyFile -TestName "[25] Update pest record"
}

# ========== STATS MODULE ==========
Write-Host "--- STATS MODULE ---" -ForegroundColor Yellow
Write-Host ""

# 26. Get overview stats
Invoke-CurlTest -Method "GET" -Endpoint "/api/stats/overview" -TestName "[26] Get overview statistics"

# 27. Get trend stats
Invoke-CurlTest -Method "GET" -Endpoint "/api/stats/trend?days=30" -TestName "[27] Get trend statistics (30 days)"

# ========== CLEANUP ==========
Write-Host "--- CLEANUP ---" -ForegroundColor Yellow
Write-Host ""

# 28. Delete pest
if ($newPestId) {
    Invoke-CurlTest -Method "DELETE" -Endpoint "/api/pestrecords/$newPestId" -TestName "[28] Delete pest record"
}

# 29. Delete harvest
if ($newHarvestId) {
    Invoke-CurlTest -Method "DELETE" -Endpoint "/api/harvestrecords/$newHarvestId" -TestName "[29] Delete harvest record"
}

# 30. Delete crop
if ($newCropId) {
    Invoke-CurlTest -Method "DELETE" -Endpoint "/api/crops/$newCropId" -TestName "[30] Delete crop"
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  TEST COMPLETED" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
