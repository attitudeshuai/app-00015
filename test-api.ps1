$baseUrl = "http://localhost:8085"
$token = ""
$newUserId = ""
$newCropId = ""
$newTaskId = ""
$newHarvestId = ""
$newPestId = ""

function Write-Result {
    param($testName, $response, $expectedCode = 200)
    
    $statusCode = $response.StatusCode
    $body = $response.Content | ConvertFrom-Json -ErrorAction SilentlyContinue
    
    if ($statusCode -eq $expectedCode) {
        Write-Host "✅ $testName" -ForegroundColor Green
        if ($body) {
            Write-Host "   Code: $($body.code), Message: $($body.message)" -ForegroundColor Gray
        }
    } else {
        Write-Host "❌ $testName (Expected: $expectedCode, Got: $statusCode)" -ForegroundColor Red
        if ($body) {
            Write-Host "   Code: $($body.code), Message: $($body.message)" -ForegroundColor Gray
        }
        Write-Host "   Response: $($response.Content)" -ForegroundColor Gray
    }
    Write-Host ""
    return $body
}

function Invoke-Api {
    param($method, $endpoint, $body = $null, $headers = @{})
    
    if ($token -and -not $headers.ContainsKey("Authorization")) {
        $headers["Authorization"] = "Bearer $token"
    }
    
    $params = @{
        Uri = "$baseUrl$endpoint"
        Method = $method
        Headers = $headers
        UseBasicParsing = $true
    }
    
    if ($body) {
        $params["Body"] = ($body | ConvertTo-Json -Depth 10)
        $params["ContentType"] = "application/json"
    }
    
    try {
        $response = Invoke-WebRequest @params
        return [pscustomobject]@{
            StatusCode = $response.StatusCode
            Content = $response.Content
        }
    } catch {
        if ($_.Exception.Response) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            return [pscustomobject]@{
                StatusCode = [int]$_.Exception.Response.StatusCode
                Content = $responseBody
            }
        }
        throw
    }
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  阳台种菜助手 API 接口测试" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "--- 🔐 用户认证模块 ---" -ForegroundColor Yellow
Write-Host ""

# 1. 注册新用户
$registerBody = @{
    username = "testuser"
    email = "testuser@example.com"
    password = "password123"
}
$response = Invoke-Api "POST" "/api/auth/register" $registerBody
$result = Write-Result "1. 注册新用户" $response 200
if ($result -and $result.code -eq 0) {
    $token = $result.data.token
    $newUserId = $result.data.user.id
    Write-Host "   Token 已保存, 用户ID: $newUserId" -ForegroundColor Cyan
}
Write-Host ""

# 2. 登录（使用seed数据中的用户）
$loginBody = @{
    usernameOrEmail = "gardener1@example.com"
    password = "password123"
}
$response = Invoke-Api "POST" "/api/auth/login" $loginBody
$result = Write-Result "2. 用户登录（gardener1）" $response 200
if ($result -and $result.code -eq 0) {
    $token = $result.data.token
    Write-Host "   Token 已更新" -ForegroundColor Cyan
}
Write-Host ""

# 3. 获取当前用户信息
$response = Invoke-Api "GET" "/api/auth/me"
$result = Write-Result "3. 获取当前用户信息" $response 200
Write-Host ""

# 4. 更新用户信息
$updateBody = @{
    username = "gardener1_updated"
    avatar = "https://example.com/new-avatar.png"
}
$response = Invoke-Api "PUT" "/api/auth/me" $updateBody
$result = Write-Result "4. 更新用户信息" $response 200
Write-Host ""

# 5. 未授权访问测试
$response = Invoke-Api "GET" "/api/auth/me" $null @{}
$result = Write-Result "5. 未授权访问测试" $response 401
Write-Host ""

Write-Host "--- 🌱 作物管理模块 ---" -ForegroundColor Yellow
Write-Host ""

# 6. 创建作物
$cropBody = @{
    name = "测试黄瓜"
    variety = "水果黄瓜"
    plantingDate = (Get-Date).AddDays(-30).ToString("yyyy-MM-dd")
    location = "南阳台"
    containerType = "塑料花盆"
    photoUrl = "https://example.com/cucumber.jpg"
}
$response = Invoke-Api "POST" "/api/crops" $cropBody
$result = Write-Result "6. 创建作物" $response 201
if ($result -and $result.code -eq 0) {
    $newCropId = $result.data.id
    Write-Host "   作物ID: $newCropId" -ForegroundColor Cyan
}
Write-Host ""

# 7. 获取作物列表（分页）
$response = Invoke-Api "GET" "/api/crops?pageNumber=1&pageSize=10"
$result = Write-Result "7. 获取作物列表（分页）" $response 200
if ($result -and $result.code -eq 0) {
    Write-Host "   总记录数: $($result.data.totalCount), 当前页: $($result.data.items.Count)" -ForegroundColor Gray
}
Write-Host ""

# 8. 获取作物详情
$response = Invoke-Api "GET" "/api/crops/$newCropId"
$result = Write-Result "8. 获取作物详情" $response 200
Write-Host ""

# 9. 搜索作物
$response = Invoke-Api "GET" "/api/crops?searchKeyword=番茄&pageNumber=1&pageSize=10"
$result = Write-Result "9. 搜索作物（关键词：番茄）" $response 200
Write-Host ""

# 10. 按状态筛选
$response = Invoke-Api "GET" "/api/crops?status=Growing&pageNumber=1&pageSize=10"
$result = Write-Result "10. 按状态筛选（生长中）" $response 200
Write-Host ""

# 11. 更新作物
$updateCropBody = @{
    name = "测试黄瓜（更新）"
    location = "东阳台"
}
$response = Invoke-Api "PUT" "/api/crops/$newCropId" $updateCropBody
$result = Write-Result "11. 更新作物信息" $response 200
Write-Host ""

# 12. 更新作物状态
$updateStatusBody = @{
    status = "Harvesting"
}
$response = Invoke-Api "PATCH" "/api/crops/$newCropId/status" $updateStatusBody
$result = Write-Result "12. 更新作物状态（采收中）" $response 200
Write-Host ""

Write-Host "--- 📋 养护任务模块 ---" -ForegroundColor Yellow
Write-Host ""

# 13. 创建养护任务
$taskBody = @{
    cropId = $newCropId
    taskType = "Water"
    scheduledDate = (Get-Date).AddDays(1).ToString("yyyy-MM-dd")
    note = "浇水500ml"
}
$response = Invoke-Api "POST" "/api/cropcaretasks" $taskBody
$result = Write-Result "13. 创建养护任务" $response 201
if ($result -and $result.code -eq 0) {
    $newTaskId = $result.data.id
    Write-Host "   任务ID: $newTaskId" -ForegroundColor Cyan
}
Write-Host ""

# 14. 获取任务列表
$response = Invoke-Api "GET" "/api/cropcaretasks?pageNumber=1&pageSize=10"
$result = Write-Result "14. 获取任务列表" $response 200
Write-Host ""

# 15. 按作物查询任务
$response = Invoke-Api "GET" "/api/cropcaretasks?cropId=$newCropId&pageNumber=1&pageSize=10"
$result = Write-Result "15. 按作物查询任务" $response 200
Write-Host ""

# 16. 更新任务状态为完成
$updateTaskStatusBody = @{
    status = "Completed"
}
$response = Invoke-Api "PATCH" "/api/cropcaretasks/$newTaskId/status" $updateTaskStatusBody
$result = Write-Result "16. 更新任务状态（完成）" $response 200
if ($result -and $result.code -eq 0) {
    Write-Host "   完成日期: $($result.data.completedDate)" -ForegroundColor Gray
}
Write-Host ""

# 17. 删除任务
$response = Invoke-Api "DELETE" "/api/cropcaretasks/$newTaskId"
$result = Write-Result "17. 删除任务" $response 200
Write-Host ""

Write-Host "--- 🍅 收成管理模块 ---" -ForegroundColor Yellow
Write-Host ""

# 18. 创建收成记录
$harvestBody = @{
    cropId = $newCropId
    harvestDate = (Get-Date).ToString("yyyy-MM-dd")
    quantity = 0.5
    unit = "kg"
    qualityNote = "第一次采收，品质很好"
    photoUrl = "https://example.com/harvest.jpg"
}
$response = Invoke-Api "POST" "/api/harvestrecords" $harvestBody
$result = Write-Result "18. 创建收成记录" $response 201
if ($result -and $result.code -eq 0) {
    $newHarvestId = $result.data.id
    Write-Host "   收成记录ID: $newHarvestId" -ForegroundColor Cyan
}
Write-Host ""

# 19. 获取收成列表
$response = Invoke-Api "GET" "/api/harvestrecords?pageNumber=1&pageSize=10"
$result = Write-Result "19. 获取收成列表" $response 200
Write-Host ""

# 20. 获取收成详情
$response = Invoke-Api "GET" "/api/harvestrecords/$newHarvestId"
$result = Write-Result "20. 获取收成详情" $response 200
Write-Host ""

# 21. 更新收成记录
$updateHarvestBody = @{
    quantity = 0.6
    qualityNote = "第一次采收，品质很好，更新"
}
$response = Invoke-Api "PUT" "/api/harvestrecords/$newHarvestId" $updateHarvestBody
$result = Write-Result "21. 更新收成记录" $response 200
Write-Host ""

Write-Host "--- 🐛 病虫害管理模块 ---" -ForegroundColor Yellow
Write-Host ""

# 22. 创建病虫害记录
$pestBody = @{
    cropId = $newCropId
    issueType = "蚜虫"
    symptoms = "叶片背面有绿色小虫子"
    treatment = "喷洒肥皂水"
    detectedDate = (Get-Date).ToString("yyyy-MM-dd")
}
$response = Invoke-Api "POST" "/api/pestrecords" $pestBody
$result = Write-Result "22. 创建病虫害记录" $response 201
if ($result -and $result.code -eq 0) {
    $newPestId = $result.data.id
    Write-Host "   病虫害记录ID: $newPestId" -ForegroundColor Cyan
}
Write-Host ""

# 23. 获取病虫害列表
$response = Invoke-Api "GET" "/api/pestrecords?pageNumber=1&pageSize=10"
$result = Write-Result "23. 获取病虫害列表" $response 200
Write-Host ""

# 24. 更新病虫害状态
$updatePestStatusBody = @{
    status = "Resolved"
}
$response = Invoke-Api "PATCH" "/api/pestrecords/$newPestId/status" $updatePestStatusBody
$result = Write-Result "24. 更新病虫害状态（已解决）" $response 200
if ($result -and $result.code -eq 0) {
    Write-Host "   解决日期: $($result.data.resolvedDate)" -ForegroundColor Gray
}
Write-Host ""

# 25. 更新病虫害记录
$updatePestBody = @{
    treatment = "喷洒肥皂水，连续3天"
}
$response = Invoke-Api "PUT" "/api/pestrecords/$newPestId" $updatePestBody
$result = Write-Result "25. 更新病虫害记录" $response 200
Write-Host ""

Write-Host "--- 📊 统计分析模块 ---" -ForegroundColor Yellow
Write-Host ""

# 26. 获取总览统计
$response = Invoke-Api "GET" "/api/stats/overview"
$result = Write-Result "26. 获取总览统计" $response 200
if ($result -and $result.code -eq 0) {
    Write-Host "   作物总数: $($result.data.totalCrops)" -ForegroundColor Gray
    Write-Host "   收成总量: $($result.data.totalHarvestQuantity) kg" -ForegroundColor Gray
    Write-Host "   进行中任务: $($result.data.pendingTasks)" -ForegroundColor Gray
}
Write-Host ""

# 27. 获取趋势统计
$response = Invoke-Api "GET" "/api/stats/trend?days=30"
$result = Write-Result "27. 获取趋势统计（30天）" $response 200
if ($result -and $result.code -eq 0) {
    Write-Host "   数据点数: $($result.data.trendData.Count)" -ForegroundColor Gray
}
Write-Host ""

Write-Host "--- 🧹 数据清理 ---" -ForegroundColor Yellow
Write-Host ""

# 28. 删除病虫害记录
$response = Invoke-Api "DELETE" "/api/pestrecords/$newPestId"
$result = Write-Result "28. 删除病虫害记录" $response 200
Write-Host ""

# 29. 删除收成记录
$response = Invoke-Api "DELETE" "/api/harvestrecords/$newHarvestId"
$result = Write-Result "29. 删除收成记录" $response 200
Write-Host ""

# 30. 删除作物
$response = Invoke-Api "DELETE" "/api/crops/$newCropId"
$result = Write-Result "30. 删除作物" $response 200
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  测试完成！" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
