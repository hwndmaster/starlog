using Genius.Atom.Data.Persistence;
using Genius.Atom.Infrastructure.Entities;
using Genius.Atom.Infrastructure.Events;
using Genius.Starlog.Core.Models;
using Microsoft.Extensions.Logging;

namespace Genius.Starlog.Core.Repositories;

public interface IProfileQueryService : IQueryService<Profile>
{
}

public interface IProfileRepository : IRepository<Profile>
{
    void SetAnonymous(Profile? profile);
}

internal sealed class ProfileRepository : RepositoryBase<Profile>, IProfileRepository, IProfileQueryService
{
    private readonly ISettingsRepository _settingsRepository;

    private Profile? _anonymousProfile;

    public ProfileRepository(IEventBus eventBus, IJsonPersister persister, ILogger<ProfileRepository> logger, ISettingsRepository settingsRepository)
        : base(eventBus, persister, logger)
    {
        _settingsRepository = settingsRepository.NotNull();
    }

    public override Task DeleteAsync(Guid entityId)
    {
        if (entityId.Equals(Profile.AnonymousProfileId))
        {
            SetAnonymous(null);
            return Task.CompletedTask;
        }

        return base.DeleteAsync(entityId);
    }

    public override Task<Profile?> FindByIdAsync(Guid entityId)
    {
        if (entityId.Equals(Profile.AnonymousProfileId))
        {
            return Task.FromResult(_anonymousProfile);
        }

        return base.FindByIdAsync(entityId);
    }

    public override Task StoreAsync(params Profile[] entities)
    {
        entities = entities.Where(x => !x.IsAnonymous).ToArray();
        if (entities.Length == 0)
        {
            return Task.CompletedTask;
        }

        return base.StoreAsync(entities);
    }

    public void SetAnonymous(Profile? profile)
        => _anonymousProfile = profile;

    protected override Task FillUpRelationsAsync(Profile entity)
    {
        // Upgrade PlainTextProfileLogCodec for profiles created before 14-Jan-2024
        var settings = new Lazy<Settings>(() => _settingsRepository.Get());
        if (entity.Settings.LogCodec is PlainTextProfileLogCodec plainTextCodec
            && plainTextCodec.LinePatternId == Guid.Empty)
        {
            plainTextCodec.LinePatternId = settings.Value.PlainTextLogCodecLinePatterns.First().Id;
        }

        return base.FillUpRelationsAsync(entity);
    }
}
