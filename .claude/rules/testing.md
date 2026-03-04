---
paths: "tests/**/*.cs"
---

# Testing

## Rules

- **Use xUnit** (`[Fact]`, `[Theory]`). Never add FluentAssertions to new tests; use xUnit native asserts (`Assert.Equal`, `Assert.True`, `Assert.Contains`, `Assert.DoesNotContain`, `Assert.Equivalent`, etc.) only.
- **Every test must have `// Arrange`, `// Act`, `// Assert` comment sections**, even when one section is trivial.
- **Mocking**: Use `Moq`.
  - **Moq** for interface mocking: `new Mock<IPartnerACMEService>()`
  - **Moq.AutoMock** for auto-wiring dependencies: `var mocker = new AutoMocker()`
  - Reuse existing mocks from `Support/Mocks/` before creating new ones:
      - `MockMediatorHandler` — in-memory mediator with notification tracking and `HasNotificationWith(message)` helper
      - `MockClaimsPrincipal` — configurable claims principal for auth-dependent tests
      - `MockRedisCacheManager` — pre-configured cache manager mock (`GetDefault(isCacheAvailable)`)
- **Test Names**: `[MethodUnderTest]_[StateUnderTest]_[ExpectedBehavior]`.

## Controllers

For every controller:

- Verify permissions and authorization boundaries.
- Test with minimum body fields.
- Test with maximum body fields.
- Test every combination of filters
- Test if the user just can update or get their own data, and not other users' data.
- Test both success and failure scenarios.