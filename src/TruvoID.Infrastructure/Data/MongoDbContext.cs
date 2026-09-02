using MongoDB.Driver;
using TruvoID.Domain.Entities;

namespace TruvoID.Infrastructure.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IMongoClient client, string databaseName)
    {
        _database = client.GetDatabase(databaseName);
    }

    public IMongoCollection<Institution> Institutions =>
        _database.GetCollection<Institution>("institutions");

    public IMongoCollection<User> Users =>
        _database.GetCollection<User>("users");

    public IMongoCollection<VerificationCall> VerificationCalls =>
        _database.GetCollection<VerificationCall>("verification_calls");

    public IMongoCollection<NotificationPreference> NotificationPreferences =>
        _database.GetCollection<NotificationPreference>("notification_preferences");

    public IMongoCollection<NotificationEvent> NotificationEvents =>
        _database.GetCollection<NotificationEvent>("notification_events");

    public IMongoCollection<InstitutionPricing> InstitutionPricings =>
        _database.GetCollection<InstitutionPricing>("institution_pricings");

    public IMongoCollection<NimcConfig> NimcConfigs =>
        _database.GetCollection<NimcConfig>("nimc_configs");
}
