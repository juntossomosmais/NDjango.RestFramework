---
name: drf-disable-action-anchors
description: DRF 3.17.1 pinned anchors for disable-an-action mechanisms (composition, http_method_names, router filtering, 405 vs 404 path)
metadata:
  type: reference
---

All paths under `encode/django-rest-framework@3.17.1`.

## Composition mechanism (canonical, doc-recommended)

- `rest_framework/viewsets.py:212-219` — `ReadOnlyModelViewSet(RetrieveModelMixin, ListModelMixin, GenericViewSet)`. This IS the canonical example of "ModelViewSet minus create/update/destroy".
- `rest_framework/viewsets.py:221-231` — `ModelViewSet(CreateModelMixin, RetrieveModelMixin, UpdateModelMixin, DestroyModelMixin, ListModelMixin, GenericViewSet)`. The full set.
- `rest_framework/viewsets.py:198-202` — `GenericViewSet(ViewSetMixin, generics.GenericAPIView)`. "No actions by default" — base for custom composition.
- `docs/api-guide/viewsets.md` section "Custom ViewSet base classes" — explicitly documents `CreateListRetrieveViewSet(CreateModelMixin, ListModelMixin, RetrieveModelMixin, GenericViewSet)` as the idiom. **This is the official DRF recommendation, not `http_method_names`.**

## Router-driven URL filtering (composition produces 404, not 405)

- `rest_framework/routers.py:226-234` — `SimpleRouter.get_method_map(viewset, method_map)`: for each `method -> action` in the route's mapping, includes only those where `hasattr(viewset, action)`.
- `rest_framework/routers.py:266-302` — `SimpleRouter.get_urls`. Key lines 278-280: `mapping = self.get_method_map(viewset, route.mapping); if not mapping: continue`. If a route's entire mapping has no actions on the viewset, the URL pattern is NOT registered at all.

Resulting status code semantics:
- **Route fully unregistered** (e.g., a viewset with only `list`/`retrieve` — there's no detail mapping involving `update`/`partial_update`/`destroy` because those don't exist, but `retrieve` exists, so the detail URL IS registered): the verbs present on the viewset go to handlers; the verbs absent from the route's mapping get **405**, because the URL is registered.
- **Per-route, mixed**: detail URL has `{'get':'retrieve','put':'update','patch':'partial_update','delete':'destroy'}`. If `destroy` is missing from the viewset, `get_method_map` drops `'delete'` from the mapping. The URL is still registered (because `retrieve` exists). Wrong verb -> goes through Django's `View.dispatch` which checks `request.method.lower() in self.http_method_names` (always True for standard verbs) but then `getattr(self, 'delete', self.http_method_not_allowed)` returns `http_method_not_allowed` since the viewset doesn't have a `delete` attribute bound. **Result: 405.**
- **Whole URL skipped**: only happens if the entire route's mapping is empty (e.g., omitting `list` AND `create` skips the list URL entirely). Then **404**.

## http_method_names mechanism (less common, but valid)

- `rest_framework/views.py:484-490` (in `APIView.dispatch`): `if request.method.lower() in self.http_method_names: handler = getattr(self, request.method.lower(), self.http_method_not_allowed) else: handler = self.http_method_not_allowed`. Both branches funnel to `http_method_not_allowed`.
- `rest_framework/views.py:165-170` — `http_method_not_allowed` raises `exceptions.MethodNotAllowed(request.method)`.
- `rest_framework/exceptions.py:191-200` — `MethodNotAllowed(APIException)`, `status_code = HTTP_405_METHOD_NOT_ALLOWED`.

Note: `http_method_names` is a `django.views.generic.View` attribute inherited by `APIView`. Overriding it on a viewset works but is documented less prominently than composition.

## Destroy is NOT special-cased in DRF

- `rest_framework/mixins.py:79-89` — `DestroyModelMixin` is structurally identical to `UpdateModelMixin` and `CreateModelMixin`. Same `perform_destroy` hook (line 88-89, body `instance.delete()`). No permission/decorator/check that treats DELETE as more dangerous.
- `ModelViewSet`'s mixin chain (`viewsets.py:221`) lists `DestroyModelMixin` alongside the others — equal rank.
- The disable-by-composition idiom (`ReadOnlyModelViewSet`) drops `create`, `update`, `destroy` together as "writes". DRF's design treats the read/write split as the load-bearing line, not destroy-vs-update.

## What DRF does NOT have at 3.17.1

- No `get_view_methods` (that's a `main`-branch / post-3.17.1 addition introduced for OpenAPI generation; not relevant here).
- No `Allow*` flags or boolean per-action toggles. Composition + `http_method_names` are the only canonical mechanisms.
- No `[NonAction]`-like attribute. Python's "delete the method from the class" is composition, not an attribute marker — the closest equivalent is "don't mix in the mixin in the first place".
