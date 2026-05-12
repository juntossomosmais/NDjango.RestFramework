using NDjango.RestFramework.Serializer;

namespace SampleProject.Serializers;

public class GiftSerializer : Serializer<GiftDto, Gift, int, AppDbContext>
{
    public GiftSerializer(AppDbContext db) : base(db) { }
}
