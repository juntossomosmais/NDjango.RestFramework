---
name: drf-pagination-anchors
description: Pinned DRF 3.17.1 anchors for PageNumberPagination envelope, InvalidPage->NotFound mapping, and generics paginator=None contract
metadata:
  type: reference
---

DRF 3.17.1 anchors for pagination behavior. Verified 2026-05-12.

- `rest_framework/pagination.py:159-179` — `PageNumberPagination.paginate_queryset`: returns `None` only when `get_page_size(request)` is falsy (pagination disabled); otherwise calls `paginator.page(page_number)` and on `InvalidPage` raises `NotFound(invalid_page_message)`. Out-of-range page → 404, not silent clamp.
- `rest_framework/pagination.py:186-192` — `get_paginated_response` returns `Response({"count": ..., "next": ..., "previous": ..., "results": data})`. Key order: count, next, previous, results. Empty page yields `count=0`, `next=None`, `previous=None`, `results=[]`. There is no special-case branch for empty — it falls out of the normal `paginator.page(1)` path on an empty queryset (Django's `Paginator` returns an empty `Page` with `num_pages=1` when count=0; `paginator.page(1)` succeeds).
- `rest_framework/pagination.py:195-219` — `get_paginated_response_schema`: `count` and `results` are `required`; `next` and `previous` are `nullable: true` URIs. Pin this when discussing OpenAPI contract.
- `rest_framework/generics.py:157-166` — `paginator` property: `None` iff `pagination_class is None`. Settable per-view, not per-request.
- `rest_framework/generics.py:168-174` — `paginate_queryset`: returns `None` iff `self.paginator is None`. This is the only legitimate `None` return path on the view side.

Implication for the C# port: a non-nullable `Task<Paginated<T>>` matches our model where every controller is wired with a paginator. DRF's `None` exists only because the paginator itself is optional — a knob NDjango doesn't currently expose.
