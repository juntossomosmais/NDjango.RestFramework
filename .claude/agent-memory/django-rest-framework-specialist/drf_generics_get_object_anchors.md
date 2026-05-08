---
name: DRF generics.py get_object / filter_queryset anchors at 3.17.1
description: Exact behaviors inside GenericAPIView.get_object and filter_queryset, used to verify that NDjango's filter-scoped load mirrors DRF.
type: reference
---

All paths under `encode/django-rest-framework@3.17.1`, `rest_framework/generics.py`.

## `GenericAPIView.get_object` (around lines 67-89)

Body sequence:
1. `queryset = self.filter_queryset(self.get_queryset())` — filter backends run *first*, before any id lookup. This is the DRF anchor for "every row-resolving action is filter-scoped".
2. `lookup_url_kwarg = self.lookup_url_kwarg or self.lookup_field` — `lookup_field` defaults to `"pk"`.
3. `filter_kwargs = {self.lookup_field: self.kwargs[lookup_url_kwarg]}`
4. `obj = get_object_or_404(queryset, **filter_kwargs)` — out-of-scope ids resolve to 404, same shape as missing-row.
5. `self.check_object_permissions(self.request, obj)` — object-level permission check happens *after* fetch. DRF runs this for every single-row action.

## `GenericAPIView.filter_queryset` (around lines 123-127)

```python
for backend in list(self.filter_backends):
    queryset = backend().filter_queryset(self.request, queryset, self)
return queryset
```

Sequential pipeline, same shape as our `Filters` chain in `FilterQuery`.

## `GenericAPIView.get_serializer_context` (around lines 118-122)

```python
return {
    'request': self.request,
    'format': self.format_kwarg,
    'view': self,
}
```

Available to every validator / serializer method via `self.context[...]`. We do not have a direct analogue — `ValidationContext<TPrimaryKey>` carries Operation/EntityId only. Serializer subclasses needing `HttpContext` reach into DI. Worth surfacing if validators routinely need request data.

## Cross-references for `mixins.py` 3.17.1

- `CreateModelMixin.get_success_headers(data)` — lines 25-29; reads `data[api_settings.URL_FIELD_NAME]` and returns `{'Location': ...}`. We build the Location header via `CreatedAtAction(nameof(GetSingle), new { id = data.Id }, jObject)` — different mechanism, same outcome.
- `UpdateModelMixin.update` clears `_prefetched_objects_cache` after `perform_update` (lines 66-69). This is Django ORM prefetch-cache invalidation; not applicable to EF Core (we don't have that cache).
- `partial_update` lines 75-77: `kwargs['partial'] = True; return self.update(...)`. DRF collapses PUT and PATCH onto one mixin method — we split them because `PartialJsonObject<T>` carries an IsSet mask `TOrigin` cannot.
