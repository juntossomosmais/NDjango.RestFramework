# Mandatory before any reasoning or code change

- This library has not been released yet. Breaking changes are expected, permitted, and must never block development decisions. Prioritize correctness, maintainability, and evolution over backward compatibility. Consumer migration cost is not a concern at this stage.

# DRF naming parity

This library is a port of Django REST Framework. Vocabulary is the porting contract that lets DRF users find their footing. For example, `Serializer` class has this name because it mimics the DRF concept of a serializer.