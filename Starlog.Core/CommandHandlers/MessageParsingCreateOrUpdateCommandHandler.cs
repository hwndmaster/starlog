using Genius.Atom.Infrastructure.Commands;
using Genius.Atom.Infrastructure.Events;
using Genius.Starlog.Core.Commands;
using Genius.Starlog.Core.Messages;
using Genius.Starlog.Core.Repositories;

namespace Genius.Starlog.Core.CommandHandlers;

internal sealed class MessageParsingCreateOrUpdateCommandHandler : ICommandHandler<MessageParsingCreateOrUpdateCommand, MessageParsingCreateOrUpdateCommandResult>
{
    private readonly IProfileRepository _profileRepo;
    private readonly IProfileQueryService _profileQuery;
    private readonly IEventBus _eventBus;

    public MessageParsingCreateOrUpdateCommandHandler(IProfileRepository profileRepo, IProfileQueryService profileQuery, IEventBus eventBus)
    {
        _profileRepo = profileRepo.NotNull();
        _profileQuery = profileQuery.NotNull();
        _eventBus = eventBus.NotNull();
    }

    public async Task<MessageParsingCreateOrUpdateCommandResult> ProcessAsync(MessageParsingCreateOrUpdateCommand command)
    {
        var profile = await _profileQuery.FindByIdAsync(command.ProfileId);
        Guard.NotNull(profile);

        Guid? guidAdded = null;
        Guid? guidUpdated = null;
        var itemIndex = profile.MessageParsings.ToList().FindIndex(x => x.Id == command.MessageParsing.Id);
        if (itemIndex == -1)
        {
            profile.MessageParsings.Add(command.MessageParsing);
            guidAdded = command.MessageParsing.Id;
        }
        else
        {
            profile.MessageParsings[itemIndex] = command.MessageParsing;

            guidUpdated = command.MessageParsing.Id;
        }
        await _profileRepo.StoreAsync(profile);

        _eventBus.Publish(new ProfilesAffectedEvent());

        return new MessageParsingCreateOrUpdateCommandResult(guidAdded, guidUpdated);
    }
}
