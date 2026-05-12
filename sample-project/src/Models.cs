using NDjango.RestFramework.Base;

namespace SampleProject;

/// <summary>
/// Common base for sample entities: int primary key plus audit columns.
/// CreatedAt / UpdatedAt are stamped automatically by <see cref="AppDbContext"/> on save.
/// </summary>
public abstract class StandardEntity : BaseModel<int>
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class Category : StandardEntity
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";

    public override string[] GetFields() =>
    [
        nameof(Id),
        nameof(Name),
        nameof(Description),
        nameof(CreatedAt),
        nameof(UpdatedAt),
    ];
}

public class Restaurant : StandardEntity
{
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public string Phone { get; set; } = "";

    public RestaurantProfile? Profile { get; set; }
    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();

    public override string[] GetFields() =>
    [
        nameof(Id),
        nameof(Name),
        nameof(Address),
        nameof(Phone),
        nameof(CreatedAt),
        nameof(UpdatedAt),
        nameof(Profile),
        // Nested-field projection — prefix is the navigation's element type name,
        // not the property name (per BaseController.ResolveAndValidateFields).
        "RestaurantProfile:Website",
        "RestaurantProfile:Capacity",
    ];
}

public class RestaurantProfile : StandardEntity
{
    public int RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }

    public string Website { get; set; } = "";
    public string OpeningHours { get; set; } = "";
    public int Capacity { get; set; }

    public override string[] GetFields() =>
    [
        nameof(Id),
        nameof(RestaurantId),
        nameof(Website),
        nameof(OpeningHours),
        nameof(Capacity),
        nameof(CreatedAt),
        nameof(UpdatedAt),
    ];
}

public class Ingredient : StandardEntity
{
    public string Name { get; set; } = "";
    public bool IsAllergen { get; set; }

    public override string[] GetFields() =>
    [
        nameof(Id),
        nameof(Name),
        nameof(IsAllergen),
        nameof(CreatedAt),
        nameof(UpdatedAt),
    ];
}

public class MenuItem : StandardEntity
{
    public int RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }

    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; } = true;

    public override string[] GetFields() =>
    [
        nameof(Id),
        nameof(RestaurantId),
        nameof(Name),
        nameof(Description),
        nameof(Price),
        nameof(IsAvailable),
        nameof(CreatedAt),
        nameof(UpdatedAt),
    ];
}

/// <summary>
/// Pure join table: no controller, no DTO. Lives in the model to demonstrate that the
/// library only generates CRUD for types it can see a controller for.
/// </summary>
public class MenuItemIngredient
{
    public int MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }

    public int IngredientId { get; set; }
    public Ingredient? Ingredient { get; set; }
}

/// <summary>
/// Demonstrates per-field <c>Validate{Field}Async</c> hooks on the matching serializer
/// (see <c>TagSerializer.ValidateNameAsync</c> / <c>ValidateSlugAsync</c>): the hooks
/// normalize the inbound value (trim, lowercase, dash-collapse) and run async DB checks
/// for uniqueness before the row is persisted.
/// </summary>
public class Tag : StandardEntity
{
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";

    public override string[] GetFields() =>
    [
        nameof(Id),
        nameof(Name),
        nameof(Slug),
        nameof(CreatedAt),
        nameof(UpdatedAt),
    ];
}

/// <summary>
/// Demonstrates HTTP-method gating via <see cref="NDjango.RestFramework.Base.ActionOptions"/>:
/// the matching <c>AuditLogsController</c> only allows GET/POST. PATCH, PUT, single-DELETE,
/// and bulk-DELETE all return <c>405 Method Not Allowed</c> while staying listed in OpenAPI.
/// </summary>
public class AuditLog : StandardEntity
{
    public string Action { get; set; } = "";
    public string EntityName { get; set; } = "";
    public string? Detail { get; set; }

    public override string[] GetFields() =>
    [
        nameof(Id),
        nameof(Action),
        nameof(EntityName),
        nameof(Detail),
        nameof(CreatedAt),
        nameof(UpdatedAt),
    ];
}

/// <summary>
/// Wide primitive type surface — exercises OpenAPI schema generation across every CLR primitive
/// the library is expected to handle.
/// </summary>
public class Gift : StandardEntity
{
    public string Name { get; set; } = "";
    public bool IsWrapped { get; set; }
    public Guid TrackingCode { get; set; }
    public decimal Price { get; set; }
    public long Barcode { get; set; }
    public double Weight { get; set; }
    public float Rating { get; set; }
    public short QuantityInStock { get; set; }
    public byte MinAge { get; set; }
    public DateTimeOffset ShippedAt { get; set; }
    public TimeSpan PreparationTime { get; set; }
    public DateOnly ExpirationDate { get; set; }
    public TimeOnly AvailableFrom { get; set; }
    public string Description { get; set; } = "";
    public string Notes { get; set; } = "";

    public override string[] GetFields() =>
    [
        nameof(Id),
        nameof(Name),
        nameof(IsWrapped),
        nameof(TrackingCode),
        nameof(Price),
        nameof(Barcode),
        nameof(Weight),
        nameof(Rating),
        nameof(QuantityInStock),
        nameof(MinAge),
        nameof(ShippedAt),
        nameof(PreparationTime),
        nameof(ExpirationDate),
        nameof(AvailableFrom),
        nameof(Description),
        nameof(Notes),
        nameof(CreatedAt),
        nameof(UpdatedAt),
    ];
}

#region DTOs

public class CategoryDto : BaseDto<int>
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
}

public class RestaurantDto : BaseDto<int>
{
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public string Phone { get; set; } = "";
}

public class RestaurantProfileDto : BaseDto<int>
{
    public int RestaurantId { get; set; }
    public string Website { get; set; } = "";
    public string OpeningHours { get; set; } = "";
    public int Capacity { get; set; }
}

public class IngredientDto : BaseDto<int>
{
    public string Name { get; set; } = "";
    public bool IsAllergen { get; set; }
}

public class MenuItemDto : BaseDto<int>
{
    public int RestaurantId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; } = true;
}

public class TagDto : BaseDto<int>
{
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
}

public class AuditLogDto : BaseDto<int>
{
    public string Action { get; set; } = "";
    public string EntityName { get; set; } = "";
    public string? Detail { get; set; }
}

public class GiftDto : BaseDto<int>
{
    public string Name { get; set; } = "";
    public bool IsWrapped { get; set; }
    public Guid TrackingCode { get; set; }
    public decimal Price { get; set; }
    public long Barcode { get; set; }
    public double Weight { get; set; }
    public float Rating { get; set; }
    public short QuantityInStock { get; set; }
    public byte MinAge { get; set; }
    public DateTimeOffset ShippedAt { get; set; }
    public TimeSpan PreparationTime { get; set; }
    public DateOnly ExpirationDate { get; set; }
    public TimeOnly AvailableFrom { get; set; }
    public string Description { get; set; } = "";
    public string Notes { get; set; } = "";
}

#endregion
