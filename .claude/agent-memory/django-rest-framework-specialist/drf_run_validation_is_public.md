---
name: DRF run_validation is the public orchestrator
description: At 3.17.1 Serializer.run_validation has no leading underscore and is the documented override seam — equivalent of public, not internal
type: reference
---

`rest_framework/serializers.py:444-463` defines `Serializer.run_validation(self, data=empty)`. No underscore prefix (the file uses `_underscore` consistently for private helpers), `ListSerializer.run_validation` overrides it, and `is_valid()` calls it at `serializers.py:215-235`. DRF's intent is that `run_validation` is reachable headless — that's why `is_valid()` is a thin caching wrapper and not the orchestrator itself.

How to apply: when our C# port marks `RunValidationAsync` `internal`, that is a divergence from DRF, not parity. The DRF-aligned shape is `public` (or a public façade that forwards to it). Cite this when item-1-style questions come up about validation visibility.
