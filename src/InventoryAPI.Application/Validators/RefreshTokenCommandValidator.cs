using FluentValidation;
using InventoryAPI.Application.Commands.Auth;

namespace InventoryAPI.Application.Validators;

/// <summary>
/// Validator for RefreshTokenCommand
/// </summary>
public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required");
    }
}
