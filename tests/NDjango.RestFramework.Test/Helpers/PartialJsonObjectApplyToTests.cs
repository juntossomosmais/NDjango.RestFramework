using System.Collections.Generic;
using NDjango.RestFramework.Helpers;
using Newtonsoft.Json.Linq;
using Xunit;

namespace NDjango.RestFramework.Test.Helpers;

public class PartialJsonObjectApplyToTests
{
    #region Test fixtures

    private class CustomerPatchDto
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public string? ComputedNotOnEntity { get; set; }
    }

    private class CustomerEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool IsActive { get; set; }
        // No ComputedNotOnEntity — should be skipped silently.
        public string ReadOnly { get; } = "preset"; // No setter — should be skipped.
    }

    private class TypeMismatchDto
    {
        public string? Name { get; set; }
        public string? Age { get; set; } // string in DTO
    }

    private class TypeMismatchEntity
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; } // int in entity — incompatible
    }

    private class NullableSourceDto
    {
        public int? Age { get; set; }
    }

    private class NonNullableTargetEntity
    {
        public int Age { get; set; }
    }

    #endregion

    public class HappyPath
    {
        [Fact]
        public void ApplyTo_WhenAllSentFieldsMatch_ShouldCopyAndReturnNames()
        {
            // Arrange
            var json = JObject.Parse("{ \"name\": \"Alice\", \"age\": 30 }");
            var partial = new PartialJsonObject<CustomerPatchDto>(json);
            var entity = new CustomerEntity { Name = "old", Age = 0, Email = "keep@me.com", IsActive = false };

            // Act
            var applied = partial.ApplyTo(entity);

            // Assert
            Assert.Equal("Alice", entity.Name);
            Assert.Equal(30, entity.Age);
            Assert.Equal("keep@me.com", entity.Email); // Not sent — must remain.
            Assert.False(entity.IsActive); // Not sent — must remain at default.
            Assert.Contains("Name", applied);
            Assert.Contains("Age", applied);
            Assert.Equal(2, applied.Count);
        }

        [Fact]
        public void ApplyTo_WhenFieldNotSent_ShouldNotCopy()
        {
            // Arrange
            var json = JObject.Parse("{ \"name\": \"Alice\" }");
            var partial = new PartialJsonObject<CustomerPatchDto>(json);
            var entity = new CustomerEntity { Name = "old", Email = "preserve@me.com" };

            // Act
            var applied = partial.ApplyTo(entity);

            // Assert
            Assert.Equal("Alice", entity.Name);
            Assert.Equal("preserve@me.com", entity.Email);
            Assert.DoesNotContain("Email", applied);
            Assert.Single(applied);
            Assert.Equal("Name", applied[0]);
        }

        [Fact]
        public void ApplyTo_WhenFieldExplicitlyNull_ShouldCopyNull()
        {
            // Arrange
            // The "sent as null" case is the entire reason PATCH semantics differ from PUT.
            // ApplyTo must propagate the null, not preserve the existing value.
            var json = JObject.Parse("{ \"email\": null }");
            var partial = new PartialJsonObject<CustomerPatchDto>(json);
            var entity = new CustomerEntity { Email = "old@me.com" };

            // Act
            var applied = partial.ApplyTo(entity);

            // Assert
            Assert.Null(entity.Email);
            Assert.Contains("Email", applied);
        }
    }

    public class SilentSkipping
    {
        [Fact]
        public void ApplyTo_WhenSourceFieldNotOnTarget_ShouldSkipSilently()
        {
            // Arrange
            var json = JObject.Parse("{ \"computedNotOnEntity\": \"x\", \"name\": \"Alice\" }");
            var partial = new PartialJsonObject<CustomerPatchDto>(json);
            var entity = new CustomerEntity();

            // Act
            var applied = partial.ApplyTo(entity);

            // Assert
            Assert.DoesNotContain("ComputedNotOnEntity", applied);
            Assert.Contains("Name", applied);
        }

        [Fact]
        public void ApplyTo_WhenTargetPropertyHasNoSetter_ShouldSkipSilently()
        {
            // Arrange
            // CustomerEntity.ReadOnly has no setter; even if the DTO had a property
            // matching the name, ApplyTo skips it silently.
            var json = JObject.Parse("{ \"readOnly\": \"hello\" }");
            var partial = new PartialJsonObject<CustomerPatchDto>(json);
            var entity = new CustomerEntity();

            // Act
            var applied = partial.ApplyTo(entity);

            // Assert
            Assert.DoesNotContain("ReadOnly", applied);
            Assert.Equal("preset", entity.ReadOnly);
        }

        [Fact]
        public void ApplyTo_WhenTypeMismatch_ShouldSkipSilently()
        {
            // Arrange
            var json = JObject.Parse("{ \"name\": \"Alice\", \"age\": \"thirty\" }");
            var partial = new PartialJsonObject<TypeMismatchDto>(json);
            var entity = new TypeMismatchEntity { Age = 99 };

            // Act
            var applied = partial.ApplyTo(entity);

            // Assert
            // Name (string→string) copies; Age (string→int) silently skipped.
            Assert.Equal("Alice", entity.Name);
            Assert.Equal(99, entity.Age);
            Assert.Contains("Name", applied);
            Assert.DoesNotContain("Age", applied);
        }
    }

    public class NullableHandling
    {
        [Fact]
        public void ApplyTo_WhenNullableSourceToNonNullableTarget_ShouldCopyValue()
        {
            // Arrange
            var json = JObject.Parse("{ \"age\": 25 }");
            var partial = new PartialJsonObject<NullableSourceDto>(json);
            var entity = new NonNullableTargetEntity { Age = 0 };

            // Act
            var applied = partial.ApplyTo(entity);

            // Assert
            Assert.Equal(25, entity.Age);
            Assert.Contains("Age", applied);
        }
    }

    public class ExceptParameter
    {
        [Fact]
        public void ApplyTo_WithExceptList_ShouldNotCopyExcludedFields()
        {
            // Arrange
            var json = JObject.Parse("{ \"name\": \"Alice\", \"email\": \"a@b.c\", \"age\": 30 }");
            var partial = new PartialJsonObject<CustomerPatchDto>(json);
            var entity = new CustomerEntity { Email = "preserve@me.com" };

            // Act
            var applied = partial.ApplyTo(entity, except: new[] { nameof(CustomerPatchDto.Email) });

            // Assert
            Assert.Equal("Alice", entity.Name);
            Assert.Equal(30, entity.Age);
            Assert.Equal("preserve@me.com", entity.Email);
            Assert.Contains("Name", applied);
            Assert.Contains("Age", applied);
            Assert.DoesNotContain("Email", applied);
        }

        [Fact]
        public void ApplyTo_WithMultipleExcludes_ShouldNotCopyAnyOfThem()
        {
            // Arrange
            var json = JObject.Parse("{ \"name\": \"Alice\", \"email\": \"a@b.c\", \"age\": 30, \"isActive\": true }");
            var partial = new PartialJsonObject<CustomerPatchDto>(json);
            var entity = new CustomerEntity();

            // Act
            var applied = partial.ApplyTo(entity, nameof(CustomerPatchDto.Email), nameof(CustomerPatchDto.Age));

            // Assert
            Assert.Equal("Alice", entity.Name);
            Assert.True(entity.IsActive);
            Assert.Equal(0, entity.Age);
            Assert.Equal(string.Empty, entity.Email);
        }
    }

    public class Caching
    {
        [Fact]
        public void ApplyTo_WhenCalledTwiceForSameTypePair_ShouldReuseCacheAndProduceSameResult()
        {
            // Arrange
            // The cache is internal (ConcurrentDictionary<(Type, Type), CopyPair[]>); we can't
            // directly inspect it, but if the second call produces the same result and didn't
            // throw, the reflection cache is at least consistent across calls.
            var json = JObject.Parse("{ \"name\": \"Alice\", \"age\": 30 }");
            var partial = new PartialJsonObject<CustomerPatchDto>(json);
            var entityA = new CustomerEntity();
            var entityB = new CustomerEntity();

            // Act
            var firstApplied = partial.ApplyTo(entityA);
            var secondApplied = partial.ApplyTo(entityB);

            // Assert
            Assert.Equal(firstApplied, secondApplied);
            Assert.Equal(entityA.Name, entityB.Name);
            Assert.Equal(entityA.Age, entityB.Age);
        }
    }

    public class NullEntity
    {
        [Fact]
        public void ApplyTo_WhenEntityIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            var json = JObject.Parse("{ \"name\": \"Alice\" }");
            var partial = new PartialJsonObject<CustomerPatchDto>(json);

            // Act
            var ex = Assert.Throws<System.ArgumentNullException>(
                () => partial.ApplyTo<CustomerEntity>(null!));

            // Assert
            Assert.Equal("entity", ex.ParamName);
        }
    }
}
