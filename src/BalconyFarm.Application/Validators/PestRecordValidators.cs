using BalconyFarm.Application.DTOs;
using FluentValidation;

namespace BalconyFarm.Application.Validators;

public class CreatePestRecordRequestDtoValidator : AbstractValidator<CreatePestRecordRequestDto>
{
    public CreatePestRecordRequestDtoValidator()
    {
        RuleFor(x => x.CropId)
            .NotEmpty().WithMessage("作物ID不能为空");

        RuleFor(x => x.IssueType)
            .NotEmpty().WithMessage("问题类型不能为空")
            .MaximumLength(100).WithMessage("问题类型长度不能超过100个字符");

        RuleFor(x => x.Symptoms)
            .NotEmpty().WithMessage("症状不能为空")
            .MaximumLength(1000).WithMessage("症状长度不能超过1000个字符");

        RuleFor(x => x.Treatment)
            .NotEmpty().WithMessage("治疗方案不能为空")
            .MaximumLength(1000).WithMessage("治疗方案长度不能超过1000个字符");

        RuleFor(x => x.DetectedDate)
            .NotEmpty().WithMessage("发现日期不能为空")
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("发现日期不能晚于当前日期");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("状态无效");
    }
}

public class UpdatePestRecordRequestDtoValidator : AbstractValidator<UpdatePestRecordRequestDto>
{
    public UpdatePestRecordRequestDtoValidator()
    {
        RuleFor(x => x.IssueType)
            .MaximumLength(100).When(x => !string.IsNullOrEmpty(x.IssueType))
            .WithMessage("问题类型长度不能超过100个字符");

        RuleFor(x => x.Symptoms)
            .MaximumLength(1000).When(x => !string.IsNullOrEmpty(x.Symptoms))
            .WithMessage("症状长度不能超过1000个字符");

        RuleFor(x => x.Treatment)
            .MaximumLength(1000).When(x => !string.IsNullOrEmpty(x.Treatment))
            .WithMessage("治疗方案长度不能超过1000个字符");

        RuleFor(x => x.DetectedDate)
            .LessThanOrEqualTo(DateTime.UtcNow).When(x => x.DetectedDate.HasValue)
            .WithMessage("发现日期不能晚于当前日期");

        RuleFor(x => x.Status)
            .IsInEnum().When(x => x.Status.HasValue)
            .WithMessage("状态无效");
    }
}

public class CreateTreatmentLogRequestDtoValidator : AbstractValidator<CreateTreatmentLogRequestDto>
{
    public CreateTreatmentLogRequestDtoValidator()
    {
        RuleFor(x => x.Medication)
            .NotEmpty().WithMessage("用药名称不能为空")
            .MaximumLength(200).WithMessage("用药名称长度不能超过200个字符");

        RuleFor(x => x.Dosage)
            .NotEmpty().WithMessage("用药量不能为空")
            .MaximumLength(100).WithMessage("用药量长度不能超过100个字符");

        RuleFor(x => x.SymptomChange)
            .NotEmpty().WithMessage("症状变化不能为空")
            .MaximumLength(1000).WithMessage("症状变化长度不能超过1000个字符");

        RuleFor(x => x.TreatmentDate)
            .NotEmpty().WithMessage("治疗日期不能为空")
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("治疗日期不能晚于当前日期");

        RuleFor(x => x.Note)
            .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Note))
            .WithMessage("备注长度不能超过500个字符");
    }
}
