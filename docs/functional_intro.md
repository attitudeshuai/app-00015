# 功能说明文档

## 1. 业务背景与解决的问题

### 1.1 业务背景
随着城市化进程加快，越来越多的城市居民渴望拥有自己的小菜园。阳台种菜作为一种微型农业方式，既满足了人们对健康食材的追求，也提供了放松身心的休闲方式。然而，城市阳台空间有限、光照条件各异、种植经验不足等问题，使得许多初学者难以获得理想的收成。

### 1.2 解决的问题
- **种植经验不足** - 新手不知道什么时候浇水、施肥、修剪
- **养护记忆混乱** - 记不清上次浇水施肥的时间
- **病虫害识别难** - 不知道作物得了什么病，如何治疗
- **缺乏成就感** - 没有记录和统计，看不到自己的种植成果
- **数据分散** - 没有统一的平台管理多种作物的生长数据

## 2. 用户角色与核心用例

### 2.1 用户角色

| 角色 | 描述 | 核心需求 |
|------|------|----------|
| 新手园艺者 | 刚开始接触阳台种菜的用户 | 简单的操作指引、养护提醒 |
| 阳台种菜爱好者 | 有一定经验，种植多种作物 | 详细的生长记录、数据分析 |
| 家庭自给自足尝试者 | 希望通过阳台种植获得部分食材 | 收成统计、季节性建议 |

### 2.2 核心用例

```
┌─────────────────────────────────────────────────────────┐
│                    阳台种菜助手系统                         │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  用户注册/登录  ───►  管理个人作物  ───►  记录养护任务   │
│        │                │                │              │
│        │                │                │              │
│        ▼                ▼                ▼              │
│  获取当前用户信息    追踪生长状态    设置浇水施肥提醒   │
│                                                         │
│        │                │                │              │
│        │                │                │              │
│        ▼                ▼                ▼              │
│  更新个人资料      记录病虫害问题    记录每次收成      │
│                                                         │
│        │                │                │              │
│        │                │                │              │
│        └────────────────┴────────────────┘              │
│                         │                               │
│                         ▼                               │
│                   查看统计报表                           │
│                   (总览/趋势/排行)                       │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

## 3. 功能模块详细说明

### 3.1 用户认证模块
- **用户注册**: 支持用户名+邮箱+密码注册，用户名和邮箱唯一
- **用户登录**: 支持用户名或邮箱登录，返回 JWT Token
- **个人信息管理**: 获取和更新当前登录用户信息
- **安全机制**: 使用 PBKDF2 算法进行密码哈希，JWT Token 有效期 2 小时

### 3.2 作物管理模块
- **作物 CRUD**: 完整的增删改查操作
- **作物状态追踪**: 支持 Growing（生长中）、Harvesting（可收获）、Finished（已结束）三种状态
- **作物属性**: 记录名称、品种、种植日期、阳台位置、容器类型、照片等
- **权限控制**: 只有作物所有者可以修改和删除
- **分页搜索**: 支持按状态、位置、容器类型筛选，支持关键词搜索

### 3.3 养护任务管理模块
- **任务类型**: 浇水(Water)、施肥(Fertilize)、修剪(Prune)、换盆(Repot)
- **任务状态**: 待处理(Pending)、进行中(InProgress)、已完成(Completed)、已取消(Cancelled)
- **任务调度**: 设置计划日期，记录完成日期
- **批量管理**: 支持分页、筛选、搜索

### 3.4 收成管理模块
- **收成记录**: 记录收获日期、数量、单位、质量评价、照片
- **收成统计**: 自动累计每个作物的总收成
- **历史追溯**: 查看所有历史收成记录

### 3.5 病虫害管理模块
- **问题记录**: 记录问题类型、症状、治疗方案
- **状态追踪**: 已发现(Detected)、治疗中(Treating)、已解决(Resolved)
- **治疗记录**: 记录发现日期和解决日期，追踪治疗效果

### 3.6 统计与搜索模块
- **总览统计**: 作物总数、各状态作物数量、任务统计、收成总量、活跃病虫害数
- **趋势统计**: 按日/周统计新增作物、完成任务、收成数量的趋势
- **排行榜**: 按收成总量对作物进行排行
- **全局搜索**: 跨模块搜索作物、任务、收成、病虫害记录

## 4. 数据库 ER 图文字描述

### 4.1 表关系

```
Users (用户表)
  ├─ Id (PK, Guid)
  ├─ Username (唯一)
  ├─ Email (唯一)
  ├─ PasswordHash
  ├─ Avatar
  ├─ CreatedAt
  └─ UpdatedAt
      │
      │ 1:N
      │
      ▼
Crops (作物表)
  ├─ Id (PK, Guid)
  ├─ UserId (FK → Users.Id)
  ├─ Name
  ├─ Variety
  ├─ PlantingDate
  ├─ Location
  ├─ ContainerType
  ├─ Status (枚举)
  ├─ PhotoUrl
  └─ CreatedAt
      │
      ├─ 1:N → CropCareTasks
      ├─ 1:N → HarvestRecords
      └─ 1:N → PestRecords

CropCareTasks (养护任务表)
  ├─ Id (PK, Guid)
  ├─ CropId (FK → Crops.Id)
  ├─ TaskType (枚举)
  ├─ ScheduledDate
  ├─ CompletedDate
  ├─ Status (枚举)
  └─ Note

HarvestRecords (收成记录表)
  ├─ Id (PK, Guid)
  ├─ CropId (FK → Crops.Id)
  ├─ HarvestDate
  ├─ Quantity (decimal)
  ├─ Unit
  ├─ QualityNote
  └─ PhotoUrl

PestRecords (病虫害记录表)
  ├─ Id (PK, Guid)
  ├─ CropId (FK → Crops.Id)
  ├─ IssueType
  ├─ Symptoms
  ├─ Treatment
  ├─ DetectedDate
  ├─ ResolvedDate
  └─ Status (枚举)
```

### 4.2 关系说明
- **Users → Crops**: 一对多关系，一个用户可以有多个作物，删除用户会级联删除其所有作物
- **Crops → CropCareTasks**: 一对多关系，一个作物可以有多个养护任务
- **Crops → HarvestRecords**: 一对多关系，一个作物可以有多个收成记录
- **Crops → PestRecords**: 一对多关系，一个作物可以有多个病虫害记录
- 所有外键关系均设置为级联删除，删除作物会同时删除相关的任务、收成和病虫害记录

## 5. 关键业务规则

### 5.1 状态流转规则

#### 作物状态流转
```
Growing (生长中)
    ↓
Harvesting (可收获)  ←→  Growing
    ↓
Finished (已结束)
```
- 作物创建后默认为 Growing 状态
- 进入收获期后可改为 Harvesting 状态
- 采收结束后改为 Finished 状态
- Finished 为终态，不可逆转

#### 养护任务状态流转
```
Pending (待处理) ←→ InProgress (进行中)
    ↓                    ↓
Completed (已完成)  Cancelled (已取消)
```
- Completed 和 Cancelled 为终态

#### 病虫害状态流转
```
Detected (已发现) → Treating (治疗中) → Resolved (已解决)
```

### 5.2 权限规则
- **公开接口**: 注册、登录、作物列表（匿名只能看基本信息）、作物详情（匿名只能看基本信息）
- **需要认证**: 所有写操作、获取个人信息、获取我的作物
- **数据隔离**: 用户只能修改和删除自己的作物、任务、收成和病虫害记录
- **认证失败**: 返回 401 Unauthorized
- **权限不足**: 返回 403 Forbidden

### 5.3 时间计算逻辑
- 所有时间存储为 UTC 时间
- 作物生长天数 = 当前日期 - 种植日期
- 任务延迟 = 当前日期 - 计划日期（当状态为 Pending 且当前日期 > 计划日期）
- 病虫害持续天数 = 解决日期 - 发现日期（已解决）或 当前日期 - 发现日期（未解决）

## 6. 接口调用示例

### 6.1 用户注册

**请求**:
```http
POST /api/auth/register
Content-Type: application/json

{
  "username": "greenfingers",
  "email": "green@example.com",
  "password": "mypassword123",
  "avatar": "https://example.com/avatar.jpg"
}
```

**响应**:
```json
{
  "code": 200,
  "message": "注册成功",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "user": {
      "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "username": "greenfingers",
      "email": "green@example.com",
      "avatar": "https://example.com/avatar.jpg",
      "createdAt": "2024-01-15T10:30:00Z"
    }
  }
}
```

### 6.2 创建作物

**请求**:
```http
POST /api/crops
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "name": "小番茄",
  "variety": "千禧樱桃番茄",
  "plantingDate": "2024-01-01T00:00:00Z",
  "location": "南阳台",
  "containerType": "塑料花盆",
  "status": "Growing",
  "photoUrl": "https://example.com/tomato.jpg"
}
```

**响应**:
```json
{
  "code": 200,
  "message": "创建成功",
  "data": {
    "id": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    "userId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "name": "小番茄",
    "variety": "千禧樱桃番茄",
    "plantingDate": "2024-01-01T00:00:00Z",
    "location": "南阳台",
    "containerType": "塑料花盆",
    "status": "Growing",
    "photoUrl": "https://example.com/tomato.jpg",
    "createdAt": "2024-01-15T10:35:00Z",
    "ownerUsername": "greenfingers"
  }
}
```

### 6.3 获取总览统计

**请求**:
```http
GET /api/stats/overview
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**响应**:
```json
{
  "code": 200,
  "message": "操作成功",
  "data": {
    "totalCrops": 5,
    "growingCrops": 3,
    "harvestingCrops": 1,
    "finishedCrops": 1,
    "totalTasks": 12,
    "pendingTasks": 3,
    "completedTasks": 8,
    "totalHarvestRecords": 4,
    "totalHarvestQuantity": 0.58,
    "activePestIssues": 2
  }
}
```
