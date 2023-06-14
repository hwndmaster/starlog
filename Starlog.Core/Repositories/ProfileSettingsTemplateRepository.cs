using Genius.Atom.Data.Persistence;
using Genius.Atom.Infrastructure.Entities;
using Genius.Atom.Infrastructure.Events;
using Genius.Starlog.Core.Models;
using Microsoft.Extensions.Logging;

namespace Genius.Starlog.Core.Repositories;

public interface IProfileSettingsTemplateQueryService : IQueryService<ProfileSettingsTemplate>
{
}

public interface IProfileSettingsTemplateRepository : IRepository<ProfileSettingsTemplate>
{
}

internal sealed class ProfileSettingsTemplateRepository : RepositoryBase<ProfileSettingsTemplate>, IProfileSettingsTemplateRepository, IProfileSettingsTemplateQueryService
{
    public ProfileSettingsTemplateRepository(IEventBus eventBus, IJsonPersister persister, ILogger<ProfileSettingsTemplateRepository> logger)
        : base(eventBus, persister, logger)
    {
    }
}
