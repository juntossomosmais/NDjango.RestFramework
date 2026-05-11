using NDjango.RestFramework.Serializer;

namespace SampleProject.Serializers;

public class RestaurantSerializer : Serializer<RestaurantDto, Restaurant, int, AppDbContext>
{
    public RestaurantSerializer(AppDbContext db) : base(db) { }
}
