using BalconyFarm.Application.DTOs;
using FluentValidation;

namespace BalconyFarm.Application.Validators;

public class CreateCropCareTaskRequestDtoValidator : AbstractValidator<CreateCropCareTaskRequestDto>
{
    public CreateCropCareTaskRequestDtoValidator()
    {
        RuleFor(x => x.CropId)
            .NotEmpty().WithMessage("作物ID不能为空");

        RuleFor(x => x.TaskType)
            .IsInEnum().WithMessage("任务类型无效");

        RuleFor(x => x.ScheduledDate)
            .NotEmpty().WithMessage("计划日期不能为空");

        RuleFor(x => x.Note)
            .MaximumLength(1000).When(x => !string.IsNullOrEmpty(x.Note))
            .WithMessage("备注长度不能超过1000个字符");
    }
}

public class UpdateCropCareTaskRequestDtoValidator : AbstractValidator<UpdateCropCareTaskRequestDto>
{
    public UpdateCropCareTaskRequestDtoValidator()
    {
        RuleFor(x => x.TaskType)
            .IsInEnum().When(x => x.TaskType.HasValue)
            .WithMessage("任务类型无效");

        RuleFor(x => x.Status)
            .IsInEnum().When(x => x.Status.HasValue)
            .WithMessage("任务状态无效");

        RuleFor(x => x.Note)
            .MaximumLength(1000).When(x => !string.IsNullOrEmpty(x.Note))
            .WithMessage("备注长度不能超过1000个字符");
    }
}
