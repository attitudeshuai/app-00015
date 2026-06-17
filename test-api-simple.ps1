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
        [PSCustomObject]$Response
    )
    
    $actualCode = $Response.StatusCode
    $body = $Response.Content | ConvertFrom-Json -ErrorAction SilentlyContinue
    
    if ($actualCode -eq $ExpectedCode -and $body -and ($body.code -eq 0 -or $body.code -eq 200)) {
        Write-Host "PASS: $TestName" -ForegroundColor Green
    } else {
        Write-Host "FAIL: $TestName (Expected: $ExpectedCode, Got: $actualCode)" -ForegroundColor Red
        if ($body) {
            Write-Host "  Code: $($body.code), Message: $($body.message)" -ForegroundColor Gray
        }
        if ($Response.Content) {
            $preview = $Response.Content
            if ($preview.Length -gt 150) { $preview = $preview.Substring(0, 147) + "..." }
            Write-Host "  $preview" -ForegroundColor Gray
        }
    }
    Write-Host ""
    return $body
}

function Invoke-ApiTest {
    param(
        [string]$Method,
        [string]$Endpoint,
        [hashtable]$Body = $null,
        [int]$ExpectedCode = 200,
        [string]$TestName,
        [bool]$RequireAuth = $true
    )
    
    $requestHeaders = @{}
    $requestHeaders["Content-Type"] = "application/json"
    
    if ($RequireAuth -and $token) {
        $requestHeaders["Authorization"] = "Bearer $token"
    }
    
    try {
        $params = @{
            Uri = "$baseUrl$Endpoint"
            Method = $Method
            Headers = $requestHeaders
            UseBasicParsing = $true
        }
        
        if ($Body) {
            $params["Body"] = ($Body | ConvertTo-Json -Depth 10)
        }
        
        $response = Invoke-WebRequest @params
        $result = [PSCustomObject]@{
            StatusCode = $response.StatusCode
            Content = $response.Content
        }
    } catch {
        $statusCode = 500
        $content = ""
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $content = $reader.ReadToEnd()
        }
        $result = [PSCustomObject]@{
            StatusCode = $statusCode
            Content = $content
        }
    }
    
    return Write-TestResult -TestName $TestName -ExpectedCode $ExpectedCode -Response $result
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  BalconyFarm API Test Suite" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# ========== AUTH MODULE ==========
Write-Host "--- AUTH MODULE ---" -ForegroundColor Yellow
Write-Host ""

# 1. Register
$body = @{
    username = "testuser001"
    email = "testuser001@example.com"
    password = "password123"
}
$result = Invoke-ApiTest -Method "POST" -Endpoint "/api/auth/register" -Body $body -TestName "[01] Register new user" -RequireAuth $false
if ($result -and ($result.code -eq 0 -or $result.code -eq 200)) { $token = $result.data.token }

# 2. Login with seed user
$body = @{
    usernameOrEmail = "gardener1@example.com"
    password = "password123"
}
$result = Invoke-ApiTest -Method "POST" -Endpoint "/api/auth/login" -Body $body -TestName "[02] Login (gardener1)" -RequireAuth $false
if ($result -and ($result.code -eq 0 -or $result.code -eq 200)) { $token = $result.data.token }

# 3. Get current user
Invoke-ApiTest -Method "GET" -Endpoint "/api/auth/me" -TestName "[03] Get current user info"

# 4. Update user
$body = @{
    username = "gardener1_updated"
    avatar = "https://example.com/new-avatar.png"
}
Invoke-ApiTest -Method "PUT" -Endpoint "/api/auth/me" -Body $body -TestName "[04] Update user info"

# 5. Unauthorized test
Invoke-ApiTest -Method "GET" -Endpoint "/api/auth/me" -TestName "[05] Unauthorized access test" -ExpectedCode 401 -RequireAuth $false

# ========== CROPS MODULE ==========
Write-Host "--- CROPS MODULE ---" -ForegroundColor Yellow
Write-Host ""

# 6. Create crop
$body = @{
    name = "Test Cucumber"
    variety = "Fruit Cucumber"
    plantingDate = "2026-05-15"
    location = "South Balcony"
    containerType = "Plastic Pot"
    photoUrl = "https://example.com/cucumber.jpg"
}
$result = Invoke-ApiTest -Method "POST" -Endpoint "/api/crops" -Body $body -TestName "[06] Create crop" -ExpectedCode 201
if ($result -and ($result.code -eq 0 -or $result.code -eq 200)) { $newCropId = $result.data.id; Write-Host "  Crop ID: $newCropId" -ForegroundColor Cyan; Write-Host "" }

# 7. Get crops list
$ep = '/api/crops?pageNumber=1' + '&pageSize=10'
Invoke-ApiTest -Method "GET" -Endpoint $ep -TestName "[07] Get crops list (paged)"

# 8. Get crop by ID
if ($newCropId) {
    Invoke-ApiTest -Method "GET" -Endpoint "/api/crops/$newCropId" -TestName "[08] Get crop by ID"
}

# 9. Search crops
$ep = '/api/crops?searchKeyword=tomato' + '&pageNumber=1' + '&pageSize=10'
Invoke-ApiTest -Method "GET" -Endpoint $ep -TestName "[09] Search crops (tomato)"

# 10. Filter by status
$ep = '/api/crops?status=Growing' + '&pageNumber=1' + '&pageSize=10'
Invoke-ApiTest -Method "GET" -Endpoint $ep -TestName "[10] Filter crops by status (Growing)"

# 11. Update crop
if ($newCropId) {
    $body = @{
        name = "Test Cucumber (updated)"
        location = "East Balcony"
    }
    Invoke-ApiTest -Method "PUT" -Endpoint "/api/crops/$newCropId" -Body $body -TestName "[11] Update crop"
}

# 12. Update crop status
if ($newCropId) {
    $body = @{ status = "Harvesting" }
    Invoke-ApiTest -Method "PATCH" -Endpoint "/api/crops/$newCropId/status" -Body $body -TestName "[12] Update crop status (Harvesting)"
}

# ========== TASKS MODULE ==========
Write-Host "--- TASKS MODULE ---" -ForegroundColor Yellow
Write-Host ""

# 13. Create task
if ($newCropId) {
    $body = @{
        cropId = $newCropId
        taskType = "Water"
        scheduledDate = "2026-06-17"
        note = "Water 500ml"
    }
    $result = Invoke-ApiTest -Method "POST" -Endpoint "/api/cropcaretasks" -Body $body -TestName "[13] Create care task" -ExpectedCode 201
    if ($result -and ($result.code -eq 0 -or $result.code -eq 200)) { $newTaskId = $result.data.id; Write-Host "  Task ID: $newTaskId" -ForegroundColor Cyan; Write-Host "" }
}

# 14. Get tasks list
$ep = '/api/cropcaretasks?pageNumber=1' + '&pageSize=10'
Invoke-ApiTest -Method "GET" -Endpoint $ep -TestName "[14] Get tasks list"

# 15. Get tasks by crop
if ($newCropId) {
    $ep = '/api/cropcaretasks?cropId=' + $newCropId + '&pageNumber=1' + '&pageSize=10'
    Invoke-ApiTest -Method "GET" -Endpoint $ep -TestName "[15] Get tasks by crop"
}

# 16. Update task status
if ($newTaskId) {
    $body = @{ status = "Completed" }
    Invoke-ApiTest -Method "PATCH" -Endpoint "/api/cropcaretasks/$newTaskId/status" -Body $body -TestName "[16] Update task status (Completed)"
}

# 17. Delete task
if ($newTaskId) {
    Invoke-ApiTest -Method "DELETE" -Endpoint "/api/cropcaretasks/$newTaskId" -TestName "[17] Delete task"
}

# ========== HARVEST MODULE ==========
Write-Host "--- HARVEST MODULE ---" -ForegroundColor Yellow
Write-Host ""

# 18. Create harvest
if ($newCropId) {
    $body = @{
        cropId = $newCropId
        harvestDate = "2026-06-15"
        quantity = 0.5
        unit = "kg"
        qualityNote = "First harvest, good quality"
        photoUrl = "https://example.com/harvest.jpg"
    }
    $result = Invoke-ApiTest -Method "POST" -Endpoint "/api/harvestrecords" -Body $body -TestName "[18] Create harvest record" -ExpectedCode 201
    if ($result -and ($result.code -eq 0 -or $result.code -eq 200)) { $newHarvestId = $result.data.id; Write-Host "  Harvest ID: $newHarvestId" -ForegroundColor Cyan; Write-Host "" }
}

# 19. Get harvest list
$ep = '/api/harvestrecords?pageNumber=1' + '&pageSize=10'
Invoke-ApiTest -Method "GET" -Endpoint $ep -TestName "[19] Get harvest list"

# 20. Get harvest by ID
if ($newHarvestId) {
    Invoke-ApiTest -Method "GET" -Endpoint "/api/harvestrecords/$newHarvestId" -TestName "[20] Get harvest by ID"
}

# 21. Update harvest
if ($newHarvestId) {
    $body = @{
        quantity = 0.6
        qualityNote = "First harvest, good quality (updated)"
    }
    Invoke-ApiTest -Method "PUT" -Endpoint "/api/harvestrecords/$newHarvestId" -Body $body -TestName "[21] Update harvest record"
}

# ========== PEST MODULE ==========
Write-Host "--- PEST MODULE ---" -ForegroundColor Yellow
Write-Host ""

# 22. Create pest record
if ($newCropId) {
    $body = @{
        cropId = $newCropId
        issueType = "Aphids"
        symptoms = "Green bugs on leaf back, leaves curling"
        treatment = "Spray soapy water"
        detectedDate = "2026-06-15"
    }
    $result = Invoke-ApiTest -Method "POST" -Endpoint "/api/pestrecords" -Body $body -TestName "[22] Create pest record" -ExpectedCode 201
    if ($result -and ($result.code -eq 0 -or $result.code -eq 200)) { $newPestId = $result.data.id; Write-Host "  Pest ID: $newPestId" -ForegroundColor Cyan; Write-Host "" }
}

# 23. Get pest list
$ep = '/api/pestrecords?pageNumber=1' + '&pageSize=10'
Invoke-ApiTest -Method "GET" -Endpoint $ep -TestName "[23] Get pest records list"

# 24. Update pest status
if ($newPestId) {
    $body = @{ status = "Resolved" }
    Invoke-ApiTest -Method "PATCH" -Endpoint "/api/pestrecords/$newPestId/status" -Body $body -TestName "[24] Update pest status (Resolved)"
}

# 25. Update pest record
if ($newPestId) {
    $body = @{
        treatment = "Spray soapy water, once daily for 3 days"
    }
    Invoke-ApiTest -Method "PUT" -Endpoint "/api/pestrecords/$newPestId" -Body $body -TestName "[25] Update pest record"
}

# ========== STATS MODULE ==========
Write-Host "--- STATS MODULE ---" -ForegroundColor Yellow
Write-Host ""

# 26. Get overview stats
Invoke-ApiTest -Method "GET" -Endpoint "/api/stats/overview" -TestName "[26] Get overview statistics"

# 27. Get trend stats
$ep = '/api/stats/trend?days=30'
Invoke-ApiTest -Method "GET" -Endpoint $ep -TestName "[27] Get trend statistics (30 days)"

# ========== CLEANUP ==========
Write-Host "--- CLEANUP ---" -ForegroundColor Yellow
Write-Host ""

# 28. Delete pest
if ($newPestId) {
    Invoke-ApiTest -Method "DELETE" -Endpoint "/api/pestrecords/$newPestId" -TestName "[28] Delete pest record"
}

# 29. Delete harvest
if ($newHarvestId) {
    Invoke-ApiTest -Method "DELETE" -Endpoint "/api/harvestrecords/$newHarvestId" -TestName "[29] Delete harvest record"
}

# 30. Delete crop
if ($newCropId) {
    Invoke-ApiTest -Method "DELETE" -Endpoint "/api/crops/$newCropId" -TestName "[30] Delete crop"
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  TEST COMPLETED" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
