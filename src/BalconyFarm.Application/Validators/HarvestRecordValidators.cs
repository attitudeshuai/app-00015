using BalconyFarm.Application.DTOs;
using FluentValidation;

namespace BalconyFarm.Application.Validators;

public class CreateHarvestRecordRequestDtoValidator : AbstractValidator<CreateHarvestRecordRequestDto>
{
    public CreateHarvestRecordRequestDtoValidator()
    {
        RuleFor(x => x.CropId)
            .NotEmpty().WithMessage("作物ID不能为空");

        RuleFor(x => x.HarvestDate)
            .NotEmpty().WithMessage("收获日期不能为空")
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("收获日期不能晚于当前日期");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("收成数量必须大于0");

        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage("单位不能为空")
            .MaximumLength(50).WithMessage("单位长度不能超过50个字符");

        RuleFor(x => x.QualityNote)
            .MaximumLength(1000).When(x => !string.IsNullOrEmpty(x.QualityNote))
            .WithMessage("质量说明长度不能超过1000个字符");
    }
}

public class UpdateHarvestRecordRequestDtoValidator : AbstractValidator<UpdateHarvestRecordRequestDto>
{
    public UpdateHarvestRecordRequestDtoValidator()
    {
        RuleFor(x => x.HarvestDate)
            .LessThanOrEqualTo(DateTime.UtcNow).When(x => x.HarvestDate.HasValue)
            .WithMessage("收获日期不能晚于当前日期");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).When(x => x.Quantity.HasValue)
            .WithMessage("收成数量必须大于0");

        RuleFor(x => x.Unit)
            .MaximumLength(50).When(x => !string.IsNullOrEmpty(x.Unit))
            .WithMessage("单位长度不能超过50个字符");

        RuleFor(x => x.QualityNote)
            .MaximumLength(1000).When(x => !string.IsNullOrEmpty(x.QualityNote))
            .WithMessage("质量说明长度不能超过1000个字符");
    }
}
