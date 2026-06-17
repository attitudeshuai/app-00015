# 阳台种菜助手 (BalconyFarm)

为城市阳台种菜爱好者提供作物管理、种植日历、浇水施肥提醒与收成记录的后端 API 服务。

## 功能亮点

- 🌱 **作物全生命周期管理** - 从播种到收获，追踪作物生长的每个阶段
- 📋 **智能养护任务** - 自动生成浇水、施肥、修剪等养护提醒
- 🏆 **收成记录与排行** - 记录每次收获，家庭收成排行榜激励种植热情
- 🐛 **病虫害管理** - 拍照记录病虫害问题，追踪治疗效果
- 📊 **数据看板** - 多维度统计分析，了解种植趋势与成果
- 🔐 **JWT 身份认证** - 安全的用户认证与数据隔离

## 技术栈

- **后端框架**: .NET Core 8.0 (ASP.NET Core Web API)
- **数据库**: MySQL 8.0
- **ORM**: Entity Framework Core 8.0 (Pomelo.EntityFrameworkCore.MySql)
- **认证**: JWT Bearer Token
- **API文档**: Swagger / OpenAPI
- **容器化**: Docker + Docker Compose
- **数据验证**: FluentValidation
- **对象映射**: Mapster
- **日志**: Serilog
- **测试**: xUnit

## 目录结构

```
BalconyFarm/
├── src/
│   ├── BalconyFarm.Api/          # API 层 - Controllers、中间件、Program.cs
│   │   ├── Controllers/
│   │   ├── Middleware/
│   │   └── Extensions/
│   ├── BalconyFarm.Application/  # 应用层 - DTO、Services、Validators
│   │   ├── DTOs/
│   │   ├── Services/
│   │   ├── Validators/
│   │   ├── Models/
│   │   └── Extensions/
│   ├── BalconyFarm.Domain/       # 领域层 - Entities、Enums、Interfaces
│   │   ├── Entities/
│   │   ├── Enums/
│   │   └── Interfaces/
│   └── BalconyFarm.Infrastructure/ # 基础设施层 - DbContext、Repositories、Services
│       ├── Data/
│       ├── Repositories/
│       ├── Services/
│       └── Extensions/
├── tests/
│   └── BalconyFarm.Tests/        # 单元测试和集成测试
├── docs/
│   └── functional_intro.md       # 功能说明文档
├── Dockerfile
├── docker-compose.yml
├── postman_collection.json
└── README.md
```

## 快速开始

### 1. Docker 一键启动

```bash
# 克隆并进入项目目录
git clone <repo-url>
cd BalconyFarm

# Docker 启动（应用 + MySQL + Adminer）
docker-compose up --build -d

# 查看日志
docker-compose logs -f app

# 验证服务健康
curl http://localhost:8085/health
```

### 2. 本地开发运行

```bash
# 安装依赖
dotnet restore

# 构建
dotnet build

# 运行（需要本地 MySQL 服务）
dotnet run --project src/BalconyFarm.Api/BalconyFarm.Api.csproj
```

### 3. 访问接口文档

启动服务后访问 Swagger UI:
- 本地开发: http://localhost:5000/swagger
- Docker 部署: http://localhost:8085/swagger

### 4. 数据库管理

Adminer 已包含在 docker-compose 中:
- 地址: http://localhost:8080
- 服务器: mysql
- 用户名: app_user
- 密码: app_pass
- 数据库: balconyfarm

## 测试

### Postman 测试集合

1. 打开 Postman
2. 点击 Import 导入 `postman_collection.json`
3. 配置环境变量 `base_url` (默认: `http://localhost:8085`)
4. 按顺序执行测试：注册 → 登录 → 各模块 CRUD → 统计接口

### 自动化测试

```bash
# 执行所有测试
dotnet test

# 执行测试并生成代码覆盖率报告
dotnet test --collect:"XPlat Code Coverage"
```

## 核心 API 接口

### 认证模块
- `POST /api/auth/register` - 用户注册
- `POST /api/auth/login` - 用户登录
- `GET /api/auth/me` - 获取当前用户信息
- `PUT /api/auth/me` - 更新个人信息

### 作物管理
- `GET /api/crops` - 获取作物列表
- `POST /api/crops` - 创建作物
- `GET /api/crops/{id}` - 获取作物详情
- `PUT /api/crops/{id}` - 更新作物
- `DELETE /api/crops/{id}` - 删除作物
- `PATCH /api/crops/{id}/status` - 修改作物状态
- `GET /api/crops/mine` - 获取我的作物

### 养护任务
- `GET /api/cropcaretasks` - 获取任务列表
- `POST /api/cropcaretasks` - 创建任务
- `GET /api/cropcaretasks/{id}` - 获取任务详情
- `PUT /api/cropcaretasks/{id}` - 更新任务
- `DELETE /api/cropcaretasks/{id}` - 删除任务
- `PATCH /api/cropcaretasks/{id}/status` - 修改任务状态

### 收成管理
- `GET /api/harvestrecords` - 获取收成列表
- `POST /api/harvestrecords` - 创建收成记录
- `GET /api/harvestrecords/{id}` - 获取收成详情
- `PUT /api/harvestrecords/{id}` - 更新收成记录
- `DELETE /api/harvestrecords/{id}` - 删除收成记录

### 病虫害管理
- `GET /api/pestrecords` - 获取病虫害列表
- `POST /api/pestrecords` - 创建病虫害记录
- `GET /api/pestrecords/{id}` - 获取病虫害详情
- `PUT /api/pestrecords/{id}` - 更新病虫害记录
- `DELETE /api/pestrecords/{id}` - 删除病虫害记录
- `PATCH /api/pestrecords/{id}/status` - 修改病虫害状态

### 统计模块
- `GET /api/stats/overview` - 总览统计
- `GET /api/stats/trend` - 趋势统计

## 演示账号

容器启动后自动创建以下测试账号：

| 用户名 | 邮箱 | 密码 |
|--------|------|------|
| gardener1 | gardener1@example.com | password123 |
| plantlover | plantlover@example.com | password123 |
| urbanfarmer | urbanfarmer@example.com | password123 |

## 停止服务

```bash
# 停止并保留数据
docker-compose down

# 停止并删除所有数据（包括数据库卷）
docker-compose down -v
```

## 贡献

欢迎提交 Issue 和 Pull Request！

## 许可

MIT License
