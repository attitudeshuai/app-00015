@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

set BASE_URL=http://localhost:8085
set TOKEN=
set NEW_USER_ID=
set NEW_CROP_ID=
set NEW_TASK_ID=
set NEW_HARVEST_ID=
set NEW_PEST_ID=

echo ========================================
echo   阳台种菜助手 API 接口测试
echo ========================================
echo.

echo --- 🔐 用户认证模块 ---
echo.

echo [1/30] 注册新用户...
for /f "delims=" %%i in ('curl.exe -s -X POST "%BASE_URL%/api/auth/register" -H "Content-Type: application/json" -d "{\"username\":\"testuser\",\"email\":\"testuser@example.com\",\"password\":\"password123\"}"') do set RESPONSE=%%i
echo !RESPONSE! | findstr "\"code\":0" >nul
if !errorlevel! equ 0 (
    echo ✅ 注册新用户 - 成功
    for /f "tokens=2 delims=:{}, " %%a in ('echo !RESPONSE! ^| findstr /r "\"token\":\"[^\"]*\""') do (
        set "TOKEN=%%~a"
    )
) else (
    echo ❌ 注册新用户 - 失败
    echo    响应: !RESPONSE!
)
echo.

echo [2/30] 用户登录（gardener1）...
for /f "delims=" %%i in ('curl.exe -s -X POST "%BASE_URL%/api/auth/login" -H "Content-Type: application/json" -d "{\"usernameOrEmail\":\"gardener1@example.com\",\"password\":\"password123\"}"') do set RESPONSE=%%i
echo !RESPONSE! | findstr "\"code\":0" >nul
if !errorlevel! equ 0 (
    echo ✅ 用户登录 - 成功
    for /f "tokens=2 delims=:{}, " %%a in ('echo !RESPONSE! ^| findstr /r "\"token\":\"[^\"]*\""') do (
        set "TOKEN=%%~a"
    )
    echo    Token 已更新
) else (
    echo ❌ 用户登录 - 失败
    echo    响应: !RESPONSE!
)
echo.

echo [3/30] 获取当前用户信息...
for /f "delims=" %%i in ('curl.exe -s -X GET "%BASE_URL%/api/auth/me" -H "Authorization: Bearer !TOKEN!"') do set RESPONSE=%%i
echo !RESPONSE! | findstr "\"code\":0" >nul
if !errorlevel! equ 0 (
    echo ✅ 获取当前用户信息 - 成功
) else (
    echo ❌ 获取当前用户信息 - 失败
    echo    响应: !RESPONSE!
)
echo.

echo [4/30] 更新用户信息...
for /f "delims=" %%i in ('curl.exe -s -X PUT "%BASE_URL%/api/auth/me" -H "Content-Type: application/json" -H "Authorization: Bearer !TOKEN!" -d "{\"username\":\"gardener1_updated\",\"avatar\":\"https://example.com/new-avatar.png\"}"') do set RESPONSE=%%i
echo !RESPONSE! | findstr "\"code\":0" >nul
if !errorlevel! equ 0 (
    echo ✅ 更新用户信息 - 成功
) else (
    echo ❌ 更新用户信息 - 失败
    echo    响应: !RESPONSE!
)
echo.

echo [5/30] 未授权访问测试...
for /f "delims=" %%i in ('curl.exe -s -o nul -w "%%{http_code}" -X GET "%BASE_URL%/api/auth/me"') do set STATUS=%%i
if "!STATUS!" equ "401" (
    echo ✅ 未授权访问测试 - 成功 (401)
) else (
    echo ❌ 未授权访问测试 - 失败 (期望401，实际!STATUS!)
)
echo.

echo --- 🌱 作物管理模块 ---
echo.

echo [6/30] 创建作物...
for /f "delims=" %%i in ('curl.exe -s -X POST "%BASE_URL%/api/crops" -H "Content-Type: application/json" -H "Authorization: Bearer !TOKEN!" -d "{\"name\":\"测试黄瓜\",\"variety\":\"水果黄瓜\",\"plantingDate\":\"2026-05-15\",\"location\":\"南阳台\",\"containerType\":\"塑料花盆\",\"photoUrl\":\"https://example.com/cucumber.jpg\"}"') do set RESPONSE=%%i
echo !RESPONSE! | findstr "\"code\":0" >nul
if !errorlevel! equ 0 (
    echo ✅ 创建作物 - 成功 (201)
    for /f "tokens=2 delims=:{}, " %%a in ('echo !RESPONSE! ^| findstr /r "\"id\":\"[0-9a-fA-F-]*\""') do (
        if "!NEW_CROP_ID!"=="" set "NEW_CROP_ID=%%~a"
    )
    echo    作物ID: !NEW_CROP_ID!
) else (
    echo ❌ 创建作物 - 失败
    echo    响应: !RESPONSE!
)
echo.

echo [7/30] 获取作物列表（分页）...
for /f "delims=" %%i in ('curl.exe -s -X GET "%BASE_URL%/api/crops?pageNumber=1^&pageSize=10" -H "Authorization: Bearer !TOKEN!"') do set RESPONSE=%%i
echo !RESPONSE! | findstr "\"code\":0" >nul
if !errorlevel! equ 0 (
    echo ✅ 获取作物列表 - 成功
) else (
    echo ❌ 获取作物列表 - 失败
    echo    响应: !RESPONSE!
)
echo.

if not "!NEW_CROP_ID!"=="" (
echo [8/30] 获取作物详情...
for /f "delims=" %%i in ('curl.exe -s -X GET "%BASE_URL%/api/crops/!NEW_CROP_ID!" -H "Authorization: Bearer !TOKEN!"') do set RESPONSE=%%i
echo !RESPONSE! | findstr "\"code\":0" >nul
if !errorlevel! equ 0 (
    echo ✅ 获取作物详情 - 成功
) else (
    echo ❌ 获取作物详情 - 失败
    echo    响应: !RESPONSE!
)
echo.
)

echo [9/30] 搜索作物（关键词：番茄）...
for /f "delims=" %%i in ('curl.exe -s -X GET "%BASE_URL%/api/crops?searchKeyword=番茄^&pageNumber=1^&pageSize=10" -H "Authorization: Bearer !TOKEN!"') do set RESPONSE=%%i
echo !RESPONSE! | findstr "\"code\":0" >nul
if !errorlevel! equ 0 (
    echo ✅ 搜索作物 - 成功
) else (
    echo ❌ 搜索作物 - 失败
    echo    响应: !RESPONSE!
)
echo.

echo [10/30] 按状态筛选（生长中）...
for /f "delims=" %%i in ('curl.exe -s -X GET "%BASE_URL%/api/crops?status=Growing^&pageNumber=1^&pageSize=10" -H "Authorization: Bearer !TOKEN!"') do set RESPONSE=%%i
echo !RESPONSE! | findstr "\"code\":0" >nul
if !errorlevel! equ 0 (
    echo ✅ 按状态筛选 - 成功
) else (
    echo ❌ 按状态筛选 - 失败
    echo    响应: !RESPONSE!
)
echo.

if not "!NEW_CROP_ID!"=="" (
echo [11/30] 更新作物信息...
for /f "delims=" %%i in ('curl.exe -s -X PUT "%BASE_URL%/api/crops/!NEW_CROP_ID!" -H "Content-Type: application/json" -H "Authorization: Bearer !TOKEN!" -d "{\"name\":\"测试黄瓜（更新）\",\"location\":\"东阳台\"}"') do set RESPONSE=%%i
echo !RESPONSE! | findstr "\"code\":0" >nul
if !errorlevel! equ 0 (
    echo ✅ 更新作物信息 - 成功
) else (
    echo ❌ 更新作物信息 - 失败
    echo    响应: !RESPONSE!
)
echo.

echo [12/30] 更新作物状态（采收中）...
for /f "delims=" %%i in ('curl.exe -s -X PATCH "%BASE_URL%/api/crops/!NEW_CROP_ID!/status" -H "Content-Type: application/json" -H "Authorization: Bearer !TOKEN!" -d "{\"status\":\"Harvesting\"}"') do set RESPONSE=%%i
echo !RESPONSE! | findstr "\"code\":0" >nul
if !errorlevel! equ 0 (
    echo ✅ 更新作物状态 - 成功
) else (
    echo ❌ 更新作物状态 - 失败
    echo    响应: !RESPONSE!
)
echo.
)

echo --- 📋 养护任务模块 ---
echo.

if not "!NEW_CROP_ID!"=="" (
echo [13/30] 创建养护任务...
for /f "delims=" %%i in ('curl.exe -s -X POST "%BASE_URL%/api/cropcaretasks" -H "Content-Type: application/json" -H "Authorization: Bearer !TOKEN!" -d "{\"cropId\":\"!NEW_CROP_ID!\",\"taskType\":\"Water\",\"scheduledDate\":\"2026-06-17\",\"note\":\"浇水500ml\"}"') do set RESPONSE=%%i
echo !RESPONSE! | findstr "\"code\":0" >nul
if !errorlevel! equ 0 (
    echo ✅ 创建养护任务 - 成功 (201)
    for /f "tokens=2 delims=:{}, " %%a in ('echo !RESPONSE! ^| findstr /r "\"id\":\"[0-9a-fA-F-]*\""') do (
        if "!NEW_TASK_ID!"=="" set "NEW_TASK_ID=%%~a"
    )
    echo    任务ID: !NEW_TASK_ID!
) else (
    echo ❌ 创建养护任务 - 失败
    echo    响应: !RESPONSE!
)
echo.
)

echo [14/30] 获取任务列表...
for /f "delims=" %%i in ('curl.exe -s -X GET "%BASE_URL%/api/cropcaretasks?pageNumber=1^&pageSize=10" -H "Authorization: Bearer !TOKEN!"') do set RESPONSE=%%i
echo !RESPONSE! | findstr "\"code\":0" >nul
if !errorlevel! equ 0 (
    echo ✅ 获取任务列表 - 成功
) else (
    echo ❌ 获取任务列表 - 失败
    echo    响应: !RESPONSE!
)
echo.

if not "!NEW_CROP_ID!"=="" (
echo [15/30] 按作物查询任务...
for /f "delims=" %%i in ('curl.exe -s -X GET "%BASE_URL%/api/cropcaretasks?cropId=!NEW_CROP_ID!^&pageNumber=1^&pageSize=10" -H "Authorization: Bearer !TOKEN!"') do set RESPONSE=%%i
echo !RESPONSE! | findstr "\"code\":0" >nul
if !errorlevel! equ 0 (
    echo ✅ 按作物查询任务 - 成功
) else (
    echo ❌ 按作物查询任务 - 失败
    echo    响应: !RESPONSE!
)
echo.
)

if not "!NEW_TASK_ID!"=="" (
echo [16/30] 更新任务状态（完成）...
for /f "delims=" %%i in ('curl.exe -s -X PATCH "%BASE_URL%/api/cropcaretasks/!NEW_TASK_ID!/status" -H "Content-Type: application/json" -H "Authorization: Bearer !TOKEN!" -d "{\"status\":\"Completed\"}"') do set RESPONSE=%%i
echo !RESPONSE! | findstr "\"code\":0" >nul
if !errorlevel! equ 0 (
    echo ✅ 更新任务状态 - 成功
) else (
    echo ❌ 更新任务状态 - 失败
    echo    响应: !RESPONSE!
)
echo.

echo [17/30] 删除任务...
for /f "delims=" %%i in ('curl.exe -s -X DELETE "%BASE_URL%/api/cropcaretasks/!NEW_TASK_ID!" -H "Authorization: Bearer !TOKEN!"') do set RESPONSE=%%i
echo !RESPONSE! | findstr "\"code\":0" >nul
if !errorlevel! equ 0 (
    echo ✅ 删除任务 - 成功
) else (
    echo ❌ 删除任务 - 失败
    echo    响应: !RESPONSE!
)
echo.
)

echo --- 🍅 收成管理模块 ---
echo.

if not "!NEW_CROP_ID!"=="" (
echo [18/30] 创建收成记录...
for /f "delims=" %%i in ('curl.exe -s -X POST "%BASE_URL%/api/harvestrecords" -H "Content-Type: application/json" -H "Authorization: Bearer !TOKEN!" -d "{\"cropId\":\"!NEW_CROP_ID!\",\"harvestDate\":\"2026-06-15\",\"quantity\":0.5,\"unit\":\"kg\",\"qualityNote\":\"第一次采收\",\"photoUrl\":\"https://example.com/harvest.jpg\"}"') do set RESPONSE=%%i
echo !RESPONSE! | findstr "\"code\":0" >nul
if !errorlevel! equ 0 (
    echo ✅ 创建收成记录 - 成功 (201)
    for /f "tokens=2 delims=:{}, " %%a in ('echo !RESPONSE! ^| findstr /r "\"id\":\"[0-9a-fA-F-]*\""') do (
        if "!NEW_HARVEST_ID!"=="" set "NEW_HARVEST_ID=%%~a"
    )
    echo    收成记录ID: !NEW_HARVEST_ID!
) else (
    echo ❌ 创建收成记录 - 失败
    echo    响应: !RESPONSE!
)
echo.
)

echo [19/30] 获取收成列表...
for /f "delims=" %%i in ('curl.exe -s -X GET "%BASE_URL%/api/harvestrecords?pageNumber=1^&pageSize=10" -H "Authorization: Bearer !TOKEN!"') do set RESPONSE=%%i
echo !RESPONSE! | findstr "\"code\":0" >nul
if !errorlevel! equ 0 (
    echo ✅ 获取收成列表 - 成功
) else (
    echo ❌ 获取收成列表 - 失败
    echo    响应: !RESPONSE!
)
echo.

if not "!NEW_HARVEST_ID!"=="" (
echo [20/30] 获取收成详情...
for /f "delims=" %%i in ('curl.exe -s -X GET "%BASE_URL%/api/harvestrecords/!NEW_HARVEST_ID!" -H "Authorization: Bearer !TOKEN!"') do set RESPONSE=%%i
echo !RESPONSE! | findstr "\"code\":0" >nul
if !errorlevel! equ 0 (
    echo ✅ 获取收成详情 - 成功
) else (
    echo ❌ 获取收成详情 - 失败
    echo    响应: !RESPONSE!
)
echo.

echo [21/30] 更新收成记录...
for /f "delims=" %%i in ('curl.exe -s -X PUT "%BASE_URL%/api/harvestrecords/!NEW_HARVEST_ID!" -H "Content-Type: application/json" -H "Authorization: Bearer !TOKEN!" -d "{\"quantity\":0.6,\"qualityNote\":\"第一次采收，更新\"}"') do set RESPONSE=%%i
echo !RESPONSE! | findstr "\"code\":0" >nul
if !errorlevel! equ 0 (
    echo ✅ 更新收成记录 - 成功
) else (
    echo ❌ 更新收成记录 - 失败
    echo    响应: !RESPONSE!
)
echo.
)

echo --- 🐛 病虫害管理模块 ---
echo.

if not "!NEW_CROP_ID!"=="" (
echo [22/30] 创建病虫害记录...
for /f "delims=" %%i in ('curl.exe -s -X POST "%BASE_URL%/api/pestrecords" -H "Content-Type: application/json" -H "Authorization: Bearer !TOKEN!" -d "{\"cropId\":\"!NEW_CROP_ID!\",\"issueType\":\"蚜虫\",\"symptoms\":\"叶片背面有绿色小虫子\",\"treatment\":\"喷洒肥皂水\",\"detectedDate\":\"2026-06-15\"}"') do set RESPONSE=%%i
echo !RESPONSE! | findstr "\"code\":0" >nul
if !errorlevel! equ 0 (
    echo ✅ 创建病虫害记录 - 成功 (201)
    for /f "tokens=2 delims=:{}, " %%a in ('echo !RESPONSE! ^| findstr /r "\"id\":\"[0-9a-fA-F-]*\""') do (
        if "!NEW_PEST_ID!"=="" set "NEW_PEST_ID=%%~a"
    )
    echo    病虫害记录ID: !NEW_PEST_ID!
) else (
    echo ❌ 创建病虫害记录 - 失败
    echo    响应: !RESPONSE!
)
echo.
)

echo [23/30] 获取病虫害列表...
for /f "delims=" %%i in ('curl.exe -s -X GET "%BASE_URL%/api/pestrecords?pageNumber=1^&pageSize=10" -H "Authorization: Bearer !TOKEN!"') do set RESPONSE=%%i
echo !RESPONSE! | findstr "\"code\":0" >nul
if !errorlevel! equ 0 (
    echo ✅ 获取病虫害列表 - 成功
) else (
    echo ❌ 获取病虫害列表 - 失败
    echo    响应: !RESPONSE!
)
echo.

if not "!NEW_PEST_ID!"=="" (
echo [24/30] 更新病虫害状态（已解决）...
for /f "delims=" %%i in ('curl.exe -s -X PATCH "%BASE_URL%/api/pestrecords/!NEW_PEST_ID!/status" -H "Content-Type: application/json" -H "Authorization: Bearer !TOKEN!" -d "{\"status\":\"Resolved\"}"') do set RESPONSE=%%i
echo !RESPONSE! | findstr "\"code\":0" >nul
if !errorlevel! equ 0 (
    echo ✅ 更新病虫害状态 - 成功
) else (
    echo ❌ 更新病虫害状态 - 失败
    echo    响应: !RESPONSE!
)
echo.

echo [25/30] 更新病虫害记录...
for /f "delims=" %%i in ('curl.exe -s -X PUT "%BASE_URL%/api/pestrecords/!NEW_PEST_ID!" -H "Content-Type: application/json" -H "Authorization: Bearer !TOKEN!" -d "{\"treatment\":\"喷洒肥皂水，连续3天\"}"') do set RESPONSE=%%i
echo !RESPONSE! | findstr "\"code\":0" >nul
if !errorlevel! equ 0 (
    echo ✅ 更新病虫害记录 - 成功
) else (
    echo ❌ 更新病虫害记录 - 失败
    echo    响应: !RESPONSE!
)
echo.
)

echo --- 📊 统计分析模块 ---
echo.

echo [26/30] 获取总览统计...
for /f "delims=" %%i in ('curl.exe -s -X GET "%BASE_URL%/api/stats/overview" -H "Authorization: Bearer !TOKEN!"') do set RESPONSE=%%i
echo !RESPONSE! | findstr "\"code\":0" >nul
if !errorlevel! equ 0 (
    echo ✅ 获取总览统计 - 成功
) else (
    echo ❌ 获取总览统计 - 失败
    echo    响应: !RESPONSE!
)
echo.

echo [27/30] 获取趋势统计（30天）...
for /f "delims=" %%i in ('curl.exe -s -X GET "%BASE_URL%/api/stats/trend?days=30" -H "Authorization: Bearer !TOKEN!"') do set RESPONSE=%%i
echo !RESPONSE! | findstr "\"code\":0" >nul
if !errorlevel! equ 0 (
    echo ✅ 获取趋势统计 - 成功
) else (
    echo ❌ 获取趋势统计 - 失败
    echo    响应: !RESPONSE!
)
echo.

echo --- 🧹 数据清理 ---
echo.

if not "!NEW_PEST_ID!"=="" (
echo [28/30] 删除病虫害记录...
for /f "delims=" %%i in ('curl.exe -s -X DELETE "%BASE_URL%/api/pestrecords/!NEW_PEST_ID!" -H "Authorization: Bearer !TOKEN!"') do set RESPONSE=%%i
echo !RESPONSE! | findstr "\"code\":0" >nul
if !errorlevel! equ 0 (
    echo ✅ 删除病虫害记录 - 成功
) else (
    echo ❌ 删除病虫害记录 - 失败
    echo    响应: !RESPONSE!
)
echo.
)

if not "!NEW_HARVEST_ID!"=="" (
echo [29/30] 删除收成记录...
for /f "delims=" %%i in ('curl.exe -s -X DELETE "%BASE_URL%/api/harvestrecords/!NEW_HARVEST_ID!" -H "Authorization: Bearer !TOKEN!"') do set RESPONSE=%%i
echo !RESPONSE! | findstr "\"code\":0" >nul
if !errorlevel! equ 0 (
    echo ✅ 删除收成记录 - 成功
) else (
    echo ❌ 删除收成记录 - 失败
    echo    响应: !RESPONSE!
)
echo.
)

if not "!NEW_CROP_ID!"=="" (
echo [30/30] 删除作物...
for /f "delims=" %%i in ('curl.exe -s -X DELETE "%BASE_URL%/api/crops/!NEW_CROP_ID!" -H "Authorization: Bearer !TOKEN!"') do set RESPONSE=%%i
echo !RESPONSE! | findstr "\"code\":0" >nul
if !errorlevel! equ 0 (
    echo ✅ 删除作物 - 成功
) else (
    echo ❌ 删除作物 - 失败
    echo    响应: !RESPONSE!
)
echo.
)

echo ========================================
echo   测试完成！
echo ========================================
echo.
pause
