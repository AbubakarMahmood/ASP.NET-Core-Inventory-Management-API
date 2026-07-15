using InventoryAPI.Application.Interfaces;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Domain.Exceptions;
using MediatR;

namespace InventoryAPI.Application.Commands.FilterPresets;

/// <summary>
/// Delete filter preset command handler
/// </summary>
public class DeleteFilterPresetCommandHandler : IRequestHandler<DeleteFilterPresetCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public DeleteFilterPresetCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(DeleteFilterPresetCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();

        var filterPreset = await _unitOfWork.FilterPresets.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(FilterPreset), request.Id);

        if (filterPreset.UserId != userId)
        {
            throw new UnauthorizedAccessException("You can only delete your own filter presets");
        }

        _unitOfWork.FilterPresets.Remove(filterPreset);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
