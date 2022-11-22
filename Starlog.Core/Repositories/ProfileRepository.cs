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
}

internal sealed class ProfileRepository : RepositoryBase<Profile>, IProfileRepository, IProfileQueryService
{
    public ProfileRepository(IEventBus eventBus, IJsonPersister persister, ILogger<ProfileRepository> logger)
        : base(eventBus, persister, logger)
    {
    }
}
