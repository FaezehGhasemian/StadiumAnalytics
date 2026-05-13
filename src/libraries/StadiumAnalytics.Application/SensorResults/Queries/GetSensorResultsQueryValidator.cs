using FluentValidation;

namespace StadiumAnalytics.Application.SensorResults.Queries
{
    public sealed class GetSensorResultsQueryValidator : AbstractValidator<GetSensorResultsQuery>
    {
        public GetSensorResultsQueryValidator()
        {
            RuleFor(x => x.Gate)
                .MaximumLength(100)
                .When(x => !string.IsNullOrWhiteSpace(x.Gate))
                .WithMessage("Gate name is too long.");

            RuleFor(x => x.StartTimeUtc)
                .LessThanOrEqualTo(x => x.EndTimeUtc!.Value)
                .When(x => x.StartTimeUtc.HasValue && x.EndTimeUtc.HasValue)
                .WithMessage("StartTimeUtc must be less than or equal to EndTimeUtc.");

            RuleFor(x => x.EndTimeUtc)
                .GreaterThanOrEqualTo(x => x.StartTimeUtc!.Value)
                .When(x => x.StartTimeUtc.HasValue && x.EndTimeUtc.HasValue)
                .WithMessage("EndTimeUtc must be greater than or equal to StartTimeUtc.");
        }
    }
}
