@echo off
setlocal

set TOKEN=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjExMTExMTExLTExMTEtMTExMS0xMTExLTExMTExMTExMTExMSIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWUiOiJnYXJkZW5lcjFfdXBkYXRlZCIsImV4cCI6MTc4MTU3NTcyMCwiaXNzIjoiQmFsY29ueUZhcm0iLCJhdWQiOiJCYWxjb255RmFybSJ9.NadvoIa1x3rMCI6YnZ9qAx0zNKIfnBRF-XJjQ93hRA4

echo === Testing PATCH /api/crops/{id}/status ===
echo Request body:
type status.json
echo.
echo Response:
curl.exe -v -X PATCH "http://localhost:8085/api/crops/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/status" ^
  -H "Content-Type: application/json" ^
  -H "Authorization: Bearer %TOKEN%" ^
  -d "@status.json"

echo.
echo === Testing POST /api/cropcaretasks ===
echo Request body:
type createtask.json
echo.
echo Response:
curl.exe -v -X POST "http://localhost:8085/api/cropcaretasks" ^
  -H "Content-Type: application/json" ^
  -H "Authorization: Bearer %TOKEN%" ^
  -d "@createtask.json"

endlocal
