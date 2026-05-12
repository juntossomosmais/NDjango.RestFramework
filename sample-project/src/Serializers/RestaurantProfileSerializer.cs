using NDjango.RestFramework.Serializer;

namespace SampleProject.Serializers;

public class RestaurantProfileSerializer : Serializer<RestaurantProfileDto, RestaurantProfile, int, AppDbContext>
{
    public RestaurantProfileSerializer(AppDbContext db) : base(db) { }
}
