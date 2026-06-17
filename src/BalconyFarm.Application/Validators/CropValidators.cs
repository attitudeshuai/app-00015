using BalconyFarm.Application.DTOs;
using FluentValidation;

namespace BalconyFarm.Application.Validators;

public class CreateCropRequestDtoValidator : AbstractValidator<CreateCropRequestDto>
{
    public CreateCropRequestDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("作物名称不能为空")
            .MaximumLength(100).WithMessage("作物名称长度不能超过100个字符");

        RuleFor(x => x.Variety)
            .NotEmpty().WithMessage("作物品种不能为空")
            .MaximumLength(100).WithMessage("作物品种长度不能超过100个字符");

        RuleFor(x => x.PlantingDate)
            .NotEmpty().WithMessage("种植日期不能为空")
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("种植日期不能晚于当前日期");

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("阳台位置不能为空")
            .MaximumLength(200).WithMessage("阳台位置长度不能超过200个字符");

        RuleFor(x => x.ContainerType)
            .NotEmpty().WithMessage("容器类型不能为空")
            .MaximumLength(100).WithMessage("容器类型长度不能超过100个字符");
    }
}

public class UpdateCropRequestDtoValidator : AbstractValidator<UpdateCropRequestDto>
{
    public UpdateCropRequestDtoValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(100).When(x => !string.IsNullOrEmpty(x.Name))
            .WithMessage("作物名称长度不能超过100个字符");

        RuleFor(x => x.Variety)
            .MaximumLength(100).When(x => !string.IsNullOrEmpty(x.Variety))
            .WithMessage("作物品种长度不能超过100个字符");

        RuleFor(x => x.PlantingDate)
            .LessThanOrEqualTo(DateTime.UtcNow).When(x => x.PlantingDate.HasValue)
            .WithMessage("种植日期不能晚于当前日期");

        RuleFor(x => x.Location)
            .MaximumLength(200).When(x => !string.IsNullOrEmpty(x.Location))
            .WithMessage("阳台位置长度不能超过200个字符");

        RuleFor(x => x.ContainerType)
            .MaximumLength(100).When(x => !string.IsNullOrEmpty(x.ContainerType))
            .WithMessage("容器类型长度不能超过100个字符");
    }
}
