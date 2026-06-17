using BalconyFarm.Application.DTOs;
using FluentValidation;

namespace BalconyFarm.Application.Validators;

public class GetRecommendationsRequestDtoValidator : AbstractValidator<GetRecommendationsRequestDto>
{
    public GetRecommendationsRequestDtoValidator()
    {
        RuleFor(x => x.City)
            .NotEmpty().WithMessage("城市名称不能为空");

        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12).When(x => x.Month.HasValue)
            .WithMessage("月份必须在1-12之间");
    }
}

public class CreateCropFromRecommendationRequestDtoValidator : AbstractValidator<CreateCropFromRecommendationRequestDto>
{
    public CreateCropFromRecommendationRequestDtoValidator()
    {
        RuleFor(x => x.City)
            .NotEmpty().WithMessage("城市名称不能为空");

        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12)
            .WithMessage("月份必须在1-12之间");

        RuleFor(x => x.CropName)
            .NotEmpty().WithMessage("作物名称不能为空")
            .MaximumLength(100).WithMessage("作物名称长度不能超过100个字符");

        RuleFor(x => x.Variety)
            .NotEmpty().WithMessage("作物品种不能为空")
            .MaximumLength(100).WithMessage("作物品种长度不能超过100个字符");

        RuleFor(x => x.PlantingDate)
            .LessThanOrEqualTo(DateTime.UtcNow).When(x => x.PlantingDate.HasValue)
            .WithMessage("种植日期不能晚于当前日期");
    }
}
