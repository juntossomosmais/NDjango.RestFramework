---
name: DRF translation tier mapping
description: How NDjango.RestFramework's classes map onto DRF's two-tier hierarchy at 3.17.1
type: project
---

DRF at 3.17.1 has a two-tier serializer hierarchy:
- `Serializer` (`rest_framework/serializers.py:170-211`) — abstract `create`/`update` raise `NotImplementedError`; `save()` is the orchestrator that dispatches to `update(instance, validated_data)` or `create(validated_data)`.
- `ModelSerializer(Serializer)` (`serializers.py:898+`) — concrete `create` calls `ModelClass._default_manager.create(**validated_data)`; concrete `update` does `setattr` then `instance.save()`. ORM writes live here.

Our `Serializer<TOrigin,TDestination,TPK,TContext>` is **semantically DRF's `ModelSerializer`**, not DRF's `Serializer`. It owns `_dbContext`, calls `SaveChangesAsync`, and exposes `MapToDestination`/`ApplyToDestination` as override seams. We do **not** ship a separate non-ORM tier — there is no `Serializer<TOrigin>` (single-parameter) base.

**Why:** Pre-release naming parity matters (the project's CLAUDE.md pins DRF naming as the "porting contract"), and the actual consumer surface is CRUD-on-EF-Core. A non-ORM tier has no current use case.

**How to apply:** When discussing the "serializer doing ORM writes," do not call this divergent — it is a faithful translation of `ModelSerializer`. The naming is the only honest gap. If the user asks about adding a non-ORM tier, push back: defer until a real use case appears.

`BaseController<TOrigin,TDestination,TPK,TContext>` corresponds to DRF's `ModelViewSet(CreateModelMixin, RetrieveModelMixin, UpdateModelMixin, DestroyModelMixin, ListModelMixin, GenericViewSet)` (`viewsets.py:184-191`). The fusion-into-one-class is correct for ASP.NET Core attribute routing — DRF's mixin composition does not translate cleanly because C# controllers must inherit one base and routing is per-method.
