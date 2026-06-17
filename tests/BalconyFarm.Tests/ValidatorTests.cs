using BalconyFarm.Application.DTOs;
using BalconyFarm.Application.Validators;
using BalconyFarm.Domain.Enums;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Xunit;
using TaskStatus = BalconyFarm.Domain.Enums.TaskStatus;

namespace BalconyFarm.Tests;

public class ValidatorTests
{
    #region AuthValidators

    public class RegisterRequestDtoValidatorTests
    {
        private readonly RegisterRequestDtoValidator _validator;

        public RegisterRequestDtoValidatorTests()
        {
            _validator = new RegisterRequestDtoValidator();
        }

        [Fact]
        public void Validate_WhenAllFieldsAreValid_ShouldPass()
        {
            var dto = new RegisterRequestDto
            {
                Username = "validuser",
                Email = "user@example.com",
                Password = "password123"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void Validate_WhenUsernameIsEmpty_ShouldFail()
        {
            var dto = new RegisterRequestDto
            {
                Username = string.Empty,
                Email = "user@example.com",
                Password = "password123"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "用户名不能为空");
        }

        [Theory]
        [InlineData("ab")]
        [InlineData("a")]
        public void Validate_WhenUsernameIsTooShort_ShouldFail(string username)
        {
            var dto = new RegisterRequestDto
            {
                Username = username,
                Email = "user@example.com",
                Password = "password123"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "用户名长度必须在3到50个字符之间");
        }

        [Fact]
        public void Validate_WhenUsernameIsTooLong_ShouldFail()
        {
            var dto = new RegisterRequestDto
            {
                Username = new string('a', 51),
                Email = "user@example.com",
                Password = "password123"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "用户名长度必须在3到50个字符之间");
        }

        [Fact]
        public void Validate_WhenEmailIsEmpty_ShouldFail()
        {
            var dto = new RegisterRequestDto
            {
                Username = "validuser",
                Email = string.Empty,
                Password = "password123"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "邮箱不能为空");
        }

        [Theory]
        [InlineData("invalid-email")]
        [InlineData("user@")]
        [InlineData("@example.com")]
        public void Validate_WhenEmailFormatIsInvalid_ShouldFail(string email)
        {
            var dto = new RegisterRequestDto
            {
                Username = "validuser",
                Email = email,
                Password = "password123"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "邮箱格式不正确");
        }

        [Fact]
        public void Validate_WhenEmailIsTooLong_ShouldFail()
        {
            var dto = new RegisterRequestDto
            {
                Username = "validuser",
                Email = new string('a', 90) + "@example.com",
                Password = "password123"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "邮箱长度不能超过100个字符");
        }

        [Fact]
        public void Validate_WhenPasswordIsEmpty_ShouldFail()
        {
            var dto = new RegisterRequestDto
            {
                Username = "validuser",
                Email = "user@example.com",
                Password = string.Empty
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "密码不能为空");
        }

        [Theory]
        [InlineData("12345")]
        [InlineData("1234")]
        [InlineData("1")]
        public void Validate_WhenPasswordIsTooShort_ShouldFail(string password)
        {
            var dto = new RegisterRequestDto
            {
                Username = "validuser",
                Email = "user@example.com",
                Password = password
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "密码长度至少为6个字符");
        }
    }

    public class LoginRequestDtoValidatorTests
    {
        private readonly LoginRequestDtoValidator _validator;

        public LoginRequestDtoValidatorTests()
        {
            _validator = new LoginRequestDtoValidator();
        }

        [Fact]
        public void Validate_WhenAllFieldsAreValid_ShouldPass()
        {
            var dto = new LoginRequestDto
            {
                UsernameOrEmail = "user@example.com",
                Password = "password123"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void Validate_WhenUsernameOrEmailIsEmpty_ShouldFail()
        {
            var dto = new LoginRequestDto
            {
                UsernameOrEmail = string.Empty,
                Password = "password123"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "用户名或邮箱不能为空");
        }

        [Fact]
        public void Validate_WhenPasswordIsEmpty_ShouldFail()
        {
            var dto = new LoginRequestDto
            {
                UsernameOrEmail = "user@example.com",
                Password = string.Empty
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "密码不能为空");
        }
    }

    public class UpdateUserRequestDtoValidatorTests
    {
        private readonly UpdateUserRequestDtoValidator _validator;

        public UpdateUserRequestDtoValidatorTests()
        {
            _validator = new UpdateUserRequestDtoValidator();
        }

        [Fact]
        public void Validate_WhenAllFieldsAreNull_ShouldPass()
        {
            var dto = new UpdateUserRequestDto();

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void Validate_WhenUsernameIsValid_ShouldPass()
        {
            var dto = new UpdateUserRequestDto
            {
                Username = "newusername"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("ab")]
        [InlineData("a")]
        public void Validate_WhenUsernameIsTooShort_ShouldFail(string username)
        {
            var dto = new UpdateUserRequestDto
            {
                Username = username
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "用户名长度必须在3到50个字符之间");
        }

        [Fact]
        public void Validate_WhenUsernameIsTooLong_ShouldFail()
        {
            var dto = new UpdateUserRequestDto
            {
                Username = new string('a', 51)
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "用户名长度必须在3到50个字符之间");
        }

        [Fact]
        public void Validate_WhenEmailIsValid_ShouldPass()
        {
            var dto = new UpdateUserRequestDto
            {
                Email = "new@example.com"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("invalid-email")]
        [InlineData("user@")]
        public void Validate_WhenEmailFormatIsInvalid_ShouldFail(string email)
        {
            var dto = new UpdateUserRequestDto
            {
                Email = email
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "邮箱格式不正确");
        }

        [Fact]
        public void Validate_WhenEmailIsTooLong_ShouldFail()
        {
            var dto = new UpdateUserRequestDto
            {
                Email = new string('a', 90) + "@example.com"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "邮箱长度不能超过100个字符");
        }
    }

    #endregion

    #region CropValidators

    public class CreateCropRequestDtoValidatorTests
    {
        private readonly CreateCropRequestDtoValidator _validator;

        public CreateCropRequestDtoValidatorTests()
        {
            _validator = new CreateCropRequestDtoValidator();
        }

        [Fact]
        public void Validate_WhenAllFieldsAreValid_ShouldPass()
        {
            var dto = new CreateCropRequestDto
            {
                Name = "番茄",
                Variety = "樱桃番茄",
                PlantingDate = DateTime.UtcNow.AddDays(-10),
                Location = "南阳台",
                ContainerType = "陶盆"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void Validate_WhenNameIsEmpty_ShouldFail()
        {
            var dto = new CreateCropRequestDto
            {
                Name = string.Empty,
                Variety = "樱桃番茄",
                PlantingDate = DateTime.UtcNow.AddDays(-10),
                Location = "南阳台",
                ContainerType = "陶盆"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "作物名称不能为空");
        }

        [Fact]
        public void Validate_WhenNameIsTooLong_ShouldFail()
        {
            var dto = new CreateCropRequestDto
            {
                Name = new string('a', 101),
                Variety = "樱桃番茄",
                PlantingDate = DateTime.UtcNow.AddDays(-10),
                Location = "南阳台",
                ContainerType = "陶盆"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "作物名称长度不能超过100个字符");
        }

        [Fact]
        public void Validate_WhenVarietyIsEmpty_ShouldFail()
        {
            var dto = new CreateCropRequestDto
            {
                Name = "番茄",
                Variety = string.Empty,
                PlantingDate = DateTime.UtcNow.AddDays(-10),
                Location = "南阳台",
                ContainerType = "陶盆"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "作物品种不能为空");
        }

        [Fact]
        public void Validate_WhenVarietyIsTooLong_ShouldFail()
        {
            var dto = new CreateCropRequestDto
            {
                Name = "番茄",
                Variety = new string('a', 101),
                PlantingDate = DateTime.UtcNow.AddDays(-10),
                Location = "南阳台",
                ContainerType = "陶盆"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "作物品种长度不能超过100个字符");
        }

        [Fact]
        public void Validate_WhenPlantingDateIsEmpty_ShouldFail()
        {
            var dto = new CreateCropRequestDto
            {
                Name = "番茄",
                Variety = "樱桃番茄",
                PlantingDate = default,
                Location = "南阳台",
                ContainerType = "陶盆"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "种植日期不能为空");
        }

        [Fact]
        public void Validate_WhenPlantingDateIsInFuture_ShouldFail()
        {
            var dto = new CreateCropRequestDto
            {
                Name = "番茄",
                Variety = "樱桃番茄",
                PlantingDate = DateTime.UtcNow.AddDays(10),
                Location = "南阳台",
                ContainerType = "陶盆"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "种植日期不能晚于当前日期");
        }

        [Fact]
        public void Validate_WhenLocationIsEmpty_ShouldFail()
        {
            var dto = new CreateCropRequestDto
            {
                Name = "番茄",
                Variety = "樱桃番茄",
                PlantingDate = DateTime.UtcNow.AddDays(-10),
                Location = string.Empty,
                ContainerType = "陶盆"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "阳台位置不能为空");
        }

        [Fact]
        public void Validate_WhenLocationIsTooLong_ShouldFail()
        {
            var dto = new CreateCropRequestDto
            {
                Name = "番茄",
                Variety = "樱桃番茄",
                PlantingDate = DateTime.UtcNow.AddDays(-10),
                Location = new string('a', 201),
                ContainerType = "陶盆"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "阳台位置长度不能超过200个字符");
        }

        [Fact]
        public void Validate_WhenContainerTypeIsEmpty_ShouldFail()
        {
            var dto = new CreateCropRequestDto
            {
                Name = "番茄",
                Variety = "樱桃番茄",
                PlantingDate = DateTime.UtcNow.AddDays(-10),
                Location = "南阳台",
                ContainerType = string.Empty
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "容器类型不能为空");
        }

        [Fact]
        public void Validate_WhenContainerTypeIsTooLong_ShouldFail()
        {
            var dto = new CreateCropRequestDto
            {
                Name = "番茄",
                Variety = "樱桃番茄",
                PlantingDate = DateTime.UtcNow.AddDays(-10),
                Location = "南阳台",
                ContainerType = new string('a', 101)
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "容器类型长度不能超过100个字符");
        }
    }

    public class UpdateCropRequestDtoValidatorTests
    {
        private readonly UpdateCropRequestDtoValidator _validator;

        public UpdateCropRequestDtoValidatorTests()
        {
            _validator = new UpdateCropRequestDtoValidator();
        }

        [Fact]
        public void Validate_WhenAllFieldsAreNull_ShouldPass()
        {
            var dto = new UpdateCropRequestDto();

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void Validate_WhenNameIsValid_ShouldPass()
        {
            var dto = new UpdateCropRequestDto
            {
                Name = "新番茄"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WhenNameIsTooLong_ShouldFail()
        {
            var dto = new UpdateCropRequestDto
            {
                Name = new string('a', 101)
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "作物名称长度不能超过100个字符");
        }

        [Fact]
        public void Validate_WhenVarietyIsValid_ShouldPass()
        {
            var dto = new UpdateCropRequestDto
            {
                Variety = "新品种"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WhenVarietyIsTooLong_ShouldFail()
        {
            var dto = new UpdateCropRequestDto
            {
                Variety = new string('a', 101)
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "作物品种长度不能超过100个字符");
        }

        [Fact]
        public void Validate_WhenPlantingDateIsValid_ShouldPass()
        {
            var dto = new UpdateCropRequestDto
            {
                PlantingDate = DateTime.UtcNow.AddDays(-5)
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WhenPlantingDateIsInFuture_ShouldFail()
        {
            var dto = new UpdateCropRequestDto
            {
                PlantingDate = DateTime.UtcNow.AddDays(10)
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "种植日期不能晚于当前日期");
        }

        [Fact]
        public void Validate_WhenLocationIsValid_ShouldPass()
        {
            var dto = new UpdateCropRequestDto
            {
                Location = "北阳台"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WhenLocationIsTooLong_ShouldFail()
        {
            var dto = new UpdateCropRequestDto
            {
                Location = new string('a', 201)
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "阳台位置长度不能超过200个字符");
        }

        [Fact]
        public void Validate_WhenContainerTypeIsValid_ShouldPass()
        {
            var dto = new UpdateCropRequestDto
            {
                ContainerType = "塑料盆"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WhenContainerTypeIsTooLong_ShouldFail()
        {
            var dto = new UpdateCropRequestDto
            {
                ContainerType = new string('a', 101)
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "容器类型长度不能超过100个字符");
        }
    }

    #endregion

    #region CropCareTaskValidators

    public class CreateCropCareTaskRequestDtoValidatorTests
    {
        private readonly CreateCropCareTaskRequestDtoValidator _validator;

        public CreateCropCareTaskRequestDtoValidatorTests()
        {
            _validator = new CreateCropCareTaskRequestDtoValidator();
        }

        [Fact]
        public void Validate_WhenAllFieldsAreValid_ShouldPass()
        {
            var dto = new CreateCropCareTaskRequestDto
            {
                CropId = Guid.NewGuid(),
                TaskType = TaskType.Water,
                ScheduledDate = DateTime.UtcNow.AddDays(1),
                Note = "记得浇透水"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void Validate_WhenCropIdIsEmpty_ShouldFail()
        {
            var dto = new CreateCropCareTaskRequestDto
            {
                CropId = Guid.Empty,
                TaskType = TaskType.Water,
                ScheduledDate = DateTime.UtcNow.AddDays(1)
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "作物ID不能为空");
        }

        [Fact]
        public void Validate_WhenTaskTypeIsInvalid_ShouldFail()
        {
            var dto = new CreateCropCareTaskRequestDto
            {
                CropId = Guid.NewGuid(),
                TaskType = (TaskType)999,
                ScheduledDate = DateTime.UtcNow.AddDays(1)
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "任务类型无效");
        }

        [Fact]
        public void Validate_WhenScheduledDateIsEmpty_ShouldFail()
        {
            var dto = new CreateCropCareTaskRequestDto
            {
                CropId = Guid.NewGuid(),
                TaskType = TaskType.Water,
                ScheduledDate = default
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "计划日期不能为空");
        }

        [Fact]
        public void Validate_WhenNoteIsTooLong_ShouldFail()
        {
            var dto = new CreateCropCareTaskRequestDto
            {
                CropId = Guid.NewGuid(),
                TaskType = TaskType.Water,
                ScheduledDate = DateTime.UtcNow.AddDays(1),
                Note = new string('a', 1001)
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "备注长度不能超过1000个字符");
        }

        [Fact]
        public void Validate_WhenNoteIsNull_ShouldPass()
        {
            var dto = new CreateCropCareTaskRequestDto
            {
                CropId = Guid.NewGuid(),
                TaskType = TaskType.Water,
                ScheduledDate = DateTime.UtcNow.AddDays(1),
                Note = null
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
        }
    }

    public class UpdateCropCareTaskRequestDtoValidatorTests
    {
        private readonly UpdateCropCareTaskRequestDtoValidator _validator;

        public UpdateCropCareTaskRequestDtoValidatorTests()
        {
            _validator = new UpdateCropCareTaskRequestDtoValidator();
        }

        [Fact]
        public void Validate_WhenAllFieldsAreNull_ShouldPass()
        {
            var dto = new UpdateCropCareTaskRequestDto();

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void Validate_WhenTaskTypeIsValid_ShouldPass()
        {
            var dto = new UpdateCropCareTaskRequestDto
            {
                TaskType = TaskType.Fertilize
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WhenTaskTypeIsInvalid_ShouldFail()
        {
            var dto = new UpdateCropCareTaskRequestDto
            {
                TaskType = (TaskType)999
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "任务类型无效");
        }

        [Fact]
        public void Validate_WhenStatusIsValid_ShouldPass()
        {
            var dto = new UpdateCropCareTaskRequestDto
            {
                Status = TaskStatus.InProgress
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WhenStatusIsInvalid_ShouldFail()
        {
            var dto = new UpdateCropCareTaskRequestDto
            {
                Status = (TaskStatus)999
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "任务状态无效");
        }

        [Fact]
        public void Validate_WhenNoteIsValid_ShouldPass()
        {
            var dto = new UpdateCropCareTaskRequestDto
            {
                Note = "已完成浇水"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WhenNoteIsTooLong_ShouldFail()
        {
            var dto = new UpdateCropCareTaskRequestDto
            {
                Note = new string('a', 1001)
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "备注长度不能超过1000个字符");
        }
    }

    #endregion

    #region HarvestRecordValidators

    public class CreateHarvestRecordRequestDtoValidatorTests
    {
        private readonly CreateHarvestRecordRequestDtoValidator _validator;

        public CreateHarvestRecordRequestDtoValidatorTests()
        {
            _validator = new CreateHarvestRecordRequestDtoValidator();
        }

        [Fact]
        public void Validate_WhenAllFieldsAreValid_ShouldPass()
        {
            var dto = new CreateHarvestRecordRequestDto
            {
                CropId = Guid.NewGuid(),
                HarvestDate = DateTime.UtcNow.AddDays(-1),
                Quantity = 2.5m,
                Unit = "kg",
                QualityNote = "品质优良"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void Validate_WhenCropIdIsEmpty_ShouldFail()
        {
            var dto = new CreateHarvestRecordRequestDto
            {
                CropId = Guid.Empty,
                HarvestDate = DateTime.UtcNow.AddDays(-1),
                Quantity = 2.5m,
                Unit = "kg"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "作物ID不能为空");
        }

        [Fact]
        public void Validate_WhenHarvestDateIsEmpty_ShouldFail()
        {
            var dto = new CreateHarvestRecordRequestDto
            {
                CropId = Guid.NewGuid(),
                HarvestDate = default,
                Quantity = 2.5m,
                Unit = "kg"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "收获日期不能为空");
        }

        [Fact]
        public void Validate_WhenHarvestDateIsInFuture_ShouldFail()
        {
            var dto = new CreateHarvestRecordRequestDto
            {
                CropId = Guid.NewGuid(),
                HarvestDate = DateTime.UtcNow.AddDays(10),
                Quantity = 2.5m,
                Unit = "kg"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "收获日期不能晚于当前日期");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-0.1)]
        public void Validate_WhenQuantityIsNotGreaterThanZero_ShouldFail(decimal quantity)
        {
            var dto = new CreateHarvestRecordRequestDto
            {
                CropId = Guid.NewGuid(),
                HarvestDate = DateTime.UtcNow.AddDays(-1),
                Quantity = quantity,
                Unit = "kg"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "收成数量必须大于0");
        }

        [Fact]
        public void Validate_WhenUnitIsEmpty_ShouldFail()
        {
            var dto = new CreateHarvestRecordRequestDto
            {
                CropId = Guid.NewGuid(),
                HarvestDate = DateTime.UtcNow.AddDays(-1),
                Quantity = 2.5m,
                Unit = string.Empty
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "单位不能为空");
        }

        [Fact]
        public void Validate_WhenUnitIsTooLong_ShouldFail()
        {
            var dto = new CreateHarvestRecordRequestDto
            {
                CropId = Guid.NewGuid(),
                HarvestDate = DateTime.UtcNow.AddDays(-1),
                Quantity = 2.5m,
                Unit = new string('a', 51)
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "单位长度不能超过50个字符");
        }

        [Fact]
        public void Validate_WhenQualityNoteIsTooLong_ShouldFail()
        {
            var dto = new CreateHarvestRecordRequestDto
            {
                CropId = Guid.NewGuid(),
                HarvestDate = DateTime.UtcNow.AddDays(-1),
                Quantity = 2.5m,
                Unit = "kg",
                QualityNote = new string('a', 1001)
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "质量说明长度不能超过1000个字符");
        }

        [Fact]
        public void Validate_WhenQualityNoteIsNull_ShouldPass()
        {
            var dto = new CreateHarvestRecordRequestDto
            {
                CropId = Guid.NewGuid(),
                HarvestDate = DateTime.UtcNow.AddDays(-1),
                Quantity = 2.5m,
                Unit = "kg",
                QualityNote = null
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
        }
    }

    public class UpdateHarvestRecordRequestDtoValidatorTests
    {
        private readonly UpdateHarvestRecordRequestDtoValidator _validator;

        public UpdateHarvestRecordRequestDtoValidatorTests()
        {
            _validator = new UpdateHarvestRecordRequestDtoValidator();
        }

        [Fact]
        public void Validate_WhenAllFieldsAreNull_ShouldPass()
        {
            var dto = new UpdateHarvestRecordRequestDto();

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void Validate_WhenHarvestDateIsValid_ShouldPass()
        {
            var dto = new UpdateHarvestRecordRequestDto
            {
                HarvestDate = DateTime.UtcNow.AddDays(-5)
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WhenHarvestDateIsInFuture_ShouldFail()
        {
            var dto = new UpdateHarvestRecordRequestDto
            {
                HarvestDate = DateTime.UtcNow.AddDays(10)
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "收获日期不能晚于当前日期");
        }

        [Fact]
        public void Validate_WhenQuantityIsValid_ShouldPass()
        {
            var dto = new UpdateHarvestRecordRequestDto
            {
                Quantity = 5.0m
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_WhenQuantityIsNotGreaterThanZero_ShouldFail(decimal quantity)
        {
            var dto = new UpdateHarvestRecordRequestDto
            {
                Quantity = quantity
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "收成数量必须大于0");
        }

        [Fact]
        public void Validate_WhenUnitIsValid_ShouldPass()
        {
            var dto = new UpdateHarvestRecordRequestDto
            {
                Unit = "斤"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WhenUnitIsTooLong_ShouldFail()
        {
            var dto = new UpdateHarvestRecordRequestDto
            {
                Unit = new string('a', 51)
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "单位长度不能超过50个字符");
        }

        [Fact]
        public void Validate_WhenQualityNoteIsValid_ShouldPass()
        {
            var dto = new UpdateHarvestRecordRequestDto
            {
                QualityNote = "品质很好"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WhenQualityNoteIsTooLong_ShouldFail()
        {
            var dto = new UpdateHarvestRecordRequestDto
            {
                QualityNote = new string('a', 1001)
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "质量说明长度不能超过1000个字符");
        }
    }

    #endregion

    #region PestRecordValidators

    public class CreatePestRecordRequestDtoValidatorTests
    {
        private readonly CreatePestRecordRequestDtoValidator _validator;

        public CreatePestRecordRequestDtoValidatorTests()
        {
            _validator = new CreatePestRecordRequestDtoValidator();
        }

        [Fact]
        public void Validate_WhenAllFieldsAreValid_ShouldPass()
        {
            var dto = new CreatePestRecordRequestDto
            {
                CropId = Guid.NewGuid(),
                IssueType = "蚜虫",
                Symptoms = "叶片上有绿色小虫",
                Treatment = "使用肥皂水喷洒",
                DetectedDate = DateTime.UtcNow.AddDays(-2),
                Status = PestStatus.Detected
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void Validate_WhenCropIdIsEmpty_ShouldFail()
        {
            var dto = new CreatePestRecordRequestDto
            {
                CropId = Guid.Empty,
                IssueType = "蚜虫",
                Symptoms = "叶片上有绿色小虫",
                Treatment = "使用肥皂水喷洒",
                DetectedDate = DateTime.UtcNow.AddDays(-2),
                Status = PestStatus.Detected
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "作物ID不能为空");
        }

        [Fact]
        public void Validate_WhenIssueTypeIsEmpty_ShouldFail()
        {
            var dto = new CreatePestRecordRequestDto
            {
                CropId = Guid.NewGuid(),
                IssueType = string.Empty,
                Symptoms = "叶片上有绿色小虫",
                Treatment = "使用肥皂水喷洒",
                DetectedDate = DateTime.UtcNow.AddDays(-2),
                Status = PestStatus.Detected
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "问题类型不能为空");
        }

        [Fact]
        public void Validate_WhenIssueTypeIsTooLong_ShouldFail()
        {
            var dto = new CreatePestRecordRequestDto
            {
                CropId = Guid.NewGuid(),
                IssueType = new string('a', 101),
                Symptoms = "叶片上有绿色小虫",
                Treatment = "使用肥皂水喷洒",
                DetectedDate = DateTime.UtcNow.AddDays(-2),
                Status = PestStatus.Detected
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "问题类型长度不能超过100个字符");
        }

        [Fact]
        public void Validate_WhenSymptomsIsEmpty_ShouldFail()
        {
            var dto = new CreatePestRecordRequestDto
            {
                CropId = Guid.NewGuid(),
                IssueType = "蚜虫",
                Symptoms = string.Empty,
                Treatment = "使用肥皂水喷洒",
                DetectedDate = DateTime.UtcNow.AddDays(-2),
                Status = PestStatus.Detected
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "症状不能为空");
        }

        [Fact]
        public void Validate_WhenSymptomsIsTooLong_ShouldFail()
        {
            var dto = new CreatePestRecordRequestDto
            {
                CropId = Guid.NewGuid(),
                IssueType = "蚜虫",
                Symptoms = new string('a', 1001),
                Treatment = "使用肥皂水喷洒",
                DetectedDate = DateTime.UtcNow.AddDays(-2),
                Status = PestStatus.Detected
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "症状长度不能超过1000个字符");
        }

        [Fact]
        public void Validate_WhenTreatmentIsEmpty_ShouldFail()
        {
            var dto = new CreatePestRecordRequestDto
            {
                CropId = Guid.NewGuid(),
                IssueType = "蚜虫",
                Symptoms = "叶片上有绿色小虫",
                Treatment = string.Empty,
                DetectedDate = DateTime.UtcNow.AddDays(-2),
                Status = PestStatus.Detected
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "治疗方案不能为空");
        }

        [Fact]
        public void Validate_WhenTreatmentIsTooLong_ShouldFail()
        {
            var dto = new CreatePestRecordRequestDto
            {
                CropId = Guid.NewGuid(),
                IssueType = "蚜虫",
                Symptoms = "叶片上有绿色小虫",
                Treatment = new string('a', 1001),
                DetectedDate = DateTime.UtcNow.AddDays(-2),
                Status = PestStatus.Detected
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "治疗方案长度不能超过1000个字符");
        }

        [Fact]
        public void Validate_WhenDetectedDateIsEmpty_ShouldFail()
        {
            var dto = new CreatePestRecordRequestDto
            {
                CropId = Guid.NewGuid(),
                IssueType = "蚜虫",
                Symptoms = "叶片上有绿色小虫",
                Treatment = "使用肥皂水喷洒",
                DetectedDate = default,
                Status = PestStatus.Detected
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "发现日期不能为空");
        }

        [Fact]
        public void Validate_WhenDetectedDateIsInFuture_ShouldFail()
        {
            var dto = new CreatePestRecordRequestDto
            {
                CropId = Guid.NewGuid(),
                IssueType = "蚜虫",
                Symptoms = "叶片上有绿色小虫",
                Treatment = "使用肥皂水喷洒",
                DetectedDate = DateTime.UtcNow.AddDays(10),
                Status = PestStatus.Detected
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "发现日期不能晚于当前日期");
        }

        [Fact]
        public void Validate_WhenStatusIsInvalid_ShouldFail()
        {
            var dto = new CreatePestRecordRequestDto
            {
                CropId = Guid.NewGuid(),
                IssueType = "蚜虫",
                Symptoms = "叶片上有绿色小虫",
                Treatment = "使用肥皂水喷洒",
                DetectedDate = DateTime.UtcNow.AddDays(-2),
                Status = (PestStatus)999
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "状态无效");
        }
    }

    public class UpdatePestRecordRequestDtoValidatorTests
    {
        private readonly UpdatePestRecordRequestDtoValidator _validator;

        public UpdatePestRecordRequestDtoValidatorTests()
        {
            _validator = new UpdatePestRecordRequestDtoValidator();
        }

        [Fact]
        public void Validate_WhenAllFieldsAreNull_ShouldPass()
        {
            var dto = new UpdatePestRecordRequestDto();

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void Validate_WhenIssueTypeIsValid_ShouldPass()
        {
            var dto = new UpdatePestRecordRequestDto
            {
                IssueType = "白粉病"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WhenIssueTypeIsTooLong_ShouldFail()
        {
            var dto = new UpdatePestRecordRequestDto
            {
                IssueType = new string('a', 101)
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "问题类型长度不能超过100个字符");
        }

        [Fact]
        public void Validate_WhenSymptomsIsValid_ShouldPass()
        {
            var dto = new UpdatePestRecordRequestDto
            {
                Symptoms = "叶片出现白色粉末"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WhenSymptomsIsTooLong_ShouldFail()
        {
            var dto = new UpdatePestRecordRequestDto
            {
                Symptoms = new string('a', 1001)
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "症状长度不能超过1000个字符");
        }

        [Fact]
        public void Validate_WhenTreatmentIsValid_ShouldPass()
        {
            var dto = new UpdatePestRecordRequestDto
            {
                Treatment = "使用小苏打溶液喷洒"
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WhenTreatmentIsTooLong_ShouldFail()
        {
            var dto = new UpdatePestRecordRequestDto
            {
                Treatment = new string('a', 1001)
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "治疗方案长度不能超过1000个字符");
        }

        [Fact]
        public void Validate_WhenDetectedDateIsValid_ShouldPass()
        {
            var dto = new UpdatePestRecordRequestDto
            {
                DetectedDate = DateTime.UtcNow.AddDays(-3)
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WhenDetectedDateIsInFuture_ShouldFail()
        {
            var dto = new UpdatePestRecordRequestDto
            {
                DetectedDate = DateTime.UtcNow.AddDays(10)
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "发现日期不能晚于当前日期");
        }

        [Fact]
        public void Validate_WhenStatusIsValid_ShouldPass()
        {
            var dto = new UpdatePestRecordRequestDto
            {
                Status = PestStatus.Resolved
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WhenStatusIsInvalid_ShouldFail()
        {
            var dto = new UpdatePestRecordRequestDto
            {
                Status = (PestStatus)999
            };

            ValidationResult result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "状态无效");
        }
    }

    #endregion
}
