$baseUrl = "http://localhost:8085"
$token = $null
$newCropId = $null
$newTaskId = $null
$newHarvestId = $null
$newPestId = $null

$headers = @{
    "Content-Type" = "application/json"
}

function Write-TestResult {
    param(
        [string]$TestName,
        [int]$ExpectedCode,
        [PSCustomObject]$Response
    )
    
    $actualCode = $Response.StatusCode
    $body = $Response.Content | ConvertFrom-Json -ErrorAction SilentlyContinue
    
    if ($actualCode -eq $ExpectedCode -and $body -and $body.code -eq 0) {
        Write-Host "✅ $TestName" -ForegroundColor Green
        if ($body.data) {
            $dataStr = $body.data | ConvertTo-Json -Depth 2 -Compress
            if ($dataStr.Length -gt 100) {
                $dataStr = $dataStr.Substring(0, 97) + "..."
            }
            Write-Host "   $dataStr" -ForegroundColor Gray
        }
    } else {
        Write-Host "❌ $TestName (Expected: $ExpectedCode, Got: $actualCode)" -ForegroundColor Red
        if ($body) {
            Write-Host "   Code: $($body.code), Message: $($body.message)" -ForegroundColor Gray
        }
        if ($Response.Content) {
            $contentPreview = $Response.Content
            if ($contentPreview.Length -gt 200) {
                $contentPreview = $contentPreview.Substring(0, 197) + "..."
            }
            Write-Host "   $contentPreview" -ForegroundColor Gray
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
Write-Host "  阳台种菜助手 API 接口测试" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# ========== 🔐 用户认证模块 ==========
Write-Host "--- 🔐 用户认证模块 ---" -ForegroundColor Yellow
Write-Host ""

# 1. 注册新用户
$body = @{
    username = "testuser001"
    email = "testuser001@example.com"
    password = "password123"
}
$result = Invoke-ApiTest -Method "POST" -Endpoint "/api/auth/register" -Body $body -TestName "1. 注册新用户" -RequireAuth $false
if ($result -and $result.code -eq 0) {
    $token = $result.data.token
    Write-Host "   已获取 Token" -ForegroundColor Cyan
}
Write-Host ""

# 2. 用户登录（使用seed数据）
$body = @{
    usernameOrEmail = "gardener1@example.com"
    password = "password123"
}
$result = Invoke-ApiTest -Method "POST" -Endpoint "/api/auth/login" -Body $body -TestName "2. 用户登录（gardener1）" -RequireAuth $false
if ($result -and $result.code -eq 0) {
    $token = $result.data.token
    Write-Host "   已更新 Token" -ForegroundColor Cyan
}
Write-Host ""

# 3. 获取当前用户信息
$result = Invoke-ApiTest -Method "GET" -Endpoint "/api/auth/me" -TestName "3. 获取当前用户信息"
Write-Host ""

# 4. 更新用户信息
$body = @{
    username = "gardener1_updated"
    avatar = "https://example.com/new-avatar.png"
}
$result = Invoke-ApiTest -Method "PUT" -Endpoint "/api/auth/me" -Body $body -TestName "4. 更新用户信息"
Write-Host ""

# 5. 未授权访问测试
$result = Invoke-ApiTest -Method "GET" -Endpoint "/api/auth/me" -TestName "5. 未授权访问测试" -ExpectedCode 401 -RequireAuth $false
Write-Host ""

# ========== 🌱 作物管理模块 ==========
Write-Host "--- 🌱 作物管理模块 ---" -ForegroundColor Yellow
Write-Host ""

# 6. 创建作物
$body = @{
    name = "测试黄瓜"
    variety = "水果黄瓜"
    plantingDate = "2026-05-15"
    location = "南阳台"
    containerType = "塑料花盆"
    photoUrl = "https://example.com/cucumber.jpg"
}
$result = Invoke-ApiTest -Method "POST" -Endpoint "/api/crops" -Body $body -TestName "6. 创建作物" -ExpectedCode 201
if ($result -and $result.code -eq 0) {
    $newCropId = $result.data.id
    Write-Host "   作物ID: $newCropId" -ForegroundColor Cyan
}
Write-Host ""

# 7. 获取作物列表（分页）
$result = Invoke-ApiTest -Method "GET" -Endpoint "/api/crops?pageNumber=1&pageSize=10" -TestName "7. 获取作物列表（分页）"
Write-Host ""

# 8. 获取作物详情
if ($newCropId) {
    $result = Invoke-ApiTest -Method "GET" -Endpoint "/api/crops/$newCropId" -TestName "8. 获取作物详情"
    Write-Host ""
}

# 9. 搜索作物
$result = Invoke-ApiTest -Method "GET" -Endpoint "/api/crops?searchKeyword=番茄&pageNumber=1&pageSize=10" -TestName "9. 搜索作物（番茄）"
Write-Host ""

# 10. 按状态筛选
$result = Invoke-ApiTest -Method "GET" -Endpoint "/api/crops?status=Growing&pageNumber=1&pageSize=10" -TestName "10. 按状态筛选（生长中）"
Write-Host ""

# 11. 更新作物
if ($newCropId) {
    $body = @{
        name = "测试黄瓜（更新）"
        location = "东阳台"
    }
    $result = Invoke-ApiTest -Method "PUT" -Endpoint "/api/crops/$newCropId" -Body $body -TestName "11. 更新作物信息"
    Write-Host ""
}

# 12. 更新作物状态
if ($newCropId) {
    $body = @{
        status = "Harvesting"
    }
    $result = Invoke-ApiTest -Method "PATCH" -Endpoint "/api/crops/$newCropId/status" -Body $body -TestName "12. 更新作物状态（采收中）"
    Write-Host ""
}

# ========== 📋 养护任务模块 ==========
Write-Host "--- 📋 养护任务模块 ---" -ForegroundColor Yellow
Write-Host ""

# 13. 创建养护任务
if ($newCropId) {
    $body = @{
        cropId = $newCropId
        taskType = "Water"
        scheduledDate = "2026-06-17"
        note = "浇水500ml"
    }
    $result = Invoke-ApiTest -Method "POST" -Endpoint "/api/cropcaretasks" -Body $body -TestName "13. 创建养护任务" -ExpectedCode 201
    if ($result -and $result.code -eq 0) {
        $newTaskId = $result.data.id
        Write-Host "   任务ID: $newTaskId" -ForegroundColor Cyan
    }
    Write-Host ""
}

# 14. 获取任务列表
$result = Invoke-ApiTest -Method "GET" -Endpoint "/api/cropcaretasks?pageNumber=1&pageSize=10" -TestName "14. 获取任务列表"
Write-Host ""

# 15. 按作物查询任务
if ($newCropId) {
    $result = Invoke-ApiTest -Method "GET" -Endpoint "/api/cropcaretasks?cropId=$newCropId&pageNumber=1&pageSize=10" -TestName "15. 按作物查询任务"
    Write-Host ""
}

# 16. 更新任务状态
if ($newTaskId) {
    $body = @{
        status = "Completed"
    }
    $result = Invoke-ApiTest -Method "PATCH" -Endpoint "/api/cropcaretasks/$newTaskId/status" -Body $body -TestName "16. 更新任务状态（完成）"
    if ($result -and $result.code -eq 0 -and $result.data.completedDate) {
        Write-Host "   完成日期: $($result.data.completedDate)" -ForegroundColor Gray
    }
    Write-Host ""
}

# 17. 删除任务
if ($newTaskId) {
    $result = Invoke-ApiTest -Method "DELETE" -Endpoint "/api/cropcaretasks/$newTaskId" -TestName "17. 删除任务"
    Write-Host ""
}

# ========== 🍅 收成管理模块 ==========
Write-Host "--- 🍅 收成管理模块 ---" -ForegroundColor Yellow
Write-Host ""

# 18. 创建收成记录
if ($newCropId) {
    $body = @{
        cropId = $newCropId
        harvestDate = "2026-06-15"
        quantity = 0.5
        unit = "kg"
        qualityNote = "第一次采收，品质很好"
        photoUrl = "https://example.com/harvest.jpg"
    }
    $result = Invoke-ApiTest -Method "POST" -Endpoint "/api/harvestrecords" -Body $body -TestName "18. 创建收成记录" -ExpectedCode 201
    if ($result -and $result.code -eq 0) {
        $newHarvestId = $result.data.id
        Write-Host "   收成记录ID: $newHarvestId" -ForegroundColor Cyan
    }
    Write-Host ""
}

# 19. 获取收成列表
$result = Invoke-ApiTest -Method "GET" -Endpoint "/api/harvestrecords?pageNumber=1&pageSize=10" -TestName "19. 获取收成列表"
Write-Host ""

# 20. 获取收成详情
if ($newHarvestId) {
    $result = Invoke-ApiTest -Method "GET" -Endpoint "/api/harvestrecords/$newHarvestId" -TestName "20. 获取收成详情"
    Write-Host ""
}

# 21. 更新收成记录
if ($newHarvestId) {
    $body = @{
        quantity = 0.6
        qualityNote = "第一次采收，品质很好（更新）"
    }
    $result = Invoke-ApiTest -Method "PUT" -Endpoint "/api/harvestrecords/$newHarvestId" -Body $body -TestName "21. 更新收成记录"
    Write-Host ""
}

# ========== 🐛 病虫害管理模块 ==========
Write-Host "--- 🐛 病虫害管理模块 ---" -ForegroundColor Yellow
Write-Host ""

# 22. 创建病虫害记录
if ($newCropId) {
    $body = @{
        cropId = $newCropId
        issueType = "蚜虫"
        symptoms = "叶片背面有绿色小虫子，叶片卷曲"
        treatment = "喷洒肥皂水稀释液"
        detectedDate = "2026-06-15"
    }
    $result = Invoke-ApiTest -Method "POST" -Endpoint "/api/pestrecords" -Body $body -TestName "22. 创建病虫害记录" -ExpectedCode 201
    if ($result -and $result.code -eq 0) {
        $newPestId = $result.data.id
        Write-Host "   病虫害记录ID: $newPestId" -ForegroundColor Cyan
    }
    Write-Host ""
}

# 23. 获取病虫害列表
$result = Invoke-ApiTest -Method "GET" -Endpoint "/api/pestrecords?pageNumber=1&pageSize=10" -TestName "23. 获取病虫害列表"
Write-Host ""

# 24. 更新病虫害状态
if ($newPestId) {
    $body = @{
        status = "Resolved"
    }
    $result = Invoke-ApiTest -Method "PATCH" -Endpoint "/api/pestrecords/$newPestId/status" -Body $body -TestName "24. 更新病虫害状态（已解决）"
    if ($result -and $result.code -eq 0 -and $result.data.resolvedDate) {
        Write-Host "   解决日期: $($result.data.resolvedDate)" -ForegroundColor Gray
    }
    Write-Host ""
}

# 25. 更新病虫害记录
if ($newPestId) {
    $body = @{
        treatment = "喷洒肥皂水稀释液，每天一次，连续3天"
    }
    $result = Invoke-ApiTest -Method "PUT" -Endpoint "/api/pestrecords/$newPestId" -Body $body -TestName "25. 更新病虫害记录"
    Write-Host ""
}

# ========== 📊 统计分析模块 ==========
Write-Host "--- 📊 统计分析模块 ---" -ForegroundColor Yellow
Write-Host ""

# 26. 获取总览统计
$result = Invoke-ApiTest -Method "GET" -Endpoint "/api/stats/overview" -TestName "26. 获取总览统计"
if ($result -and $result.code -eq 0) {
    Write-Host "   作物总数: $($result.data.totalCrops), 收成总量: $($result.data.totalHarvestQuantity)kg" -ForegroundColor Gray
}
Write-Host ""

# 27. 获取趋势统计
$result = Invoke-ApiTest -Method "GET" -Endpoint "/api/stats/trend?days=30" -TestName "27. 获取趋势统计（30天）"
Write-Host ""

# ========== 🧹 数据清理 ==========
Write-Host "--- 🧹 数据清理 ---" -ForegroundColor Yellow
Write-Host ""

# 28. 删除病虫害记录
if ($newPestId) {
    $result = Invoke-ApiTest -Method "DELETE" -Endpoint "/api/pestrecords/$newPestId" -TestName "28. 删除病虫害记录"
    Write-Host ""
}

# 29. 删除收成记录
if ($newHarvestId) {
    $result = Invoke-ApiTest -Method "DELETE" -Endpoint "/api/harvestrecords/$newHarvestId" -TestName "29. 删除收成记录"
    Write-Host ""
}

# 30. 删除作物
if ($newCropId) {
    $result = Invoke-ApiTest -Method "DELETE" -Endpoint "/api/crops/$newCropId" -TestName "30. 删除作物"
    Write-Host ""
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  测试完成！" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
