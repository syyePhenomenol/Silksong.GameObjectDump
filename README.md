# Silksong.GameObjectDump

Helper mod for dumping GameObjects to a text file.

Supports:
- Filtering for certain Components and FsmStateActions (in DumpOptions).
- Registering custom log handlers for certain types (in LoggableRegistry).

To free up some memmory after use, call `ClearReflectionCaches()`.